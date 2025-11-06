using Magenta.Wallet.Application.DTOs.Commands;
using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Application.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Workers;

public class PaymentsEventsConsumer : BackgroundService
{
    private readonly IInboxStore _inboxStore;
    private readonly WalletCommandService _commandService;
    private readonly RabbitMQConfiguration _config;
    private readonly ILogger<PaymentsEventsConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public PaymentsEventsConsumer(
        IInboxStore inboxStore,
        WalletCommandService commandService,
        IOptions<RabbitMQConfiguration> config,
        ILogger<PaymentsEventsConsumer> logger)
    {
        _inboxStore = inboxStore;
        _commandService = commandService;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMQAsync(stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                await ProcessMessageAsync(ea, stoppingToken);
                if (_channel != null)
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {RoutingKey}", ea.RoutingKey);
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        if (_channel != null)
        {
            await _channel.BasicConsumeAsync(_config.WalletQueue, false, consumer);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task InitializeRabbitMQAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_config.Uri) };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateModelAsync();

        await _channel.ExchangeDeclareAsync(_config.PaymentsExchange, "topic", true, false);
        await _channel.QueueDeclareAsync(_config.WalletQueue, true, false, false);
        
        await _channel.QueueBindAsync(
            _config.WalletQueue,
            _config.PaymentsExchange,
            "payments.deposit.settled");
        
        await _channel.QueueBindAsync(
            _config.WalletQueue,
            _config.PaymentsExchange,
            "payments.withdrawal.broadcasted");
        
        await _channel.QueueBindAsync(
            _config.WalletQueue,
            _config.PaymentsExchange,
            "payments.withdrawal.settled");
        
        await _channel.QueueBindAsync(
            _config.WalletQueue,
            _config.PaymentsExchange,
            "payments.withdrawal.failed");

        _logger.LogInformation("PaymentsEventsConsumer initialized");
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(body) ?? new();

        var source = ea.RoutingKey;
        var idempotencyKey = payload.GetValueOrDefault("idempotencyKey")?.ToString() ?? 
                           payload.GetValueOrDefault("eventId")?.ToString() ?? 
                           Guid.NewGuid().ToString();

        // Deduplicate via inbox
        var recorded = await _inboxStore.TryRecordEventAsync(
            source, idempotencyKey, payload, cancellationToken);

        if (!recorded)
        {
            _logger.LogInformation("Duplicate event ignored: {Source}/{IdempotencyKey}", source, idempotencyKey);
            return;
        }

        try
        {
            // Route to appropriate handler
            await RouteToHandlerAsync(source, payload, cancellationToken);
            
            await _inboxStore.MarkProcessedAsync(source, idempotencyKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event {Source}", source);
            throw;
        }
    }

    private async Task RouteToHandlerAsync(
        string routingKey,
        Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        switch (routingKey)
        {
            case "payments.deposit.settled":
                var depositCmd = new ApplyDepositSettlementCommand
                {
                    PlayerId = GetLong(payload, "playerId"),
                    Currency = GetString(payload, "currency"),
                    Network = GetString(payload, "network"),
                    AmountMinor = GetLong(payload, "amountMinor"),
                    TxHash = GetString(payload, "txHash"),
                    IdempotencyKey = GetString(payload, "idempotencyKey"),
                    CorrelationId = payload.GetValueOrDefault("correlationId")?.ToString()
                };
                await _commandService.HandleAsync(depositCmd, cancellationToken);
                break;

            case "payments.withdrawal.settled":
                var settleCmd = new FinalizeWithdrawalCommand
                {
                    PlayerId = GetLong(payload, "playerId"),
                    Currency = GetString(payload, "currency"),
                    Network = GetString(payload, "network"),
                    AmountMinor = GetLong(payload, "amountMinor"),
                    FeeMinor = GetLong(payload, "feeMinor"),
                    RequestId = GetString(payload, "requestId"),
                    TxHash = GetString(payload, "txHash"),
                    CorrelationId = payload.GetValueOrDefault("correlationId")?.ToString()
                };
                await _commandService.HandleAsync(settleCmd, cancellationToken);
                break;

            case "payments.withdrawal.failed":
                var releaseCmd = new ReleaseWithdrawalCommand
                {
                    PlayerId = GetLong(payload, "playerId"),
                    Currency = GetString(payload, "currency"),
                    Network = GetString(payload, "network"),
                    AmountMinor = GetLong(payload, "amountMinor"),
                    RequestId = GetString(payload, "requestId"),
                    CorrelationId = payload.GetValueOrDefault("correlationId")?.ToString()
                };
                await _commandService.HandleAsync(releaseCmd, cancellationToken);
                break;

            case "payments.withdrawal.broadcasted":
                // No postings, just log
                _logger.LogInformation("Withdrawal broadcasted: {RequestId}", 
                    GetString(payload, "requestId"));
                break;
        }
    }

    private static long GetLong(Dictionary<string, object> payload, string key)
    {
        return payload.GetValueOrDefault(key) switch
        {
            long l => l,
            int i => i,
            string s when long.TryParse(s, out var l) => l,
            _ => 0
        };
    }

    private static string GetString(Dictionary<string, object> payload, string key)
    {
        return payload.GetValueOrDefault(key)?.ToString() ?? string.Empty;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}




