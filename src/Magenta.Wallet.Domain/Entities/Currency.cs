namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Currency (e.g., USDT, BTC).
/// </summary>
public class Currency
{
    public int CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Decimals { get; set; } // 0-18
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    public ICollection<CurrencyNetwork> CurrencyNetworks { get; set; } = new List<CurrencyNetwork>();
}




