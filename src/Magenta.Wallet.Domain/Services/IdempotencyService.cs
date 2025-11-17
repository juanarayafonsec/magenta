namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Domain service interface for idempotency checking.
/// Implementation will be in Infrastructure layer.
/// </summary>
public interface IIdempotencyService
{
    Task<bool> IsDuplicateAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
    Task RecordIdempotencyKeyAsync(string source, string idempotencyKey, Guid txId, CancellationToken cancellationToken = default);
}

