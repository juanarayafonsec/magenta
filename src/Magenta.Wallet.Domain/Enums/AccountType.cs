namespace Magenta.Wallet.Domain.Enums;

/// <summary>
/// Account types in the wallet system.
/// </summary>
public enum AccountType
{
    MAIN,              // Player's spendable balance
    WITHDRAW_HOLD,     // Funds locked for withdrawal requests
    BONUS,             // Optional bonus wallet
    HOUSE,             // Casino treasury (generic)
    HOUSE_WAGER,       // HOUSE:WAGER - Wager pool for bets/wins
    HOUSE_FEES         // HOUSE:FEES - Fee collection account
}




