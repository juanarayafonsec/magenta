using System.Text.Json;

namespace Magenta.Payments.Domain.Entities;

public class DepositSession
{
    public Guid SessionId { get; set; }
    public long PlayerId { get; set; }
    public int ProviderId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? MemoOrTag { get; set; }
    public string? ProviderReference { get; set; }
    public long? ExpectedAmountMinor { get; set; }
    public long? MinAmountMinor { get; set; }
    public int ConfirmationsRequired { get; set; } = 1;
    public Enums.DepositSessionStatus Status { get; set; } = Enums.DepositSessionStatus.OPEN;
    public DateTime? ExpiresAt { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

