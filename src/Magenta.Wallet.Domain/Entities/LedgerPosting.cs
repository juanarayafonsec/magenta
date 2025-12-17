namespace Magenta.Wallet.Domain.Entities;

public class LedgerPosting
{
    public long LedgerPostingId { get; set; }
    public Guid LedgerTransactionId { get; set; }
    public LedgerTransaction? LedgerTransaction { get; set; }
    public long AccountId { get; set; }
    public Account? Account { get; set; }
    public string Direction { get; set; } = string.Empty; // DR or CR
    public long AmountMinor { get; set; } // BIGINT - always positive, direction indicates DR/CR
}
