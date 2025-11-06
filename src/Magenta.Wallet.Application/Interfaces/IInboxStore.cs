namespace Magenta.Wallet.Application.Interfaces;

/// <summary>
/// Interface for inbox event storage (deduplication).
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Records an inbox event if it doesn't already exist (by source + idempotency_key).
    /// Returns true if inserted, false if already exists.
    /// </summary>
    Task<bool> TryRecordEventAsync(
        string source,
        string idempotencyKey,
        Dictionary<string, object> payload,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks an inbox event as processed.
    /// </summary>
    Task MarkProcessedAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}




