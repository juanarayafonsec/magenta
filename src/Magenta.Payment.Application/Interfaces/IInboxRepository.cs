using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IInboxRepository
{
    Task<InboxEvent?> GetBySourceAndKeyAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InboxEvent> CreateAsync(InboxEvent evt, CancellationToken cancellationToken = default);
    Task<List<InboxEvent>> GetUnprocessedEventsAsync(int limit, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(long inboxEventId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(long inboxEventId, string error, CancellationToken cancellationToken = default);
}
