using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Payments.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly PaymentsDbContext _context;

    public OutboxRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task AddEventAsync(
        string eventType,
        string routingKey,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        var outboxEvent = new OutboxEvent
        {
            EventType = eventType,
            RoutingKey = routingKey,
            Payload = payloadJson,
            CreatedAt = DateTime.UtcNow
        };
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxEvent>> GetUnpublishedEventsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var events = await _context.OutboxEvents
            .Where(e => e.PublishedAt == null)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return events.Select(e => new OutboxEvent(
            e.Id,
            e.EventType,
            e.RoutingKey,
            System.Text.Json.JsonSerializer.Deserialize<object>(e.Payload.RootElement.GetRawText()) ?? new object(),
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

