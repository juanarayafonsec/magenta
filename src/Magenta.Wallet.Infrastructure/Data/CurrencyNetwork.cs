namespace Magenta.Wallet.Infrastructure.Data;

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
}

