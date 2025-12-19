namespace Magenta.Payment.Application.DTOs;

public class DepositSessionResult
{
    public string Address { get; set; } = string.Empty;
    public string? MemoOrTag { get; set; }
    public string? ProviderReference { get; set; }
    public DateTime ExpiresAt { get; set; }
}
