using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace Magenta.Payment.Workers.Jobs;

public class OutboxPublisherJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherJob> _logger;
    private readonly RabbitMQConfiguration _rabbitMqConfig;
    private IConnection? _connection;
    private IModel? _channel;

    public OutboxPublisherJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherJob> logger,
        IOptions<RabbitMQConfiguration> rabbitMqOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rabbitMqConfig = rabbitMqOptions.Value ?? throw new ArgumentNullException(nameof(rabbitMqOptions));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Initialize RabbitMQ connection if needed
        if (_connection == null || !_connection.IsOpen)
        {
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
        }

        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        // Run continuously for a time budget (e.g., 30 seconds)
        var endTime = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var unpublishedEvents = await outboxRepository.GetUnpublishedEventsAsync(limit: 50, cancellationToken);

                if (unpublishedEvents.Count == 0)
                {
                    await Task.Delay(1000, cancellationToken); // Small delay when no events
                    continue;
                }

                foreach (var evt in unpublishedEvents)
                {
                    try
                    {
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

                        await outboxRepository.MarkAsPublishedAsync(evt.OutboxEventId, cancellationToken);

                        _logger.LogInformation("Published outbox event {EventId} with routing key {RoutingKey}",
                            evt.OutboxEventId, evt.RoutingKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish outbox event {EventId}", evt.OutboxEventId);
                        await outboxRepository.IncrementPublishAttemptsAsync(evt.OutboxEventId, ex.Message, cancellationToken);
                    }
                }

                // Small delay between batches
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox publisher job");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
