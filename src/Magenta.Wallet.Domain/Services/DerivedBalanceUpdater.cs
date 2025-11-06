using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Updates derived balances from ledger postings.
/// Computes reserved_minor and cashable_minor per WalletDocumentation.md.
/// </summary>
public static class DerivedBalanceUpdater
{
    /// <summary>
    /// Applies a posting to an account balance.
    /// </summary>
    public static void ApplyPosting(AccountBalance balance, Direction direction, long amountMinor)
    {
        if (direction == Direction.CREDIT)
        {
            balance.BalanceMinor += amountMinor;
        }
        else // DEBIT
        {
            balance.BalanceMinor -= amountMinor;
        }

        balance.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Recomputes reserved_minor and cashable_minor for a player's accounts.
    /// reserved_minor = sum of all WITHDRAW_HOLD balances for the player.
    /// cashable_minor = balance_minor - reserved_minor.
    /// </summary>
    public static void RecomputeReservedAndCashable(
        Dictionary<long, AccountBalance> balancesByAccountId,
        Dictionary<long, AccountBalance> withdrawHoldBalances)
    {
        // reserved_minor is the sum of WITHDRAW_HOLD balances
        var totalReserved = withdrawHoldBalances.Values
            .Sum(b => b.BalanceMinor);

        // Update reserved_minor and cashable_minor for MAIN accounts
        foreach (var balance in balancesByAccountId.Values)
        {
            // Only MAIN accounts have reserved/cashable (other account types don't use these)
            var account = balance.Account;
            if (account.AccountType == AccountType.MAIN)
            {
                // For MAIN accounts, reserved is calculated from WITHDRAW_HOLD accounts
                // Find the corresponding WITHDRAW_HOLD account for the same player/currency_network
                var withdrawHoldAccount = account.CurrencyNetwork.Accounts
                    .FirstOrDefault(a => a.PlayerId == account.PlayerId && 
                                        a.AccountType == AccountType.WITHDRAW_HOLD);
                
                if (withdrawHoldAccount?.Balance != null)
                {
                    balance.ReservedMinor = withdrawHoldAccount.Balance.BalanceMinor;
                }
                else
                {
                    balance.ReservedMinor = 0;
                }

                balance.CashableMinor = balance.BalanceMinor - balance.ReservedMinor;
                balance.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Updates reserved_minor for a MAIN account based on its corresponding WITHDRAW_HOLD balance.
    /// </summary>
    public static void UpdateReservedFromWithdrawHold(AccountBalance mainBalance, AccountBalance? withdrawHoldBalance)
    {
        mainBalance.ReservedMinor = withdrawHoldBalance?.BalanceMinor ?? 0;
        mainBalance.CashableMinor = mainBalance.BalanceMinor - mainBalance.ReservedMinor;
        mainBalance.UpdatedAt = DateTime.UtcNow;
    }
}




