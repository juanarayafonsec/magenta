namespace Magenta.Wallet.Domain.Enums;

public enum AccountType
{
    // Player accounts
    MAIN,
    WITHDRAW_HOLD,
    BONUS,
    
    // House accounts
    HOUSE,
    HOUSE_WAGER,      // HOUSE:WAGER
    HOUSE_FEES        // HOUSE:FEES
}
