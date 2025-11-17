namespace Magenta.Wallet.Domain.Enums;

public enum AccountType
{
    MAIN,
    WITHDRAW_HOLD,
    BONUS,
    HOUSE,
    HOUSE_WAGER,  // HOUSE:WAGER in DB
    HOUSE_FEES    // HOUSE:FEES in DB
}

