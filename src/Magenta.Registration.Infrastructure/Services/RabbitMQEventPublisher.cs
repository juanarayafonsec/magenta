using Magenta.Registration.Infrastructure.Configuration;
using Magenta.Registration.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Magenta.Registration.Infrastructure.Services;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly string _exchangeName;

    public RabbitMQEventPublisher(IOptions<RabbitMQConfiguration> rabbitMqOptions, ILogger<RabbitMQEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var config = rabbitMqOptions.Value;
        
        // Validate configuration
        var validationErrors = config.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = string.Join(", ", validationErrors);
            throw new InvalidOperationException($"Invalid RabbitMQ configuration: {errorMessage}");
        }

        _exchangeName = config.ExchangeName;

        // Create connection factory
        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
            AutomaticRecoveryEnabled = config.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(config.NetworkRecoveryIntervalSeconds)
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            // Declare the exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", config.Host, config.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", config.Host, config.Port);
            throw;
        }
    }

    /// <summary>
    /// Publishes an event to RabbitMQ.
    /// </summary>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var eventType = typeof(T).Name;
            var routingKey = GetRoutingKey(eventType);
            
            // Serialize the event
            var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var body = Encoding.UTF8.GetBytes(json);

            // Create message properties
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = eventType;
            properties.Headers = new Dictionary<string, object>
            {
                { "EventType", eventType },
                { "Source", "Magenta.Registration" },
                { "Version", "1.0" }
            };

            // Publish the message
            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", eventType, routingKey);
            
            await Task.CompletedTask; // For async compatibility
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets the routing key for the event type.
    /// </summary>
    private static string GetRoutingKey(string eventType)
    {
        return eventType switch
        {
            "UserCreatedEvent" => "user.created",
            "UserUpdatedEvent" => "user.updated",
            "UserDeletedEvent" => "user.deleted",
            _ => $"event.{eventType.ToLowerInvariant()}"
        };
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and channel.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("Disconnected from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
