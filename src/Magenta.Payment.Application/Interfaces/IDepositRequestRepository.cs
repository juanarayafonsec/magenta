using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IDepositRequestRepository
{
    Task<DepositRequest?> GetByIdAsync(Guid depositId, CancellationToken cancellationToken = default);
    Task<DepositRequest?> GetByTxHashAsync(string txHash, CancellationToken cancellationToken = default);
    Task<DepositRequest> CreateAsync(DepositRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(DepositRequest request, CancellationToken cancellationToken = default);
    Task<List<DepositRequest>> GetPendingVerificationAsync(CancellationToken cancellationToken = default);
    Task<List<DepositRequest>> GetConfirmedPendingSettlementAsync(CancellationToken cancellationToken = default);
}
