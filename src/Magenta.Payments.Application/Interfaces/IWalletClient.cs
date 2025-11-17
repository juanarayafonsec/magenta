namespace Magenta.Payments.Application.Interfaces;

/// <summary>
/// gRPC client interface for Wallet System
/// </summary>
public interface IWalletClient
{
    /// <summary>
    /// Reserves funds for withdrawal in Wallet
    /// </summary>
    Task<OperationResult> ReserveWithdrawalAsync(
        long playerId,
        string currency,
        string network,
        long amountMinor,
        string requestId,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies deposit settlement in Wallet
    /// </summary>
    Task<OperationResult> ApplyDepositSettlementAsync(
        long playerId,
        string currency,
        string network,
        long amountMinor,
        string txHash,
        string idempotencyKey,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}

public record OperationResult(
    bool Ok,
    string Message
);

