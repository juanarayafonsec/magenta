namespace Magenta.Payment.Application.DTOs;

public class CreateDepositSessionRequest
{
    public long PlayerId { get; set; }
    public int ProviderId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long? ExpectedAmountMinor { get; set; }
    public int ConfirmationsRequired { get; set; } = 1;
}
