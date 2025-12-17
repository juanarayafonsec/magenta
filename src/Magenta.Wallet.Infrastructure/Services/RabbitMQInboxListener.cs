using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Services;

/// <summary>
/// MQ listener that writes all incoming messages to inbox_events table and ACKs only after DB write.
/// This ensures reliable event processing.
/// </summary>
public class RabbitMQInboxListener : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQInboxListener> _logger;
    private readonly RabbitMQConfiguration _rabbitMqConfig;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _queueName;

    public RabbitMQInboxListener(
        IServiceProvider serviceProvider,
        ILogger<RabbitMQInboxListener> logger,
        IOptions<RabbitMQConfiguration> rabbitMqOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rabbitMqConfig = rabbitMqOptions.Value ?? throw new ArgumentNullException(nameof(rabbitMqOptions));
        _queueName = "wallet.inbox.events";
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqConfig.Host,
            Port = _rabbitMqConfig.Port,
            UserName = _rabbitMqConfig.Username,
            Password = _rabbitMqConfig.Password,
            VirtualHost = _rabbitMqConfig.VirtualHost,
            AutomaticRecoveryEnabled = _rabbitMqConfig.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_rabbitMqConfig.NetworkRecoveryIntervalSeconds)
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _rabbitMqConfig.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind to events we care about
            _channel.QueueBind(_queueName, _rabbitMqConfig.ExchangeName, "payments.deposit.settled");
            _channel.QueueBind(_queueName, _rabbitMqConfig.ExchangeName, "payments.withdrawal.settled");
            _channel.QueueBind(_queueName, _rabbitMqConfig.ExchangeName, "payments.withdrawal.failed");

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

                    // Check if already processed (idempotency)
                    var source = ea.BasicProperties.Headers?.ContainsKey("Source") == true
                        ? Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["Source"])
                        : "unknown";

                    if (await inboxRepository.ExistsAsync(source, messageId, cancellationToken))
                    {
                        _logger.LogInformation("Duplicate message received, ignoring: {MessageId}", messageId);
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        return;
                    }

                    // Write to inbox table
                    var inboxEvent = new InboxEvent
                    {
                        Source = source,
                        MessageId = messageId,
                        EventType = ea.BasicProperties.Type ?? "Unknown",
                        RoutingKey = routingKey,
                        Payload = JsonDocument.Parse(message)
                    };

                    await inboxRepository.CreateInboxEventAsync(inboxEvent, cancellationToken);

                    // ACK only after DB write succeeds
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                    _logger.LogInformation("Received and stored inbox event {MessageId} with routing key {RoutingKey}", 
                        messageId, routingKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with routing key {RoutingKey}: {Message}", 
                        routingKey, message);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("RabbitMQInboxListener started, listening on queue {QueueName}", _queueName);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQInboxListener");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("RabbitMQInboxListener stopped");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
