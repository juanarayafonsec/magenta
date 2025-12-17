using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface IOutboxRepository
{
    Task CreateOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<List<OutboxEvent>> GetPendingEventsAsync(int limit, CancellationToken cancellationToken = default);
    Task MarkAsSentAsync(long outboxEventId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(long outboxEventId, string errorMessage, CancellationToken cancellationToken = default);
    Task IncrementRetryCountAsync(long outboxEventId, CancellationToken cancellationToken = default);
}
