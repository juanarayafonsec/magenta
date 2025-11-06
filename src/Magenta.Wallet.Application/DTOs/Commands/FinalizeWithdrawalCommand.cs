namespace Magenta.Wallet.Application.DTOs.Commands;

public class FinalizeWithdrawalCommand
{
    public long PlayerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public long FeeMinor { get; set; }
    public string RequestId { get; set; } = string.Empty; // idempotency key
    public string TxHash { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}




