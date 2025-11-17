namespace Magenta.Wallet.Infrastructure.Data;

public class IdempotencyKey
{
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKeyValue { get; set; } = string.Empty;
    public Guid TxId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

