using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Magenta.Payments.Infrastructure.Workers;

public class InboxConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxConsumer> _logger;
    private readonly RabbitMqConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public InboxConsumer(
        IServiceProvider serviceProvider,
        ILogger<InboxConsumer> logger,
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
                _logger.LogError(ex, "Error processing message from queue {Queue}", _config.PaymentsQueue);
                _channel?.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel?.BasicConsume(queue: _config.PaymentsQueue, autoAck: false, consumer: consumer);

        _logger.LogInformation("InboxConsumer started, listening to queue {Queue}", _config.PaymentsQueue);

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
        
        // Declare exchanges and queue
        _channel.ExchangeDeclare(_config.WalletExchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(_config.PaymentsQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.PaymentsQueue, _config.WalletExchange, "wallet.withdrawal.reserved");
        
        _logger.LogInformation("RabbitMQ connection established for InboxConsumer");
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var routingKey = ea.RoutingKey;
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var payload = JsonDocument.Parse(body);

        using var scope = _serviceProvider.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

        // Extract idempotency key
        var eventId = payload.RootElement.TryGetProperty("eventId", out var eId) 
            ? eId.GetString() 
            : Guid.NewGuid().ToString();
        var idempotencyKey = payload.RootElement.TryGetProperty("idempotencyKey", out var idemKey) 
            ? idemKey.GetString() 
            : eventId;

        var source = "wallet";

        // Try to record in inbox (dedupe)
        if (!await inboxRepository.TryRecordInboxEventAsync(source, idempotencyKey ?? eventId, payload, cancellationToken))
        {
            _logger.LogWarning("Duplicate event detected: {IdempotencyKey}", idempotencyKey);
            return;
        }

        // Process wallet events
        if (routingKey == "wallet.withdrawal.reserved")
        {
            // Handle withdrawal reserved event
            _logger.LogInformation("Processed wallet.withdrawal.reserved event");
        }

        await inboxRepository.MarkAsProcessedAsync(source, idempotencyKey ?? eventId, cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

