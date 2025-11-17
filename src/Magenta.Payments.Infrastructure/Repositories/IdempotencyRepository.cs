using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payments.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly PaymentsDbContext _context;

    public IdempotencyRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryRecordIdempotencyKeyAsync(
        string source,
        string idempotencyKey,
        Guid? relatedId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = new IdempotencyKey
            {
                Source = source,
                IdempotencyKeyValue = idempotencyKey,
                RelatedId = relatedId,
                CreatedAt = DateTime.UtcNow
            };
            _context.IdempotencyKeys.Add(key);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Duplicate key - already exists
            return false;
        }
    }

    public async Task<Guid?> GetRelatedIdAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Source == source && k.IdempotencyKeyValue == idempotencyKey, cancellationToken);
        
        return key?.RelatedId;
    }
}

