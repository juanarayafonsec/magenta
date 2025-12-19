using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IWithdrawalRequestRepository
{
    Task<WithdrawalRequest?> GetByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default);
    Task<WithdrawalRequest> CreateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<List<WithdrawalRequest>> GetProcessingAsync(CancellationToken cancellationToken = default);
    Task<List<WithdrawalRequest>> GetBroadcastedAsync(CancellationToken cancellationToken = default);
}
