namespace Magenta.Wallet.Application.DTOs;

public class ReserveWithdrawalRequest
{
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long AmountMinor { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Source { get; set; } = "payments";
}
