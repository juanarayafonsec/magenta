namespace Magenta.Wallet.Application.DTOs;

public class CurrencyNetworkDto
{
    public int CurrencyNetworkId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public int Decimals { get; set; }
    public string? Symbol { get; set; }
}
