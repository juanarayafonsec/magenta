using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
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

    public async Task<bool> TryRecordEventAsync(
        string source,
        string idempotencyKey,
        Dictionary<string, object> payload,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.InboxEvents
            .AnyAsync(ie => ie.Source == source && ie.IdempotencyKey == idempotencyKey, cancellationToken);

        if (exists)
            return false;

        var payloadJson = JsonSerializer.SerializeToDocument(payload);

        var inboxEvent = new InboxEvent
        {
            Source = source,
            IdempotencyKey = idempotencyKey,
            Payload = payloadJson
        };

        _context.InboxEvents.Add(inboxEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task MarkProcessedAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var inboxEvent = await _context.InboxEvents
            .Where(ie => ie.Source == source && ie.IdempotencyKey == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (inboxEvent != null)
        {
            inboxEvent.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}




