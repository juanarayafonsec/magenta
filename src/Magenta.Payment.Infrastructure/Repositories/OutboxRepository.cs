using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly PaymentDbContext _context;

    public OutboxRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateAsync(OutboxEvent evt, CancellationToken cancellationToken = default)
    {
        _context.OutboxEvents.Add(evt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxEvent>> GetUnpublishedEventsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .Where(e => e.PublishedAt == null)
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsPublishedAsync(long outboxEventId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.OutboxEvents.FindAsync(new object[] { outboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.PublishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementPublishAttemptsAsync(long outboxEventId, string? error, CancellationToken cancellationToken = default)
    {
        var evt = await _context.OutboxEvents.FindAsync(new object[] { outboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.PublishAttempts++;
            evt.LastError = error;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
