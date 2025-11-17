using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.ValueObjects;

namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Updates derived account_balances based on ledger postings.
/// Computes reserved_minor and cashable_minor as specified in WalletDocumentation.md
/// </summary>
public class DerivedBalanceUpdater
{
    /// <summary>
    /// Applies postings to account balances and recomputes reserved/cashable amounts.
    /// reserved_minor = sum of WITHDRAW_HOLD balances for the player
    /// cashable_minor = balance_minor - reserved_minor
    /// </summary>
    public static void UpdateBalances(
        Dictionary<long, AccountBalance> balances,
        List<LedgerPosting> postings,
        Dictionary<long, Account> accounts)
    {
        // Apply postings to balances
        foreach (var posting in postings)
        {
            if (!balances.TryGetValue(posting.AccountId, out var balance))
            {
                balance = new AccountBalance { AccountId = posting.AccountId };
                balances[posting.AccountId] = balance;
            }

            if (posting.Direction == Direction.CREDIT)
            {
                balance.BalanceMinor += posting.AmountMinor;
            }
            else // DEBIT
            {
                balance.BalanceMinor -= posting.AmountMinor;
            }

            balance.UpdatedAt = DateTime.UtcNow;
        }

        // Group accounts by player to compute reserved_minor
        var playerAccounts = accounts.Values
            .Where(a => a.AccountType == AccountType.MAIN || a.AccountType == AccountType.WITHDRAW_HOLD)
            .GroupBy(a => a.PlayerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var playerGroup in playerAccounts)
        {
            var playerId = playerGroup.Key;
            var playerAccountList = playerGroup.Value;

            // Find all WITHDRAW_HOLD accounts for this player
            var withdrawHoldAccounts = playerAccountList
                .Where(a => a.AccountType == AccountType.WITHDRAW_HOLD)
                .ToList();

            // Sum reserved_minor from all WITHDRAW_HOLD accounts
            long totalReserved = 0;
            foreach (var withdrawAccount in withdrawHoldAccounts)
            {
                if (balances.TryGetValue(withdrawAccount.AccountId, out var withdrawBalance))
                {
                    totalReserved += withdrawBalance.BalanceMinor;
                }
            }

            // Update cashable_minor for all MAIN accounts of this player
            var mainAccounts = playerAccountList
                .Where(a => a.AccountType == AccountType.MAIN)
                .ToList();

            foreach (var mainAccount in mainAccounts)
            {
                if (balances.TryGetValue(mainAccount.AccountId, out var mainBalance))
                {
                    mainBalance.ReservedMinor = totalReserved;
                    mainBalance.CashableMinor = mainBalance.BalanceMinor - totalReserved;
                    if (mainBalance.CashableMinor < 0)
                        mainBalance.CashableMinor = 0;
                }
            }
        }
    }
}

