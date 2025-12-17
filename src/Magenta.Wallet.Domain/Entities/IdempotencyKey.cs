namespace Magenta.Wallet.Domain.Entities;

public class IdempotencyKey
{
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKeyValue { get; set; } = string.Empty;
    public Guid? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
