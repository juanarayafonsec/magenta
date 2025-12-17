namespace Magenta.Wallet.Application.DTOs;

public class WinRequest
{
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long AmountMinor { get; set; }
    public string WinId { get; set; } = string.Empty;
    public string Source { get; set; } = "game";
}
