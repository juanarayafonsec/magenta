using System.Text.Json;

namespace Magenta.Wallet.Domain.Entities;

public class LedgerTransaction
{
    public Guid LedgerTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ReferenceType { get; set; } = string.Empty; // Deposit, Withdrawal, Bet, Win, Rollback
    public string ReferenceId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // payments, game, etc.
    public JsonDocument? Metadata { get; set; }
    
    // Navigation property
    public ICollection<LedgerPosting> Postings { get; set; } = new List<LedgerPosting>();
}
