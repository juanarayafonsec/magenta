using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
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

    public async Task<List<OutboxEventDto>> GetUnpublishedEventsAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.OutboxEvents
            .Where(oe => oe.PublishedAt == null)
            .OrderBy(oe => oe.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return events.Select(e => new OutboxEventDto
        {
            Id = e.Id,
            EventType = e.EventType,
            RoutingKey = e.RoutingKey,
            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(e.Payload.RootElement) ?? new(),
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    public async Task MarkPublishedAsync(
        long outboxEventId,
        CancellationToken cancellationToken = default)
    {
        var outboxEvent = await _context.OutboxEvents
            .FindAsync(new object[] { outboxEventId }, cancellationToken);

        if (outboxEvent != null)
        {
            outboxEvent.PublishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}




