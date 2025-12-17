using Magenta.Wallet.Application.DTOs;

namespace Magenta.Wallet.Application.Interfaces;

public interface ILedgerService
{
    Task<OperationResult> ReserveWithdrawalAsync(ReserveWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> FinalizeWithdrawalSettledAsync(FinalizeWithdrawalSettledRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> FinalizeWithdrawalFailedAsync(FinalizeWithdrawalFailedRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> ApplyDepositSettlementAsync(DepositSettlementRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> PostBetAsync(BetRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> PostWinAsync(WinRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> RollbackTransactionAsync(RollbackRequest request, CancellationToken cancellationToken = default);
}
