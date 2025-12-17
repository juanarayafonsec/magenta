using Grpc.Core;
using Magenta.Wallet.Application.Interfaces;

namespace Magenta.Wallet.Grpc.Services;

public class WalletService : Grpc.WalletService.WalletServiceBase
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<WalletService> _logger;

    public WalletService(ILedgerService ledgerService, ILogger<WalletService> logger)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<OperationResult> ReserveWithdrawal(ReserveWithdrawalRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.ReserveWithdrawalRequest
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                AmountMinor = request.AmountMinor,
                IdempotencyKey = request.IdempotencyKey,
                Source = request.Source
            };

            var result = await _ledgerService.ReserveWithdrawalAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReserveWithdrawal");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<OperationResult> FinalizeWithdrawalSettled(FinalizeWithdrawalSettledRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.FinalizeWithdrawalSettledRequest
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                AmountMinor = request.AmountMinor,
                FeeMinor = request.FeeMinor == 0 ? null : request.FeeMinor,
                IdempotencyKey = request.IdempotencyKey,
                Source = request.Source
            };

            var result = await _ledgerService.FinalizeWithdrawalSettledAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FinalizeWithdrawalSettled");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<OperationResult> FinalizeWithdrawalFailed(FinalizeWithdrawalFailedRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.FinalizeWithdrawalFailedRequest
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                IdempotencyKey = request.IdempotencyKey,
                Source = request.Source
            };

            var result = await _ledgerService.FinalizeWithdrawalFailedAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FinalizeWithdrawalFailed");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<OperationResult> ApplyDepositSettlement(DepositSettlementRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.DepositSettlementRequest
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                AmountMinor = request.AmountMinor,
                TransactionHash = request.TransactionHash,
                Source = request.Source
            };

            var result = await _ledgerService.ApplyDepositSettlementAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApplyDepositSettlement");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<OperationResult> PostBet(BetRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.BetRequest
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                AmountMinor = request.AmountMinor,
                BetId = request.BetId,
                Source = request.Source
            };

            var result = await _ledgerService.PostBetAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PostBet");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<OperationResult> PostWin(WinRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.WinRequest
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                AmountMinor = request.AmountMinor,
                WinId = request.WinId,
                Source = request.Source
            };

            var result = await _ledgerService.PostWinAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PostWin");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<OperationResult> RollbackTransaction(RollbackRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new Application.DTOs.RollbackRequest
            {
                OriginalTransactionReference = request.OriginalTransactionReference,
                RollbackId = request.RollbackId,
                Source = request.Source
            };

            var result = await _ledgerService.RollbackTransactionAsync(dto, context.CancellationToken);

            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage ?? string.Empty,
                TransactionId = result.TransactionId?.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RollbackTransaction");
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
