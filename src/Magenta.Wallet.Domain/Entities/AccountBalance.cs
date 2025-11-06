namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Derived balance table - cached snapshot for fast reads.
/// Updated atomically with ledger postings in the same transaction.
/// </summary>
public class AccountBalance
{
    public long AccountId { get; set; }
    public long BalanceMinor { get; set; } = 0;
    public long ReservedMinor { get; set; } = 0;
    public long CashableMinor { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Account Account { get; set; } = null!;
}




