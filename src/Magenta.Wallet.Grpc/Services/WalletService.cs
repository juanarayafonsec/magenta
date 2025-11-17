using Grpc.Core;
using Magenta.Grp;
using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Handlers;
using Microsoft.Extensions.Logging;

namespace Magenta.Wallet.Grpc.Services;

public class WalletService : WalletServiceBase
{
    private readonly ApplyDepositSettlementHandler _applyDepositHandler;
    private readonly ReserveWithdrawalHandler _reserveWithdrawalHandler;
    private readonly FinalizeWithdrawalHandler _finalizeWithdrawalHandler;
    private readonly ReleaseWithdrawalHandler _releaseWithdrawalHandler;
    private readonly PlaceBetHandler _placeBetHandler;
    private readonly SettleWinHandler _settleWinHandler;
    private readonly RollbackHandler _rollbackHandler;
    private readonly GetBalanceHandler _getBalanceHandler;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        ApplyDepositSettlementHandler applyDepositHandler,
        ReserveWithdrawalHandler reserveWithdrawalHandler,
        FinalizeWithdrawalHandler finalizeWithdrawalHandler,
        ReleaseWithdrawalHandler releaseWithdrawalHandler,
        PlaceBetHandler placeBetHandler,
        SettleWinHandler settleWinHandler,
        RollbackHandler rollbackHandler,
        GetBalanceHandler getBalanceHandler,
        ILogger<WalletService> logger)
    {
        _applyDepositHandler = applyDepositHandler;
        _reserveWithdrawalHandler = reserveWithdrawalHandler;
        _finalizeWithdrawalHandler = finalizeWithdrawalHandler;
        _releaseWithdrawalHandler = releaseWithdrawalHandler;
        _placeBetHandler = placeBetHandler;
        _settleWinHandler = settleWinHandler;
        _rollbackHandler = rollbackHandler;
        _getBalanceHandler = getBalanceHandler;
        _logger = logger;
    }

    public override async Task<OperationResult> ApplyDepositSettlement(
        ApplyDepositSettlementRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        _logger.LogInformation("ApplyDepositSettlement: PlayerId={PlayerId}, Amount={Amount}, CorrelationId={CorrelationId}",
            request.PlayerId, request.AmountMinor, correlationId);

        var command = new ApplyDepositSettlementCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.AmountMinor,
            request.TxHash,
            request.IdempotencyKey,
            correlationId
        );

        var result = await _applyDepositHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<OperationResult> ReserveWithdrawal(
        ReserveWithdrawalRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        var command = new ReserveWithdrawalCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.AmountMinor,
            request.RequestId,
            correlationId
        );

        var result = await _reserveWithdrawalHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<OperationResult> FinalizeWithdrawal(
        FinalizeWithdrawalRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        var command = new FinalizeWithdrawalCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.AmountMinor,
            request.FeeMinor,
            request.RequestId,
            request.TxHash,
            correlationId
        );

        var result = await _finalizeWithdrawalHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<OperationResult> ReleaseWithdrawal(
        ReleaseWithdrawalRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        var command = new ReleaseWithdrawalCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.AmountMinor,
            request.RequestId,
            correlationId
        );

        var result = await _releaseWithdrawalHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<OperationResult> PlaceBet(
        PlaceBetRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        var command = new PlaceBetCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.AmountMinor,
            request.BetId,
            request.Provider,
            request.RoundId,
            request.GameCode,
            correlationId
        );

        var result = await _placeBetHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<OperationResult> SettleWin(
        SettleWinRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        var command = new SettleWinCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.AmountMinor,
            request.WinId,
            request.BetId,
            request.RoundId,
            request.Provider,
            correlationId
        );

        var result = await _settleWinHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<OperationResult> Rollback(
        RollbackRequest request,
        ServerCallContext context)
    {
        var correlationId = request.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.RequestHeaders.GetValue("correlation-id") ?? Guid.NewGuid().ToString();
        }

        var command = new RollbackCommand(
            request.PlayerId,
            request.Currency,
            request.Network,
            request.ReferenceType,
            request.ReferenceId,
            request.RollbackId,
            request.Reason,
            correlationId
        );

        var result = await _rollbackHandler.HandleAsync(command, context.CancellationToken);
        return new OperationResult { Ok = result.Ok, Message = result.Message };
    }

    public override async Task<GetBalanceResponse> GetBalance(
        GetBalanceRequest request,
        ServerCallContext context)
    {
        var query = new GetBalanceQuery(request.PlayerId);
        var result = await _getBalanceHandler.HandleAsync(query, context.CancellationToken);

        var response = new GetBalanceResponse();
        foreach (var item in result.Items)
        {
            response.Items.Add(new BalanceItem
            {
                Currency = item.Currency,
                Network = item.Network,
                BalanceMinor = item.BalanceMinor,
                ReservedMinor = item.ReservedMinor,
                CashableMinor = item.CashableMinor
            });
        }

        return response;
    }
}

