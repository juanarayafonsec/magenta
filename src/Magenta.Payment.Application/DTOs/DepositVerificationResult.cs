namespace Magenta.Payment.Application.DTOs;

public class DepositVerificationResult
{
    public bool IsValid { get; set; }
    public long AmountMinor { get; set; }
    public int CurrencyNetworkId { get; set; }
    public int Confirmations { get; set; }
    public string? ErrorMessage { get; set; }
}
