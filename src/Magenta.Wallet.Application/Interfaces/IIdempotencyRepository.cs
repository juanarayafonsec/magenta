using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface IIdempotencyRepository
{
    Task<bool> ExistsAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Guid?> GetTransactionIdAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
    Task CreateIdempotencyKeyAsync(string source, string idempotencyKey, Guid transactionId, CancellationToken cancellationToken = default);
}
