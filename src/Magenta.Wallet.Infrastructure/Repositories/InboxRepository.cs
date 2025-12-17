using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly WalletDbContext _context;

    public InboxRepository(WalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistsAsync(string source, string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.InboxEvents
            .AnyAsync(e => e.Source == source && e.MessageId == messageId, cancellationToken);
    }

    public async Task CreateInboxEventAsync(InboxEvent inboxEvent, CancellationToken cancellationToken = default)
    {
        _context.InboxEvents.Add(inboxEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<InboxEvent>> GetUnprocessedEventsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.InboxEvents
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(long inboxEventId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.InboxEvents.FindAsync(new object[] { inboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(long inboxEventId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var evt = await _context.InboxEvents.FindAsync(new object[] { inboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.ErrorMessage = errorMessage;
            evt.ProcessedAt = DateTime.UtcNow; // Mark as processed even if failed
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
