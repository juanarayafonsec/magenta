using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Magenta.Payment.Infrastructure.Services;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly RabbitMQConfiguration _config;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQEventPublisher(
        IOptions<RabbitMQConfiguration> config,
        ILogger<RabbitMQEventPublisher> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var factory = new ConnectionFactory
        {
            HostName = _config.Host,
            Port = _config.Port,
            UserName = _config.Username,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync(string eventType, string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Type = eventType;

            _channel.BasicPublish(
                exchange: _config.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", eventType, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", eventType);
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
