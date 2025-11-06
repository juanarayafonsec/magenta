using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Single ledger posting (one DR or CR line).
/// </summary>
public class LedgerPosting
{
    public long PostingId { get; set; }
    public Guid TxId { get; set; }
    public long AccountId { get; set; }
    public Direction Direction { get; set; }
    public long AmountMinor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public LedgerTransaction Transaction { get; set; } = null!;
    public Account Account { get; set; } = null!;
}




