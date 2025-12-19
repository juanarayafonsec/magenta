using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IOutboxRepository
{
    Task CreateAsync(OutboxEvent evt, CancellationToken cancellationToken = default);
    Task<List<OutboxEvent>> GetUnpublishedEventsAsync(int limit, CancellationToken cancellationToken = default);
    Task MarkAsPublishedAsync(long outboxEventId, CancellationToken cancellationToken = default);
    Task IncrementPublishAttemptsAsync(long outboxEventId, string? error, CancellationToken cancellationToken = default);
}
