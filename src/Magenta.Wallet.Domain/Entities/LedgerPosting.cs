namespace Magenta.Wallet.Domain.Entities;

public class LedgerPosting
{
    public long PostingId { get; set; }
    public Guid TxId { get; set; }
    public long AccountId { get; set; }
    public Enums.Direction Direction { get; set; }
    public long AmountMinor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

