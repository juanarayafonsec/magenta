using Magenta.Wallet.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Workers;

public class InboxProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxProcessorWorker> _logger;

    public InboxProcessorWorker(
        IServiceProvider serviceProvider,
        ILogger<InboxProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InboxProcessorWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inbox events");
            }

            // Poll every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessInboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();

        var unprocessedEvents = await inboxRepository.GetUnprocessedEventsAsync(limit: 100, cancellationToken);

        foreach (var evt in unprocessedEvents)
        {
            try
            {
                await DispatchEventAsync(evt, ledgerService, cancellationToken);
                await inboxRepository.MarkAsProcessedAsync(evt.InboxEventId, cancellationToken);

                _logger.LogInformation("Processed inbox event {EventId} of type {EventType}", 
                    evt.InboxEventId, evt.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process inbox event {EventId}", evt.InboxEventId);
                await inboxRepository.MarkAsFailedAsync(evt.InboxEventId, ex.Message, cancellationToken);
            }
        }
    }

    private async Task DispatchEventAsync(
        Domain.Entities.InboxEvent evt,
        ILedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        // Dispatch based on routing key
        // This is a simplified version - in production, you'd use a more sophisticated event handler registry
        var payload = evt.Payload.RootElement;

        switch (evt.RoutingKey)
        {
            case "payments.deposit.settled":
                await HandleDepositSettledAsync(payload, ledgerService, cancellationToken);
                break;
            case "payments.withdrawal.settled":
                await HandleWithdrawalSettledAsync(payload, ledgerService, cancellationToken);
                break;
            case "payments.withdrawal.failed":
                await HandleWithdrawalFailedAsync(payload, ledgerService, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown routing key: {RoutingKey}", evt.RoutingKey);
                break;
        }
    }

    private async Task HandleDepositSettledAsync(
        JsonElement payload,
        ILedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        var request = new Application.DTOs.DepositSettlementRequest
        {
            PlayerId = payload.GetProperty("playerId").GetInt64(),
            CurrencyNetworkId = payload.GetProperty("currencyNetworkId").GetInt32(),
            AmountMinor = payload.GetProperty("amountMinor").GetInt64(),
            TransactionHash = payload.GetProperty("transactionHash").GetString() ?? string.Empty,
            Source = "payments"
        };

        var result = await ledgerService.ApplyDepositSettlementAsync(request, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to apply deposit settlement: {result.ErrorMessage}");
        }
    }

    private async Task HandleWithdrawalSettledAsync(
        JsonElement payload,
        ILedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        var request = new Application.DTOs.FinalizeWithdrawalSettledRequest
        {
            PlayerId = payload.GetProperty("playerId").GetInt64(),
            CurrencyNetworkId = payload.GetProperty("currencyNetworkId").GetInt32(),
            AmountMinor = payload.GetProperty("amountMinor").GetInt64(),
            FeeMinor = payload.TryGetProperty("feeMinor", out var fee) ? fee.GetInt64() : null,
            IdempotencyKey = payload.GetProperty("idempotencyKey").GetString() ?? string.Empty,
            Source = "payments"
        };

        var result = await ledgerService.FinalizeWithdrawalSettledAsync(request, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to finalize withdrawal: {result.ErrorMessage}");
        }
    }

    private async Task HandleWithdrawalFailedAsync(
        JsonElement payload,
        ILedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        var request = new Application.DTOs.FinalizeWithdrawalFailedRequest
        {
            PlayerId = payload.GetProperty("playerId").GetInt64(),
            CurrencyNetworkId = payload.GetProperty("currencyNetworkId").GetInt32(),
            IdempotencyKey = payload.GetProperty("idempotencyKey").GetString() ?? string.Empty,
            Source = "payments"
        };

        var result = await ledgerService.FinalizeWithdrawalFailedAsync(request, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to finalize withdrawal failure: {result.ErrorMessage}");
        }
    }
}
