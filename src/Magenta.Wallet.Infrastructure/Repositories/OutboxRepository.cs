using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly WalletDbContext _context;

    public OutboxRepository(WalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateOutboxEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxEvent>> GetPendingEventsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == "PENDING")
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSentAsync(long outboxEventId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.OutboxEvents.FindAsync(new object[] { outboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.Status = "SENT";
            evt.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(long outboxEventId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var evt = await _context.OutboxEvents.FindAsync(new object[] { outboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.Status = "FAILED";
            evt.ErrorMessage = errorMessage;
            evt.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementRetryCountAsync(long outboxEventId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.OutboxEvents.FindAsync(new object[] { outboxEventId }, cancellationToken);
        if (evt != null)
        {
            evt.RetryCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
