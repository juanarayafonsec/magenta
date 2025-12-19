namespace Magenta.Payment.Application.Interfaces;

public interface IWalletGrpcClient
{
    Task<WalletOperationResult> ReserveWithdrawalAsync(
        long playerId,
        int currencyNetworkId,
        long amountMinor,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<WalletOperationResult> ApplyDepositSettlementAsync(
        long playerId,
        int currencyNetworkId,
        long amountMinor,
        string transactionHash,
        CancellationToken cancellationToken = default);
}

public class WalletOperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? TransactionId { get; set; }

    public static WalletOperationResult SuccessResult(Guid transactionId) =>
        new() { Success = true, TransactionId = transactionId };

    public static WalletOperationResult FailureResult(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
