namespace Magenta.Wallet.Domain.Entities;

public class CurrencyNetwork
{
    public int CurrencyNetworkId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public int Decimals { get; set; }
}
