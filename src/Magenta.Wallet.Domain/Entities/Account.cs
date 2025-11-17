namespace Magenta.Wallet.Domain.Entities;

public class Account
{
    public long AccountId { get; set; }
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public Enums.AccountType AccountType { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

