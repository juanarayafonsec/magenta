using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class LedgerWriter : ILedgerWriter
{
    private readonly WalletDbContext _context;

    public LedgerWriter(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        return new DbTransactionWrapper(transaction);
    }

    public async Task<Account> EnsureAccountAsync(long playerId, int currencyNetworkId, AccountType accountType, CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.CurrencyNetworkId == currencyNetworkId && a.AccountType == accountType, cancellationToken);

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

        return account;
    }

    public async Task<AccountBalance> EnsureAccountBalanceAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var balance = await _context.AccountBalances
            .FirstOrDefaultAsync(b => b.AccountId == accountId, cancellationToken);

        if (balance == null)
        {
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

    public async Task<Guid> WriteTransactionAsync(
        TxType txType,
        string? externalRef,
        JsonDocument metadata,
        List<(long AccountId, Direction Direction, long AmountMinor)> postings,
        Dictionary<long, AccountBalance> balanceUpdates,
        CancellationToken cancellationToken = default)
    {
        var txId = Guid.NewGuid();
        var transaction = new LedgerTransaction
        {
            TxId = txId,
            TxType = txType,
            ExternalRef = externalRef,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };

        _context.LedgerTransactions.Add(transaction);

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
        }

        // Lock and update account balances
        foreach (var (accountId, balance) in balanceUpdates)
        {
            var existingBalance = await _context.AccountBalances
                .FromSqlRaw($"SELECT * FROM account_balances WHERE account_id = {accountId} FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingBalance == null)
            {
                _context.AccountBalances.Add(balance);
            }
            else
            {
                existingBalance.BalanceMinor = balance.BalanceMinor;
                existingBalance.ReservedMinor = balance.ReservedMinor;
                existingBalance.CashableMinor = balance.CashableMinor;
                existingBalance.UpdatedAt = balance.UpdatedAt;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return txId;
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

    public async Task AddOutboxEventAsync(string eventType, string routingKey, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var outboxEvent = new OutboxEvent
        {
            EventType = eventType,
            RoutingKey = routingKey,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        };
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private class DbTransactionWrapper : IDbTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public DbTransactionWrapper(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}

