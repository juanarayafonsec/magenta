using System.Text.Json;

namespace Magenta.Wallet.Domain.Entities;

public class InboxEvent
{
    public long InboxEventId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = null!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
