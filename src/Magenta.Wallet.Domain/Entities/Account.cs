namespace Magenta.Wallet.Domain.Entities;

public class Account
{
    public long AccountId { get; set; }
    public long? PlayerId { get; set; } // null for house accounts
    public string AccountType { get; set; } = string.Empty; // MAIN, WITHDRAW_HOLD, HOUSE, etc.
    public int CurrencyNetworkId { get; set; }
    public CurrencyNetwork? CurrencyNetwork { get; set; }
}
