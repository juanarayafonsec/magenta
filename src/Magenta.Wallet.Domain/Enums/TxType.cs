namespace Magenta.Wallet.Domain.Enums;

/// <summary>
/// Transaction types for ledger transactions.
/// </summary>
public enum TxType
{
    DEPOSIT,
    WITHDRAW_RESERVE,
    WITHDRAW_FINALIZE,
    WITHDRAW_RELEASE,
    BET,
    WIN,
    ROLLBACK,
    FEE
}




