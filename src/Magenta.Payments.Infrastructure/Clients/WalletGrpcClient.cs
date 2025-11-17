using Grpc.Net.Client;
using Magenta.Grp;
using Magenta.Payments.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Magenta.Payments.Infrastructure.Clients;

public class WalletGrpcClient : IWalletClient
{
    private readonly WalletService.WalletServiceClient _client;
    private readonly ILogger<WalletGrpcClient> _logger;

    public WalletGrpcClient(string walletGrpcUrl, ILogger<WalletGrpcClient> logger)
    {
        _logger = logger;
        var channel = GrpcChannel.ForAddress(walletGrpcUrl);
        _client = new WalletService.WalletServiceClient(channel);
    }

    public async Task<OperationResult> ReserveWithdrawalAsync(
        long playerId,
        string currency,
        string network,
        long amountMinor,
        string requestId,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ReserveWithdrawalRequest
            {
                PlayerId = playerId,
                Currency = currency,
                Network = network,
                AmountMinor = amountMinor,
                RequestId = requestId,
                CorrelationId = correlationId ?? string.Empty
            };

            var response = await _client.ReserveWithdrawalAsync(request, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Wallet ReserveWithdrawal: PlayerId={PlayerId}, Amount={Amount}, Result={Result}",
                playerId, amountMinor, response.Ok);

            return new OperationResult(response.Ok, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Wallet ReserveWithdrawal");
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }

    public async Task<OperationResult> ApplyDepositSettlementAsync(
        long playerId,
        string currency,
        string network,
        long amountMinor,
        string txHash,
        string idempotencyKey,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ApplyDepositSettlementRequest
            {
                PlayerId = playerId,
                Currency = currency,
                Network = network,
                AmountMinor = amountMinor,
                TxHash = txHash,
                IdempotencyKey = idempotencyKey,
                CorrelationId = correlationId ?? string.Empty
            };

            var response = await _client.ApplyDepositSettlementAsync(request, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Wallet ApplyDepositSettlement: PlayerId={PlayerId}, Amount={Amount}, TxHash={TxHash}, Result={Result}",
                playerId, amountMinor, txHash, response.Ok);

            return new OperationResult(response.Ok, response.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Wallet ApplyDepositSettlement");
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

