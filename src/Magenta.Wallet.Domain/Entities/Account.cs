using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Account entity. Unique on (player_id, currency_network_id, account_type).
/// </summary>
public class Account
{
    public long AccountId { get; set; }
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public AccountType AccountType { get; set; }
    public string Status { get; set; } = "ACTIVE";
    
    public CurrencyNetwork CurrencyNetwork { get; set; } = null!;
    public ICollection<LedgerPosting> Postings { get; set; } = new List<LedgerPosting>();
    public AccountBalance? Balance { get; set; }
}




