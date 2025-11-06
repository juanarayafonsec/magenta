namespace Magenta.Wallet.Application.DTOs.Commands;

public class RollbackCommand
{
    public long PlayerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty; // "BET" | "WIN"
    public string ReferenceId { get; set; } = string.Empty; // betId or winId
    public string RollbackId { get; set; } = string.Empty; // idempotency key
    public string Reason { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}




