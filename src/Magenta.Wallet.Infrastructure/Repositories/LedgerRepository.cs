using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly WalletDbContext _context;

    public LedgerRepository(WalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<LedgerTransaction?> GetTransactionByReferenceAsync(string source, string referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerTransactions
            .Include(t => t.Postings)
            .ThenInclude(p => p.Account)
            .FirstOrDefaultAsync(t => t.Source == source && t.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<Guid> CreateTransactionAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.LedgerTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction.LedgerTransactionId;
    }

    public async Task<List<LedgerPosting>> GetAccountPostingsAsync(long accountId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerPostings
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.LedgerPostingId)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CalculateAccountBalanceAsync(long accountId, CancellationToken cancellationToken = default)
    {
        // Calculate balance: CR - DR
        var debits = await _context.LedgerPostings
            .Where(p => p.AccountId == accountId && p.Direction == "DR")
            .SumAsync(p => (long?)p.AmountMinor, cancellationToken) ?? 0;

        var credits = await _context.LedgerPostings
            .Where(p => p.AccountId == accountId && p.Direction == "CR")
            .SumAsync(p => (long?)p.AmountMinor, cancellationToken) ?? 0;

        return credits - debits;
    }
}
