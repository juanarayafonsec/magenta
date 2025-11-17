using Magenta.Payments.Domain.Entities;

namespace Magenta.Payments.Domain.Interfaces;

/// <summary>
/// Provider abstraction for payment gateway integrations
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Creates a deposit session and returns address/QR information
    /// </summary>
    Task<CreateDepositSessionResult> CreateDepositSessionAsync(
        long playerId,
        int currencyNetworkId,
        long? expectedAmountMinor,
        long? minAmountMinor,
        int confirmationsRequired,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a deposit transaction by txHash or reference
    /// </summary>
    Task<ProviderDepositResult> VerifyDepositAsync(
        string txHashOrRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a withdrawal to the provider
    /// </summary>
    Task<ProviderWithdrawalResult> SendWithdrawalAsync(
        WithdrawalRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a transaction
    /// </summary>
    Task<ProviderTransactionStatus> GetTransactionStatusAsync(
        string reference,
        CancellationToken cancellationToken = default);
}

public record CreateDepositSessionResult(
    string Address,
    string? MemoOrTag,
    string? QrUri,
    string ProviderReference,
    int ConfirmationsRequired
);

public record ProviderDepositResult(
    bool IsValid,
    long AmountMinor,
    int ConfirmationsReceived,
    int ConfirmationsRequired,
    string? TxHash,
    string? ProviderReference
);

public record ProviderWithdrawalResult(
    bool Success,
    string? TxHash,
    string? ProviderReference,
    string? ErrorMessage
);

public record ProviderTransactionStatus(
    string Status,
    int ConfirmationsReceived,
    int ConfirmationsRequired,
    string? TxHash,
    bool IsFinal
);

