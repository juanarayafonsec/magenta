namespace Magenta.Payment.Application.DTOs;

public class WithdrawalResult
{
    public bool Success { get; set; }
    public string? ProviderReference { get; set; }
    public string? TxHash { get; set; }
    public string? ErrorMessage { get; set; }
}
