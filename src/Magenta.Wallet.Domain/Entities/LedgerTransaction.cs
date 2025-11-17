using System.Text.Json;

namespace Magenta.Wallet.Domain.Entities;

public class LedgerTransaction
{
    public Guid TxId { get; set; }
    public Enums.TxType TxType { get; set; }
    public string? ExternalRef { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

