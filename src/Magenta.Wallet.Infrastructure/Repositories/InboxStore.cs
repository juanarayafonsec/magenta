using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class InboxStore : IInboxStore
{
    private readonly WalletDbContext _context;

    public InboxStore(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryRecordInboxEventAsync(string source, string idempotencyKey, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var inboxEvent = new InboxEvent
            {
                Source = source,
                IdempotencyKeyValue = idempotencyKey,
                Payload = payload
            };
            _context.InboxEvents.Add(inboxEvent);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") == true || 
                                           ex.InnerException?.Message?.Contains("unique") == true)
        {
            // Duplicate key - event already processed
            return false;
        }
    }

    public async Task MarkAsProcessedAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
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

