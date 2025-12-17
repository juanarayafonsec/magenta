namespace Magenta.Wallet.Application.DTOs;

public class DepositSettlementRequest
{
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long AmountMinor { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public string Source { get; set; } = "payments";
}
