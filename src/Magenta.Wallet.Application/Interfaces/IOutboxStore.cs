using System.Text.Json;

namespace Magenta.Wallet.Application.Interfaces;

public interface IOutboxStore
{
    Task<List<OutboxEventDto>> GetUnpublishedEventsAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsPublishedAsync(long eventId, CancellationToken cancellationToken = default);
}

public record OutboxEventDto(
    long Id,
    string EventType,
    string RoutingKey,
    JsonDocument Payload,
    DateTime CreatedAt
);

