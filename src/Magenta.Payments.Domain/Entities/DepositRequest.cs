using System.Text.Json;

namespace Magenta.Payments.Domain.Entities;

public class DepositRequest
{
    public Guid DepositId { get; set; }
    public Guid? SessionId { get; set; }
    public long PlayerId { get; set; }
    public int ProviderId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public int ConfirmationsReceived { get; set; } = 0;
    public int ConfirmationsRequired { get; set; } = 1;
    public Enums.DepositRequestStatus? Status { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

