namespace Magenta.Payment.Application.DTOs;

public class CreateWithdrawalRequest
{
    public long PlayerId { get; set; }
    public int ProviderId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long AmountMinor { get; set; }
    public string TargetAddress { get; set; } = string.Empty;
}
