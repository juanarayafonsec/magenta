using System.Text.Json;

namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Inbox event for deduplicating consumed events.
/// </summary>
public class InboxEvent
{
    public long Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public DateTime? ProcessedAt { get; set; }
}




