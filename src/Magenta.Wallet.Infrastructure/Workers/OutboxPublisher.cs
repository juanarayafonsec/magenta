using Magenta.Wallet.Application.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Workers;

public class OutboxPublisher : BackgroundService
{
    private readonly IOutboxStore _outboxStore;
    private readonly RabbitMQConfiguration _config;
    private readonly ILogger<OutboxPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public OutboxPublisher(
        IOutboxStore outboxStore,
        IOptions<RabbitMQConfiguration> config,
        ILogger<OutboxPublisher> logger)
    {
        _outboxStore = outboxStore;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMQAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var events = await _outboxStore.GetUnpublishedEventsAsync(100, stoppingToken);

                foreach (var evt in events)
                {
                    try
                    {
                        await PublishEventAsync(evt, stoppingToken);
                        await _outboxStore.MarkPublishedAsync(evt.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish outbox event {EventId}", evt.Id);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxPublisher");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task InitializeRabbitMQAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_config.Uri) };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateModelAsync();

        await _channel.ExchangeDeclareAsync(_config.WalletExchange, "topic", true, false);
        
        _logger.LogInformation("OutboxPublisher initialized");
    }

    private async Task PublishEventAsync(OutboxEventDto evt, CancellationToken cancellationToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel not initialized");

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt.Payload));
        
        var props = new BasicProperties
        {
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: _config.WalletExchange,
            routingKey: evt.RoutingKey,
            basicProperties: props,
            body: body);
        
        _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", 
            evt.EventType, evt.RoutingKey);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}




