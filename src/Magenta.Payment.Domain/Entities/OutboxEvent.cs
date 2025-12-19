using System.Text.Json;

namespace Magenta.Payment.Domain.Entities;

public class OutboxEvent
{
    public long OutboxEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int PublishAttempts { get; set; } = 0;
    public string? LastError { get; set; }
}
