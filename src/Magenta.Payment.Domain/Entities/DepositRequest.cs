using System.Text.Json;

namespace Magenta.Payment.Domain.Entities;

public class DepositRequest
{
    public Guid DepositId { get; set; }
    public Guid? SessionId { get; set; }
    public long PlayerId { get; set; }
    public int ProviderId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public string TxHash { get; set; } = string.Empty; // UNIQUE
    public long AmountMinor { get; set; }
    public int ConfirmationsReceived { get; set; } = 0;
    public int ConfirmationsRequired { get; set; } = 1;
    public string Status { get; set; } = "PENDING"; // PENDING, CONFIRMED, SETTLED, FAILED
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
