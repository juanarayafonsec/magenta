using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Maps business operations to DR/CR posting pairs per WalletDocumentation.md.
/// </summary>
public static class PostingRules
{
    /// <summary>
    /// Deposit settled: DR HOUSE; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> DepositSettled(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.HOUSE, Direction.DEBIT, amountMinor),
            new(AccountType.MAIN, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Withdrawal reserved: DR Player:MAIN; CR Player:WITHDRAW_HOLD
    /// </summary>
    public static List<PostingRule> WithdrawalReserved(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.MAIN, Direction.DEBIT, amountMinor),
            new(AccountType.WITHDRAW_HOLD, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Withdrawal settled: DR Player:WITHDRAW_HOLD; CR HOUSE (net), CR HOUSE:FEES (fee)
    /// </summary>
    public static List<PostingRule> WithdrawalSettled(long amountMinor, long feeMinor)
    {
        var netAmount = amountMinor - feeMinor;
        return new List<PostingRule>
        {
            new(AccountType.WITHDRAW_HOLD, Direction.DEBIT, amountMinor),
            new(AccountType.HOUSE, Direction.CREDIT, netAmount),
            new(AccountType.HOUSE_FEES, Direction.CREDIT, feeMinor)
        };
    }

    /// <summary>
    /// Withdrawal failed: DR Player:WITHDRAW_HOLD; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> WithdrawalFailed(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.WITHDRAW_HOLD, Direction.DEBIT, amountMinor),
            new(AccountType.MAIN, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Bet: DR Player:MAIN; CR HOUSE:WAGER
    /// </summary>
    public static List<PostingRule> Bet(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.MAIN, Direction.DEBIT, amountMinor),
            new(AccountType.HOUSE_WAGER, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Win: DR HOUSE:WAGER; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> Win(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.HOUSE_WAGER, Direction.DEBIT, amountMinor),
            new(AccountType.MAIN, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Rollback bet: DR HOUSE:WAGER; CR Player:MAIN
    /// </summary>
    public static List<PostingRule> RollbackBet(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.HOUSE_WAGER, Direction.DEBIT, amountMinor),
            new(AccountType.MAIN, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Rollback win: DR Player:MAIN; CR HOUSE:WAGER
    /// </summary>
    public static List<PostingRule> RollbackWin(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.MAIN, Direction.DEBIT, amountMinor),
            new(AccountType.HOUSE_WAGER, Direction.CREDIT, amountMinor)
        };
    }

    /// <summary>
    /// Standalone fee: DR Player:MAIN; CR HOUSE:FEES
    /// </summary>
    public static List<PostingRule> Fee(long amountMinor)
    {
        return new List<PostingRule>
        {
            new(AccountType.MAIN, Direction.DEBIT, amountMinor),
            new(AccountType.HOUSE_FEES, Direction.CREDIT, amountMinor)
        };
    }
}




