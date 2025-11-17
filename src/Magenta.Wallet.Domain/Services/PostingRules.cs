using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.ValueObjects;

namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Maps business operations to DR/CR posting pairs according to WalletDocumentation.md
/// </summary>
public static class PostingRules
{
    public record PostingRule(long AccountId, Direction Direction, Money Amount);

    /// <summary>
    /// Deposit settled: DR HOUSE; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> DepositSettled(long houseAccountId, long playerMainAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(houseAccountId, Direction.DEBIT, amount),
            new(playerMainAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Withdrawal reserved: DR Player:MAIN; CR Player:WITHDRAW_HOLD
    /// </summary>
    public static List<PostingRule> WithdrawalReserved(long playerMainAccountId, long playerWithdrawHoldAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(playerMainAccountId, Direction.DEBIT, amount),
            new(playerWithdrawHoldAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Withdrawal settled: DR Player:WITHDRAW_HOLD; CR HOUSE (net), CR HOUSE:FEES (fee)
    /// </summary>
    public static List<PostingRule> WithdrawalSettled(long playerWithdrawHoldAccountId, long houseAccountId, long houseFeesAccountId, Money amountMinor, Money feeMinor)
    {
        var netAmount = new Money(amountMinor.MinorUnits - feeMinor.MinorUnits);
        return new List<PostingRule>
        {
            new(playerWithdrawHoldAccountId, Direction.DEBIT, amountMinor),
            new(houseAccountId, Direction.CREDIT, netAmount),
            new(houseFeesAccountId, Direction.CREDIT, feeMinor)
        };
    }

    /// <summary>
    /// Withdrawal failed: DR Player:WITHDRAW_HOLD; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> WithdrawalFailed(long playerWithdrawHoldAccountId, long playerMainAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(playerWithdrawHoldAccountId, Direction.DEBIT, amount),
            new(playerMainAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Bet: DR Player:MAIN; CR HOUSE:WAGER
    /// </summary>
    public static List<PostingRule> Bet(long playerMainAccountId, long houseWagerAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(playerMainAccountId, Direction.DEBIT, amount),
            new(houseWagerAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Win: DR HOUSE:WAGER; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> Win(long houseWagerAccountId, long playerMainAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(houseWagerAccountId, Direction.DEBIT, amount),
            new(playerMainAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Rollback bet: DR HOUSE:WAGER; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> RollbackBet(long houseWagerAccountId, long playerMainAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(houseWagerAccountId, Direction.DEBIT, amount),
            new(playerMainAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Rollback win: DR Player:MAIN; CR HOUSE:WAGER
    /// </summary>
    public static List<PostingRule> RollbackWin(long playerMainAccountId, long houseWagerAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(playerMainAccountId, Direction.DEBIT, amount),
            new(houseWagerAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Fee: DR Player:MAIN; CR HOUSE:FEES
    /// </summary>
    public static List<PostingRule> Fee(long playerMainAccountId, long houseFeesAccountId, Money amount)
    {
        return new List<PostingRule>
        {
            new(playerMainAccountId, Direction.DEBIT, amount),
            new(houseFeesAccountId, Direction.CREDIT, amount)
        };
    }

    /// <summary>
    /// Validates that sum of DR equals sum of CR
    /// </summary>
    public static bool IsBalanced(List<PostingRule> rules)
    {
        var totalDebit = rules.Where(r => r.Direction == Direction.DEBIT).Sum(r => r.Amount.MinorUnits);
        var totalCredit = rules.Where(r => r.Direction == Direction.CREDIT).Sum(r => r.Amount.MinorUnits);
        return totalDebit == totalCredit;
    }
}

