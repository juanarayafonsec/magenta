namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Idempotency key tracking for deduplication.
/// </summary>
public class IdempotencyKey
{
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKeyValue { get; set; } = string.Empty;
    public Guid TxId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}




