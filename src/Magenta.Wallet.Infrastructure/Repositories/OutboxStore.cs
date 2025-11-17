using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class OutboxStore : IOutboxStore
{
    private readonly WalletDbContext _context;

    public OutboxStore(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<List<OutboxEventDto>> GetUnpublishedEventsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var events = await _context.OutboxEvents
            .Where(e => e.PublishedAt == null)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return events.Select(e => new OutboxEventDto(
            e.Id,
            e.EventType,
            e.RoutingKey,
            e.Payload,
            e.CreatedAt
        )).ToList();
    }

    public async Task MarkAsPublishedAsync(long eventId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.OutboxEvents.FindAsync(new object[] { eventId }, cancellationToken);
        if (evt != null)
        {
            evt.PublishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

