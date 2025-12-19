using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly PaymentDbContext _context;

    public IdempotencyRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistsAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyKeys
            .AnyAsync(k => k.Source == source && k.IdempotencyKeyValue == idempotencyKey, cancellationToken);
    }

    public async Task<Guid?> GetTxIdAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Source == source && k.IdempotencyKeyValue == idempotencyKey, cancellationToken);
        return key?.TxId;
    }

    public async Task CreateAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
    {
        _context.IdempotencyKeys.Add(key);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
