using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IDepositSessionRepository
{
    Task<DepositSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<DepositSession> CreateAsync(DepositSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(DepositSession session, CancellationToken cancellationToken = default);
    Task<List<DepositSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
