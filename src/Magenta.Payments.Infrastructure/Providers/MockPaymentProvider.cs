using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Magenta.Payments.Infrastructure.Providers;

/// <summary>
/// Mock provider for development/testing
/// </summary>
public class MockPaymentProvider : IPaymentProvider
{
    private readonly ILogger<MockPaymentProvider> _logger;

    public MockPaymentProvider(ILogger<MockPaymentProvider> logger)
    {
        _logger = logger;
    }

    public Task<CreateDepositSessionResult> CreateDepositSessionAsync(
        long playerId,
        int currencyNetworkId,
        long? expectedAmountMinor,
        long? minAmountMinor,
        int confirmationsRequired,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default)
    {
        var address = $"T{GenerateRandomAddress()}";
        var qrUri = $"tron:{address}";
        if (expectedAmountMinor.HasValue)
            qrUri += $"?amount={expectedAmountMinor.Value / 1_000_000m}";

        _logger.LogInformation("Mock provider: Created deposit session for player {PlayerId}, address {Address}",
            playerId, address);

        return Task.FromResult(new CreateDepositSessionResult(
            address,
            null,
            qrUri,
            Guid.NewGuid().ToString(),
            confirmationsRequired
        ));
    }

    public Task<ProviderDepositResult> VerifyDepositAsync(
        string txHashOrRef,
        CancellationToken cancellationToken = default)
    {
        // Mock: always return confirmed
        _logger.LogInformation("Mock provider: Verifying deposit {TxHash}", txHashOrRef);

        return Task.FromResult(new ProviderDepositResult(
            IsValid: true,
            AmountMinor: 50_000_000, // 50 USDT
            ConfirmationsReceived: 1,
            ConfirmationsRequired: 1,
            TxHash: txHashOrRef,
            ProviderReference: txHashOrRef
        ));
    }

    public Task<ProviderWithdrawalResult> SendWithdrawalAsync(
        WithdrawalRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock provider: Sending withdrawal {WithdrawalId}, amount {Amount}",
            request.WithdrawalId, request.AmountMinor);

        // Mock: simulate successful broadcast
        var txHash = $"0x{GenerateRandomAddress()}";

        return Task.FromResult(new ProviderWithdrawalResult(
            Success: true,
            TxHash: txHash,
            ProviderReference: Guid.NewGuid().ToString(),
            ErrorMessage: null
        ));
    }

    public Task<ProviderTransactionStatus> GetTransactionStatusAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock provider: Getting transaction status {Reference}", reference);

        return Task.FromResult(new ProviderTransactionStatus(
            Status: "SETTLED",
            ConfirmationsReceived: 12,
            ConfirmationsRequired: 12,
            TxHash: reference,
            IsFinal: true
        ));
    }

    private string GenerateRandomAddress()
    {
        var random = new Random();
        var chars = "0123456789ABCDEF";
        return new string(Enumerable.Repeat(chars, 33).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

