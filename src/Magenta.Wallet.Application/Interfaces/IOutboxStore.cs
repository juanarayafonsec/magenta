namespace Magenta.Wallet.Application.Interfaces;

/// <summary>
/// Interface for outbox event storage.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Gets all unpublished outbox events (ordered by created_at).
    /// </summary>
    Task<List<OutboxEventDto>> GetUnpublishedEventsAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks an outbox event as published.
    /// </summary>
    Task MarkPublishedAsync(
        long outboxEventId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for outbox event.
/// </summary>
public class OutboxEventDto
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}




