using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Payments.Infrastructure.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly PaymentsDbContext _context;

    public InboxRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryRecordInboxEventAsync(
        string source,
        string idempotencyKey,
        object payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payloadJson = JsonDocument.Parse(JsonSerializer.Serialize(payload));
            var inboxEvent = new InboxEvent
            {
                Source = source,
                IdempotencyKeyValue = idempotencyKey,
                Payload = payloadJson
            };
            _context.InboxEvents.Add(inboxEvent);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Duplicate - already processed
            return false;
        }
    }

    public async Task MarkAsProcessedAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var evt = await _context.InboxEvents
            .FirstOrDefaultAsync(e => e.Source == source && e.IdempotencyKeyValue == idempotencyKey, cancellationToken);
        
        if (evt != null)
        {
            evt.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

