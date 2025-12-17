using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Domain.Services;


public class LedgerService
{
    public static void ValidateTransaction(LedgerTransaction transaction)
    {
        if (transaction.Postings == null || transaction.Postings.Count < 2)
        {
            throw new InvalidOperationException("A ledger transaction must have at least 2 postings (double-entry accounting).");
        }

        long totalDebits = 0;
        long totalCredits = 0;

        foreach (var posting in transaction.Postings)
        {
            if (posting.Direction == PostingDirection.DR.ToString())
            {
                totalDebits += posting.AmountMinor;
            }
            else if (posting.Direction == PostingDirection.CR.ToString())
            {
                totalCredits += posting.AmountMinor;
            }
            else
            {
                throw new InvalidOperationException($"Invalid posting direction: {posting.Direction}. Must be DR or CR.");
            }
        }

        if (totalDebits != totalCredits)
        {
            throw new InvalidOperationException(
                $"Double-entry validation failed: Total debits ({totalDebits}) must equal total credits ({totalCredits}).");
        }
    }

    public static LedgerPosting CreatePosting(long accountId, PostingDirection direction, long amountMinor)
    {
        if (amountMinor <= 0)
        {
            throw new ArgumentException("Amount must be positive. Direction indicates DR/CR.", nameof(amountMinor));
        }

        return new LedgerPosting
        {
            AccountId = accountId,
            Direction = direction.ToString(),
            AmountMinor = amountMinor
        };
    }
}
