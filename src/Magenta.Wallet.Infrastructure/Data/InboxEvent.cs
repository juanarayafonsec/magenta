using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Data;

public class InboxEvent
{
    public long Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKeyValue { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public DateTime? ProcessedAt { get; set; }
}

