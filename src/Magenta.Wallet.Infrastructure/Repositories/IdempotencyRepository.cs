using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly WalletDbContext _context;

    public IdempotencyRepository(WalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistsAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyKeys
            .AnyAsync(k => k.Source == source && k.IdempotencyKeyValue == idempotencyKey, cancellationToken);
    }

    public async Task<Guid?> GetTransactionIdAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Source == source && k.IdempotencyKeyValue == idempotencyKey, cancellationToken);
        
        return key?.TransactionId;
    }

    public async Task CreateIdempotencyKeyAsync(string source, string idempotencyKey, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var key = new IdempotencyKey
        {
            Source = source,
            IdempotencyKeyValue = idempotencyKey,
            TransactionId = transactionId
        };
        
        _context.IdempotencyKeys.Add(key);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
