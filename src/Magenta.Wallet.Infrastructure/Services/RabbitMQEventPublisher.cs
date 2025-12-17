using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Services;

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
        
        var validationErrors = config.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = string.Join(", ", validationErrors);
            throw new InvalidOperationException($"Invalid RabbitMQ configuration: {errorMessage}");
        }

        _exchangeName = config.ExchangeName;

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

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = typeof(T).Name;
            properties.Headers = new Dictionary<string, object>
            {
                { "EventType", typeof(T).Name },
                { "Source", "Magenta.Wallet" },
                { "Version", "1.0" }
            };

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", typeof(T).Name, routingKey);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
            throw;
        }
    }

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
