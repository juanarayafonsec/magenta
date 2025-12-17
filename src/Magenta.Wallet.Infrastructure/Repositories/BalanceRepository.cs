using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class BalanceRepository : IBalanceRepository
{
    private readonly WalletDbContext _context;
    private readonly ILedgerRepository _ledgerRepository;

    public BalanceRepository(WalletDbContext context, ILedgerRepository ledgerRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _ledgerRepository = ledgerRepository ?? throw new ArgumentNullException(nameof(ledgerRepository));
    }

    public async Task UpdateDerivedBalanceAsync(long accountId, long balanceMinor, CancellationToken cancellationToken = default)
    {
        var balance = await _context.AccountBalancesDerived
            .FirstOrDefaultAsync(b => b.AccountId == accountId, cancellationToken);

        if (balance == null)
        {
            balance = new AccountBalanceDerived
            {
                AccountId = accountId,
                BalanceMinor = balanceMinor
            };
            _context.AccountBalancesDerived.Add(balance);
        }
        else
        {
            balance.BalanceMinor = balanceMinor;
            balance.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long?> GetDerivedBalanceAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var balance = await _context.AccountBalancesDerived
            .FirstOrDefaultAsync(b => b.AccountId == accountId, cancellationToken);
        
        return balance?.BalanceMinor;
    }

    public async Task<Dictionary<long, long>> GetPlayerBalancesAsync(long playerId, CancellationToken cancellationToken = default)
    {
        // Get all MAIN accounts for the player
        var mainAccounts = await _context.Accounts
            .Where(a => a.PlayerId == playerId && a.AccountType == "MAIN")
            .ToListAsync(cancellationToken);

        var balances = new Dictionary<long, long>();
        
        foreach (var account in mainAccounts)
        {
            // Calculate actual balance from ledger
            var actualBalance = await _ledgerRepository.CalculateAccountBalanceAsync(account.AccountId, cancellationToken);
            
            // Update derived balance
            await UpdateDerivedBalanceAsync(account.AccountId, actualBalance, cancellationToken);
            
            balances[account.AccountId] = actualBalance;
        }

        return balances;
    }
}
