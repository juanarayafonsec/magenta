namespace Magenta.Wallet.Domain.Entities;

public class CurrencyCatalog
{
    public int CurrencyCatalogId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Decimals { get; set; }
    public string Symbol { get; set; } = string.Empty;
}
