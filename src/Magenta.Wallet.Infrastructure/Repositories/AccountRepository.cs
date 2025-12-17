using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly WalletDbContext _context;

    public AccountRepository(WalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Account?> GetAccountAsync(long? playerId, int currencyNetworkId, string accountType, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.CurrencyNetwork)
            .FirstOrDefaultAsync(a => a.PlayerId == playerId 
                && a.CurrencyNetworkId == currencyNetworkId 
                && a.AccountType == accountType, cancellationToken);
    }

    public async Task<Account> GetOrCreateAccountAsync(long? playerId, int currencyNetworkId, string accountType, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountAsync(playerId, currencyNetworkId, accountType, cancellationToken);
        
        if (account == null)
        {
            account = new Account
            {
                PlayerId = playerId,
                CurrencyNetworkId = currencyNetworkId,
                AccountType = accountType
            };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);
            
            // Reload to get navigation properties
            account = await GetAccountAsync(playerId, currencyNetworkId, accountType, cancellationToken) 
                ?? throw new InvalidOperationException("Failed to create account");
        }
        
        return account;
    }

    public async Task<Account> LockAccountForUpdateAsync(long accountId, CancellationToken cancellationToken = default)
    {
        // Load the account - with SERIALIZABLE isolation level, this will lock the row
        // Note: In production with multiple pods, explicit SELECT FOR UPDATE is recommended
        // For now, SERIALIZABLE isolation provides the necessary protection
        var account = await _context.Accounts
            .Where(a => a.AccountId == accountId)
            .Include(a => a.CurrencyNetwork)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID {accountId} not found");
        }

        // Ensure the entity is tracked so changes are detected
        _context.Entry(account).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;

        return account;
    }

    public async Task<List<Account>> GetPlayerAccountsAsync(long playerId, int currencyNetworkId, CancellationToken cancellationToken = default)
    {
        var query = _context.Accounts
            .Include(a => a.CurrencyNetwork)
            .Where(a => a.PlayerId == playerId);
            
        if (currencyNetworkId > 0)
        {
            query = query.Where(a => a.CurrencyNetworkId == currencyNetworkId);
        }
        
        return await query.ToListAsync(cancellationToken);
    }
}
