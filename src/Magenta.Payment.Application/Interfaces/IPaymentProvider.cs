using Magenta.Payment.Application.DTOs;

namespace Magenta.Payment.Application.Interfaces;

public interface IPaymentProvider
{
    Task<DepositSessionResult> CreateDepositSessionAsync(
        int currencyNetworkId,
        long? expectedAmountMinor,
        int confirmationsRequired,
        CancellationToken cancellationToken = default);

    Task<DepositVerificationResult> VerifyDepositAsync(
        string txHash,
        CancellationToken cancellationToken = default);

    Task<WithdrawalResult> SendWithdrawalAsync(
        int currencyNetworkId,
        string targetAddress,
        long amountMinor,
        CancellationToken cancellationToken = default);

    Task<TransactionStatusResult> GetTransactionStatusAsync(
        string reference,
        CancellationToken cancellationToken = default);

    bool VerifyWebhookSignature(string payload, string signature, string secret);
}
