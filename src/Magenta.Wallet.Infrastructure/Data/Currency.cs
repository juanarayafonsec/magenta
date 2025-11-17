namespace Magenta.Wallet.Infrastructure.Data;

public class Currency
{
    public int CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Decimals { get; set; }
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

