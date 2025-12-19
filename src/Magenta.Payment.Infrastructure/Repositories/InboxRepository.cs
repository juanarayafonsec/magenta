using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly PaymentDbContext _context;

    public InboxRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InboxEvent?> GetBySourceAndKeyAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.InboxEvents
            .FirstOrDefaultAsync(e => e.Source == source && e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<InboxEvent> CreateAsync(InboxEvent evt, CancellationToken cancellationToken = default)
    {
        _context.InboxEvents.Add(evt);
        await _context.SaveChangesAsync(cancellationToken);
        return evt;
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

    public async Task MarkAsFailedAsync(long inboxEventId, string error, CancellationToken cancellationToken = default)
    {
        var evt = await _context.InboxEvents.FindAsync(new object[] { inboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.LastError = error;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
