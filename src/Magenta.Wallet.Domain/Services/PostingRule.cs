using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Represents a single posting rule (one DR or CR entry).
/// </summary>
public class PostingRule
{
    public AccountType AccountType { get; set; }
    public Direction Direction { get; set; }
    public long AmountMinor { get; set; }

    public PostingRule(AccountType accountType, Direction direction, long amountMinor)
    {
        AccountType = accountType;
        Direction = direction;
        AmountMinor = amountMinor;
    }
}




