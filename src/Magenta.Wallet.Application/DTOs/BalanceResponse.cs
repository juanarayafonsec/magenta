namespace Magenta.Wallet.Application.DTOs;

public class BalanceResponse
{
    public List<CurrencyBalanceDto> Balances { get; set; } = new();
}

public class CurrencyBalanceDto
{
    public int CurrencyNetworkId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long BalanceMinor { get; set; }
    public int Decimals { get; set; }
}
