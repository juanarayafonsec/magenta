using Magenta.Wallet.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Messaging;

public class PaymentsEventsConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentsEventsConsumer> _logger;
    private readonly RabbitMqConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public PaymentsEventsConsumer(
        IServiceProvider serviceProvider,
        ILogger<PaymentsEventsConsumer> logger,
        IOptions<RabbitMqConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMqAsync(stoppingToken);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                await ProcessMessageAsync(ea, stoppingToken);
                _channel?.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}", _config.WalletQueue);
                _channel?.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel?.BasicConsume(queue: _config.WalletQueue, autoAck: false, consumer: consumer);

        _logger.LogInformation("PaymentsEventsConsumer started, listening to queue {Queue}", _config.WalletQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_config.Uri) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        RabbitMqSetup.DeclareExchanges(_channel, _config);
        _logger.LogInformation("RabbitMQ connection established for PaymentsEventsConsumer");
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var routingKey = ea.RoutingKey;
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var payload = JsonDocument.Parse(body);

        using var scope = _serviceProvider.CreateScope();
        var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();

        // Extract idempotency key from payload
        var eventId = payload.RootElement.GetProperty("eventId").GetString() ?? Guid.NewGuid().ToString();
        var idempotencyKey = payload.RootElement.TryGetProperty("idempotencyKey", out var idemKey) 
            ? idemKey.GetString() 
            : eventId;

        var source = "payments";

        // Try to record in inbox (dedupe)
        if (!await inboxStore.TryRecordInboxEventAsync(source, idempotencyKey, payload, cancellationToken))
        {
            _logger.LogWarning("Duplicate event detected: {IdempotencyKey}", idempotencyKey);
            return;
        }

        // Route to appropriate handler
        var handlers = scope.ServiceProvider.GetServices<IPaymentsEventHandler>();
        foreach (var handler in handlers)
        {
            if (await handler.CanHandleAsync(routingKey))
            {
                await handler.HandleAsync(routingKey, payload, cancellationToken);
                await inboxStore.MarkAsProcessedAsync(source, idempotencyKey, cancellationToken);
                return;
            }
        }

        _logger.LogWarning("No handler found for routing key {RoutingKey}", routingKey);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

public interface IPaymentsEventHandler
{
    Task<bool> CanHandleAsync(string routingKey);
    Task HandleAsync(string routingKey, JsonDocument payload, CancellationToken cancellationToken);
}

