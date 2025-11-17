using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class PaymentsDepositSettledHandler : IPaymentsEventHandler
{
    private readonly ApplyDepositSettlementHandler _handler;
    private readonly ILogger<PaymentsDepositSettledHandler> _logger;

    public PaymentsDepositSettledHandler(
        ApplyDepositSettlementHandler handler,
        ILogger<PaymentsDepositSettledHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(string routingKey) => Task.FromResult(routingKey == "payments.deposit.settled");

    public async Task HandleAsync(string routingKey, JsonDocument payload, CancellationToken cancellationToken)
    {
        var root = payload.RootElement;
        var command = new ApplyDepositSettlementCommand(
            root.GetProperty("playerId").GetInt64(),
            root.GetProperty("currency").GetString() ?? "",
            root.GetProperty("network").GetString() ?? "",
            root.GetProperty("amountMinor").GetInt64(),
            root.GetProperty("txHash").GetString() ?? "",
            root.GetProperty("idempotencyKey").GetString() ?? root.GetProperty("eventId").GetString() ?? "",
            root.TryGetProperty("correlationId", out var corrId) ? corrId.GetString() : null
        );

        await _handler.HandleAsync(command, cancellationToken);
    }
}

public class PaymentsWithdrawalSettledHandler : IPaymentsEventHandler
{
    private readonly FinalizeWithdrawalHandler _handler;
    private readonly ILogger<PaymentsWithdrawalSettledHandler> _logger;

    public PaymentsWithdrawalSettledHandler(
        FinalizeWithdrawalHandler handler,
        ILogger<PaymentsWithdrawalSettledHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(string routingKey) => Task.FromResult(routingKey == "payments.withdrawal.settled");

    public async Task HandleAsync(string routingKey, JsonDocument payload, CancellationToken cancellationToken)
    {
        var root = payload.RootElement;
        var command = new FinalizeWithdrawalCommand(
            root.GetProperty("playerId").GetInt64(),
            root.GetProperty("currency").GetString() ?? "",
            root.GetProperty("network").GetString() ?? "",
            root.GetProperty("amountMinor").GetInt64(),
            root.GetProperty("feeMinor").GetInt64(),
            root.GetProperty("requestId").GetString() ?? "",
            root.GetProperty("txHash").GetString() ?? "",
            root.TryGetProperty("correlationId", out var corrId) ? corrId.GetString() : null
        );

        await _handler.HandleAsync(command, cancellationToken);
    }
}

public class PaymentsWithdrawalFailedHandler : IPaymentsEventHandler
{
    private readonly ReleaseWithdrawalHandler _handler;
    private readonly ILogger<PaymentsWithdrawalFailedHandler> _logger;

    public PaymentsWithdrawalFailedHandler(
        ReleaseWithdrawalHandler handler,
        ILogger<PaymentsWithdrawalFailedHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(string routingKey) => Task.FromResult(routingKey == "payments.withdrawal.failed");

    public async Task HandleAsync(string routingKey, JsonDocument payload, CancellationToken cancellationToken)
    {
        var root = payload.RootElement;
        var command = new ReleaseWithdrawalCommand(
            root.GetProperty("playerId").GetInt64(),
            root.GetProperty("currency").GetString() ?? "",
            root.GetProperty("network").GetString() ?? "",
            root.GetProperty("amountMinor").GetInt64(),
            root.GetProperty("requestId").GetString() ?? "",
            root.TryGetProperty("correlationId", out var corrId) ? corrId.GetString() : null
        );

        await _handler.HandleAsync(command, cancellationToken);
    }
}

public class PaymentsWithdrawalBroadcastedHandler : IPaymentsEventHandler
{
    private readonly ILogger<PaymentsWithdrawalBroadcastedHandler> _logger;

    public PaymentsWithdrawalBroadcastedHandler(ILogger<PaymentsWithdrawalBroadcastedHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(string routingKey) => Task.FromResult(routingKey == "payments.withdrawal.broadcasted");

    public Task HandleAsync(string routingKey, JsonDocument payload, CancellationToken cancellationToken)
    {
        // No postings for broadcasted - just log
        _logger.LogInformation("Withdrawal broadcasted: {RequestId}", 
            payload.RootElement.TryGetProperty("requestId", out var reqId) ? reqId.GetString() : "unknown");
        return Task.CompletedTask;
    }
}

