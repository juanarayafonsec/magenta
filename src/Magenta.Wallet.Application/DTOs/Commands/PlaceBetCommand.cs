namespace Magenta.Wallet.Application.DTOs.Commands;

public class PlaceBetCommand
{
    public long PlayerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string BetId { get; set; } = string.Empty; // idempotency key
    public string Provider { get; set; } = string.Empty;
    public string RoundId { get; set; } = string.Empty;
    public string GameCode { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}




