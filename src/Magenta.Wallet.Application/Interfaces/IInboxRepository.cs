using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface IInboxRepository
{
    Task<bool> ExistsAsync(string source, string messageId, CancellationToken cancellationToken = default);
    Task CreateInboxEventAsync(InboxEvent inboxEvent, CancellationToken cancellationToken = default);
    Task<List<InboxEvent>> GetUnprocessedEventsAsync(int limit, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(long inboxEventId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(long inboxEventId, string errorMessage, CancellationToken cancellationToken = default);
}
