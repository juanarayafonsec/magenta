using System.Text.Json;

namespace Magenta.Payment.Domain.Entities;

public class InboxEvent
{
    public long InboxEventId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = null!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}
