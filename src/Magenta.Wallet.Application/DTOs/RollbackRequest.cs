namespace Magenta.Wallet.Application.DTOs;

public class RollbackRequest
{
    public string OriginalTransactionReference { get; set; } = string.Empty;
    public string RollbackId { get; set; } = string.Empty;
    public string Source { get; set; } = "game";
}
