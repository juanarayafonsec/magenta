namespace Magenta.Wallet.Application.DTOs.Commands;

public class ReleaseWithdrawalCommand
{
    public long PlayerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string RequestId { get; set; } = string.Empty; // idempotency key
    public string? CorrelationId { get; set; }
}




