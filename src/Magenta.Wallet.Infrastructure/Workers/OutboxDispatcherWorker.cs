using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Workers;

public class OutboxDispatcherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcherWorker> _logger;
    private readonly RabbitMQConfiguration _rabbitMqConfig;
    private IConnection? _connection;
    private IModel? _channel;

    public OutboxDispatcherWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcherWorker> logger,
        IOptions<RabbitMQConfiguration> rabbitMqOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rabbitMqConfig = rabbitMqOptions.Value ?? throw new ArgumentNullException(nameof(rabbitMqOptions));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize RabbitMQ connection
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqConfig.Host,
            Port = _rabbitMqConfig.Port,
            UserName = _rabbitMqConfig.Username,
            Password = _rabbitMqConfig.Password,
            VirtualHost = _rabbitMqConfig.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_rabbitMqConfig.ExchangeName, ExchangeType.Topic, durable: true);

        _logger.LogInformation("OutboxDispatcherWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            // Poll every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pendingEvents = await outboxRepository.GetPendingEventsAsync(limit: 100, cancellationToken);

        foreach (var evt in pendingEvents)
        {
            try
            {
                // Publish to RabbitMQ
                var body = Encoding.UTF8.GetBytes(evt.Payload.RootElement.GetRawText());

                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Type = evt.EventType;

                _channel.BasicPublish(
                    exchange: _rabbitMqConfig.ExchangeName,
                    routingKey: evt.RoutingKey,
                    basicProperties: properties,
                    body: body);

                // Mark as sent
                await outboxRepository.MarkAsSentAsync(evt.OutboxEventId, cancellationToken);

                _logger.LogInformation("Published outbox event {EventId} with routing key {RoutingKey}", 
                    evt.OutboxEventId, evt.RoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId}", evt.OutboxEventId);
                await outboxRepository.IncrementRetryCountAsync(evt.OutboxEventId, cancellationToken);

                // Mark as failed after max retries (e.g., 5)
                if (evt.RetryCount >= 5)
                {
                    await outboxRepository.MarkAsFailedAsync(evt.OutboxEventId, ex.Message, cancellationToken);
                }
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
