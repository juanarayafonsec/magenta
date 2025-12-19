using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IIdempotencyRepository
{
    Task<bool> ExistsAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Guid?> GetTxIdAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
    Task CreateAsync(IdempotencyKey key, CancellationToken cancellationToken = default);
}
