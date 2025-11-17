using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Application.Handlers;

public abstract class BaseHandler
{
    protected readonly ILedgerWriter LedgerWriter;
    protected readonly ICurrencyNetworkResolver CurrencyNetworkResolver;
    protected readonly IIdempotencyService IdempotencyService;
    protected readonly WalletDbContext DbContext;

    protected BaseHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext)
    {
        LedgerWriter = ledgerWriter;
        CurrencyNetworkResolver = currencyNetworkResolver;
        IdempotencyService = idempotencyService;
        DbContext = dbContext;
    }

    protected async Task<Dictionary<long, AccountBalance>> GetOrCreateBalancesAsync(
        List<long> accountIds,
        CancellationToken cancellationToken)
    {
        var balances = new Dictionary<long, AccountBalance>();
        
        foreach (var accountId in accountIds)
        {
            var balance = await LedgerWriter.EnsureAccountBalanceAsync(accountId, cancellationToken);
            balances[accountId] = balance;
        }

        return balances;
    }

    protected async Task UpdateDerivedBalancesAsync(
        Dictionary<long, AccountBalance> balances,
        List<PostingRules.PostingRule> postings,
        CancellationToken cancellationToken)
    {
        // Apply postings to balances
        foreach (var posting in postings)
        {
            var balance = balances[posting.AccountId];
            if (posting.Direction == Direction.CREDIT)
                balance.BalanceMinor += posting.Amount.MinorUnits;
            else
                balance.BalanceMinor -= posting.Amount.MinorUnits;
        }

        // Get all accounts involved to compute reserved_minor
        var accountIds = balances.Keys.ToList();
        var accounts = await DbContext.Accounts
            .Where(a => accountIds.Contains(a.AccountId))
            .ToListAsync(cancellationToken);

        // Group by player to compute reserved_minor
        var playerGroups = accounts
            .Where(a => a.AccountType == AccountType.MAIN || a.AccountType == AccountType.WITHDRAW_HOLD)
            .GroupBy(a => a.PlayerId)
            .ToList();

        foreach (var playerGroup in playerGroups)
        {
            var playerId = playerGroup.Key;
            var playerAccounts = playerGroup.ToList();

            // Sum WITHDRAW_HOLD balances for this player
            long totalReserved = 0;
            foreach (var withdrawAccount in playerAccounts.Where(a => a.AccountType == AccountType.WITHDRAW_HOLD))
            {
                if (balances.TryGetValue(withdrawAccount.AccountId, out var withdrawBalance))
                {
                    totalReserved += withdrawBalance.BalanceMinor;
                }
            }

            // Update cashable_minor for MAIN accounts
            foreach (var mainAccount in playerAccounts.Where(a => a.AccountType == AccountType.MAIN))
            {
                if (balances.TryGetValue(mainAccount.AccountId, out var mainBalance))
                {
                    mainBalance.ReservedMinor = totalReserved;
                    mainBalance.CashableMinor = mainBalance.BalanceMinor - totalReserved;
                    if (mainBalance.CashableMinor < 0) mainBalance.CashableMinor = 0;
                    mainBalance.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        // Update timestamps for all balances
        foreach (var balance in balances.Values)
        {
            balance.UpdatedAt = DateTime.UtcNow;
        }
    }

    protected async Task AddBalanceChangedEventAsync(
        long playerId,
        string currency,
        string network,
        long balanceMinor,
        long cashableMinor,
        long reservedMinor,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var payload = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid().ToString(),
            occurredAt = DateTime.UtcNow,
            playerId = playerId,
            correlationId = correlationId,
            changes = new[]
            {
                new { currency, network, balanceMinor, cashableMinor, reservedMinor }
            }
        }));

        await LedgerWriter.AddOutboxEventAsync(
            "WalletBalanceChanged",
            "wallet.balance.changed",
            payload,
            cancellationToken);
    }
}

