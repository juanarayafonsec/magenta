using System.Text.Json;

namespace Magenta.Wallet.Domain.Entities;

public class OutboxEvent
{
    public long OutboxEventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = null!;
    public string Status { get; set; } = "PENDING"; // PENDING, SENT, FAILED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
}
