namespace Magenta.Wallet.Domain.Entities;

public class AccountBalanceDerived
{
    public long AccountBalanceDerivedId { get; set; }
    public long AccountId { get; set; }
    public Account? Account { get; set; }
    public long BalanceMinor { get; set; } // Derived balance in minor units
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
