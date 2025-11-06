namespace Magenta.Wallet.Application.DTOs.Commands;

public class SettleWinCommand
{
    public long PlayerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string WinId { get; set; } = string.Empty; // idempotency key
    public string BetId { get; set; } = string.Empty;
    public string RoundId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}




