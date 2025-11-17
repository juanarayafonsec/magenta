namespace Magenta.Wallet.Domain.Entities;

public class AccountBalance
{
    public long AccountId { get; set; }
    public long BalanceMinor { get; set; }
    public long ReservedMinor { get; set; }
    public long CashableMinor { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

