using Magenta.Grp;
using Magenta.Wallet.Application.DTOs.Commands;
using Magenta.Wallet.Application.DTOs.Queries;
using Magenta.Wallet.Application.Handlers;
using Magenta.Wallet.Application.Services;
using Grpc.Core;

namespace Magenta.Wallet.Grpc.Services;

public class WalletServiceImplementation : WalletService.WalletServiceBase
{
    private readonly WalletCommandService _commandService;
    private readonly GetBalanceHandler _balanceHandler;
    private readonly ILogger<WalletServiceImplementation> _logger;

    public WalletServiceImplementation(
        WalletCommandService commandService,
        GetBalanceHandler balanceHandler,
        ILogger<WalletServiceImplementation> logger)
    {
        _commandService = commandService;
        _balanceHandler = balanceHandler;
        _logger = logger;
    }

    public override async Task<OperationResult> ApplyDepositSettlement(
        ApplyDepositSettlementRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new ApplyDepositSettlementCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                AmountMinor = request.AmountMinor,
                TxHash = request.TxHash,
                IdempotencyKey = request.IdempotencyKey,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Deposit settlement applied" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying deposit settlement for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<OperationResult> ReserveWithdrawal(
        ReserveWithdrawalRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new ReserveWithdrawalCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                AmountMinor = request.AmountMinor,
                RequestId = request.RequestId,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Withdrawal reserved" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving withdrawal for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<OperationResult> FinalizeWithdrawal(
        FinalizeWithdrawalRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new FinalizeWithdrawalCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                AmountMinor = request.AmountMinor,
                FeeMinor = request.FeeMinor,
                RequestId = request.RequestId,
                TxHash = request.TxHash,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Withdrawal finalized" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing withdrawal for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<OperationResult> ReleaseWithdrawal(
        ReleaseWithdrawalRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new ReleaseWithdrawalCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                AmountMinor = request.AmountMinor,
                RequestId = request.RequestId,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Withdrawal released" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing withdrawal for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<OperationResult> PlaceBet(
        PlaceBetRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new PlaceBetCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                AmountMinor = request.AmountMinor,
                BetId = request.BetId,
                Provider = request.Provider,
                RoundId = request.RoundId,
                GameCode = request.GameCode,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Bet placed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing bet for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<OperationResult> SettleWin(
        SettleWinRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new SettleWinCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                AmountMinor = request.AmountMinor,
                WinId = request.WinId,
                BetId = request.BetId,
                RoundId = request.RoundId,
                Provider = request.Provider,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Win settled" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error settling win for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<OperationResult> Rollback(
        RollbackRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new RollbackCommand
            {
                PlayerId = request.PlayerId,
                Currency = request.Currency,
                Network = request.Network,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                RollbackId = request.RollbackId,
                Reason = request.Reason,
                CorrelationId = request.CorrelationId
            };

            await _commandService.HandleAsync(command, context.CancellationToken);

            return new OperationResult { Ok = true, Message = "Rollback processed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rollback for player {PlayerId}", request.PlayerId);
            return new OperationResult { Ok = false, Message = ex.Message };
        }
    }

    public override async Task<GetBalanceResponse> GetBalance(
        GetBalanceRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new GetBalanceQuery { PlayerId = request.PlayerId };
            var response = await _balanceHandler.HandleAsync(query, context.CancellationToken);

            var grpcResponse = new GetBalanceResponse();
            grpcResponse.Items.AddRange(response.Items.Select(i => new BalanceItem
            {
                Currency = i.Currency,
                Network = i.Network,
                BalanceMinor = i.BalanceMinor,
                ReservedMinor = i.ReservedMinor,
                CashableMinor = i.CashableMinor
            }));

            return grpcResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for player {PlayerId}", request.PlayerId);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}




