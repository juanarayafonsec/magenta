using System.Text.Json;

namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Ledger transaction - logical grouping of postings.
/// </summary>
public class LedgerTransaction
{
    public Guid TxId { get; set; }
    public string TxType { get; set; } = string.Empty;
    public string? ExternalRef { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<LedgerPosting> Postings { get; set; } = new List<LedgerPosting>();
}




