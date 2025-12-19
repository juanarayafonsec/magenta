namespace Magenta.Payment.Application.DTOs;

public class CreateDepositSessionResponse
{
    public Guid SessionId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? MemoOrTag { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
