namespace Magenta.Wallet.Application.DTOs;

public class FinalizeWithdrawalFailedRequest
{
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Source { get; set; } = "payments";
}
