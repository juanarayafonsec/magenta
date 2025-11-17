using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Magenta.Payments.Infrastructure.Workers;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly RabbitMqConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisher> logger,
        IOptions<RabbitMqConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMqAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingEventsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxPublisher");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_config.Uri) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare exchanges
        _channel.ExchangeDeclare(_config.PaymentsExchange, ExchangeType.Topic, durable: true, autoDelete: false);
        
        _logger.LogInformation("RabbitMQ connection established for OutboxPublisher");
    }

    private async Task PublishPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var events = await outboxRepository.GetUnpublishedEventsAsync(100, cancellationToken);

        foreach (var evt in events)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt.Payload));
                _channel?.BasicPublish(
                    exchange: _config.PaymentsExchange,
                    routingKey: evt.RoutingKey,
                    basicProperties: null,
                    body: body);

                await outboxRepository.MarkAsPublishedAsync(evt.Id, cancellationToken);
                _logger.LogInformation("Published event {EventId} with routing key {RoutingKey}", evt.Id, evt.RoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventId}", evt.Id);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

