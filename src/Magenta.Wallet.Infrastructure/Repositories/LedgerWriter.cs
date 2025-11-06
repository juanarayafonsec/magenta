using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class LedgerWriter : ILedgerWriter
{
    private readonly WalletDbContext _context;
    private IDbContextTransaction? _transaction;

    public LedgerWriter(WalletDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started");

        _transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to commit");

        await _context.SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to rollback");

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task<Guid> CreateLedgerTransactionAsync(
        string txType,
        string? externalRef,
        Dictionary<string, object> metadata,
        List<(long AccountId, Direction Direction, long AmountMinor)> postings,
        CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Transaction not started");

        var txId = Guid.NewGuid();
        var metadataJson = JsonSerializer.SerializeToDocument(metadata);

        var transaction = new LedgerTransaction
        {
            TxId = txId,
            TxType = txType,
            ExternalRef = externalRef,
            Metadata = metadataJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.LedgerTransactions.Add(transaction);

        var affectedAccountIds = new HashSet<long>();
        var accountBalances = new Dictionary<long, AccountBalance>();

        foreach (var (accountId, direction, amountMinor) in postings)
        {
            var posting = new LedgerPosting
            {
                TxId = txId,
                AccountId = accountId,
                Direction = direction,
                AmountMinor = amountMinor,
                CreatedAt = DateTime.UtcNow
            };

            _context.LedgerPostings.Add(posting);
            affectedAccountIds.Add(accountId);
        }

        // Verify DR/CR balance
        var totalDebit = postings.Where(p => p.Direction == Direction.DEBIT).Sum(p => p.AmountMinor);
        var totalCredit = postings.Where(p => p.Direction == Direction.CREDIT).Sum(p => p.AmountMinor);
        
        if (totalDebit != totalCredit)
            throw new InvalidOperationException($"Unbalanced transaction: DR={totalDebit}, CR={totalCredit}");

        // Load and lock balances with FOR UPDATE
        foreach (var accountId in affectedAccountIds)
        {
            var balance = await _context.AccountBalances
                .Where(ab => ab.AccountId == accountId)
                .FirstOrDefaultAsync(cancellationToken);

            if (balance == null)
            {
                // Ensure account exists
                var account = await _context.Accounts.FindAsync(new object[] { accountId }, cancellationToken);
                if (account == null)
                    throw new InvalidOperationException($"Account {accountId} not found");

                balance = new AccountBalance
                {
                    AccountId = accountId,
                    BalanceMinor = 0,
                    ReservedMinor = 0,
                    CashableMinor = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.AccountBalances.Add(balance);
            }

            accountBalances[accountId] = balance;
        }

        // Apply postings to balances
        foreach (var (accountId, direction, amountMinor) in postings)
        {
            var balance = accountBalances[accountId];
            DerivedBalanceUpdater.ApplyPosting(balance, direction, amountMinor);
        }

        // Recompute reserved and cashable for MAIN accounts
        await RecomputeReservedAndCashableAsync(affectedAccountIds, cancellationToken);

        return txId;
    }

    private async Task RecomputeReservedAndCashableAsync(HashSet<long> affectedAccountIds, CancellationToken cancellationToken)
    {
        // Find all MAIN accounts in affected account IDs
        var mainAccounts = await _context.Accounts
            .Where(a => affectedAccountIds.Contains(a.AccountId) && a.AccountType == AccountType.MAIN)
            .ToListAsync(cancellationToken);

        foreach (var mainAccount in mainAccounts)
        {
            var mainBalance = await _context.AccountBalances
                .Where(ab => ab.AccountId == mainAccount.AccountId)
                .FirstOrDefaultAsync(cancellationToken);

            if (mainBalance == null)
                continue;

            // Find corresponding WITHDRAW_HOLD account
            var withdrawHoldAccount = await _context.Accounts
                .Where(a => a.PlayerId == mainAccount.PlayerId &&
                           a.CurrencyNetworkId == mainAccount.CurrencyNetworkId &&
                           a.AccountType == AccountType.WITHDRAW_HOLD)
                .FirstOrDefaultAsync(cancellationToken);

            if (withdrawHoldAccount != null)
            {
                var withdrawHoldBalance = await _context.AccountBalances
                    .Where(ab => ab.AccountId == withdrawHoldAccount.AccountId)
                    .FirstOrDefaultAsync(cancellationToken);

                DerivedBalanceUpdater.UpdateReservedFromWithdrawHold(
                    mainBalance, 
                    withdrawHoldBalance);
            }
            else
            {
                mainBalance.ReservedMinor = 0;
                mainBalance.CashableMinor = mainBalance.BalanceMinor;
                mainBalance.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    public async Task<long> EnsureAccountAsync(
        long playerId,
        int currencyNetworkId,
        AccountType accountType,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .Where(a => a.PlayerId == playerId &&
                       a.CurrencyNetworkId == currencyNetworkId &&
                       a.AccountType == accountType)
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            account = new Account
            {
                PlayerId = playerId,
                CurrencyNetworkId = currencyNetworkId,
                AccountType = accountType,
                Status = "ACTIVE"
            };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Ensure balance exists
        var balance = await _context.AccountBalances
            .Where(ab => ab.AccountId == account.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (balance == null)
        {
            balance = new AccountBalance
            {
                AccountId = account.AccountId,
                BalanceMinor = 0,
                ReservedMinor = 0,
                CashableMinor = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.AccountBalances.Add(balance);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return account.AccountId;
    }

    public async Task<AccountBalance> EnsureAndLockAccountBalanceAsync(
        long accountId,
        CancellationToken cancellationToken = default)
    {
        var balance = await _context.AccountBalances
            .Where(ab => ab.AccountId == accountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (balance == null)
        {
            var account = await _context.Accounts.FindAsync(new object[] { accountId }, cancellationToken);
            if (account == null)
                throw new InvalidOperationException($"Account {accountId} not found");

            balance = new AccountBalance
            {
                AccountId = accountId,
                BalanceMinor = 0,
                ReservedMinor = 0,
                CashableMinor = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.AccountBalances.Add(balance);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return balance;
    }

    public async Task UpdateAccountBalanceAsync(
        AccountBalance balance,
        CancellationToken cancellationToken = default)
    {
        _context.AccountBalances.Update(balance);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordIdempotencyKeyAsync(
        string source,
        string idempotencyKey,
        Guid txId,
        CancellationToken cancellationToken = default)
    {
        var key = new IdempotencyKey
        {
            Source = source,
            IdempotencyKeyValue = idempotencyKey,
            TxId = txId,
            CreatedAt = DateTime.UtcNow
        };

        _context.IdempotencyKeys.Add(key);
    }

    public async Task<bool> IdempotencyKeyExistsAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyKeys
            .AnyAsync(ik => ik.Source == source && ik.IdempotencyKeyValue == idempotencyKey, cancellationToken);
    }

    public async Task<Guid?> GetTransactionIdByKeyAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = await _context.IdempotencyKeys
            .Where(ik => ik.Source == source && ik.IdempotencyKeyValue == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);

        return key?.TxId;
    }

    public async Task AddOutboxEventAsync(
        string eventType,
        string routingKey,
        Dictionary<string, object> payload,
        CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.SerializeToDocument(payload);

        var outboxEvent = new OutboxEvent
        {
            EventType = eventType,
            RoutingKey = routingKey,
            Payload = payloadJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxEvents.Add(outboxEvent);
    }
}




