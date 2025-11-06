namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Join table linking currencies to networks (e.g., USDT on TRON).
/// </summary>
public class CurrencyNetwork
{
    public int CurrencyNetworkId { get; set; }
    public int CurrencyId { get; set; }
    public int NetworkId { get; set; }
    public string? TokenContract { get; set; }
    public long WithdrawalFeeMinor { get; set; } = 0;
    public long MinDepositMinor { get; set; } = 0;
    public long MinWithdrawMinor { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    public Currency Currency { get; set; } = null!;
    public Network Network { get; set; } = null!;
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}




