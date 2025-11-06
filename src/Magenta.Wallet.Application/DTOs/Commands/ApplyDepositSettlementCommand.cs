namespace Magenta.Wallet.Application.DTOs.Commands;

public class ApplyDepositSettlementCommand
{
    public long PlayerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}




