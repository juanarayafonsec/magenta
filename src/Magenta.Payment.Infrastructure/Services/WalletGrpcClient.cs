using Grpc.Net.Client;
using Magenta.Payment.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Magenta.Payment.Infrastructure.Services;

public class WalletGrpcClient : IWalletGrpcClient
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WalletGrpcClient> _logger;
    private readonly string _walletGrpcUrl;

    public WalletGrpcClient(IConfiguration configuration, ILogger<WalletGrpcClient> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _walletGrpcUrl = _configuration["Wallet:GrpcUrl"] ?? "http://localhost:5001";
    }

    public async Task<WalletOperationResult> ReserveWithdrawalAsync(
        long playerId,
        int currencyNetworkId,
        long amountMinor,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress(_walletGrpcUrl);
            var client = new Magenta.Wallet.Grpc.WalletService.WalletServiceClient(channel);

            var request = new Magenta.Wallet.Grpc.ReserveWithdrawalRequest
            {
                PlayerId = playerId,
                CurrencyNetworkId = currencyNetworkId,
                AmountMinor = amountMinor,
                IdempotencyKey = idempotencyKey,
                Source = "payments"
            };

            var response = await client.ReserveWithdrawalAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                var transactionId = Guid.TryParse(response.TransactionId, out var txId) ? txId : (Guid?)null;
                return WalletOperationResult.SuccessResult(transactionId ?? Guid.Empty);
            }

            return WalletOperationResult.FailureResult(response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve withdrawal via gRPC");
            return WalletOperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<WalletOperationResult> ApplyDepositSettlementAsync(
        long playerId,
        int currencyNetworkId,
        long amountMinor,
        string transactionHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress(_walletGrpcUrl);
            var client = new Magenta.Wallet.Grpc.WalletService.WalletServiceClient(channel);

            var request = new Magenta.Wallet.Grpc.DepositSettlementRequest
            {
                PlayerId = playerId,
                CurrencyNetworkId = currencyNetworkId,
                AmountMinor = amountMinor,
                TransactionHash = transactionHash,
                Source = "payments"
            };

            var response = await client.ApplyDepositSettlementAsync(request, cancellationToken: cancellationToken);

            if (response.Success)
            {
                var transactionId = Guid.TryParse(response.TransactionId, out var txId) ? txId : (Guid?)null;
                return WalletOperationResult.SuccessResult(transactionId ?? Guid.Empty);
            }

            return WalletOperationResult.FailureResult(response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply deposit settlement via gRPC");
            return WalletOperationResult.FailureResult(ex.Message);
        }
    }
}
