using System.Text.Json;

namespace Magenta.Payment.Domain.Entities;

public class WithdrawalRequest
{
    public Guid WithdrawalId { get; set; }
    public long PlayerId { get; set; }
    public int ProviderId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long AmountMinor { get; set; }
    public long FeeMinor { get; set; } = 0;
    public string TargetAddress { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? TxHash { get; set; }
    public string Status { get; set; } = "REQUESTED"; // REQUESTED, PROCESSING, BROADCASTED, SETTLED, FAILED
    public string? FailReason { get; set; }
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
