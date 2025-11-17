using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly WalletDbContext _context;

    public IdempotencyService(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsDuplicateAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyKeys
            .AnyAsync(k => k.Source == source && k.IdempotencyKeyValue == idempotencyKey, cancellationToken);
    }

    public async Task RecordIdempotencyKeyAsync(string source, string idempotencyKey, Guid txId, CancellationToken cancellationToken = default)
    {
        var key = new IdempotencyKey
        {
            Source = source,
            IdempotencyKeyValue = idempotencyKey,
            TxId = txId,
            CreatedAt = DateTime.UtcNow
        };
        _context.IdempotencyKeys.Add(key);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

