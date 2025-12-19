namespace Magenta.Payment.Application.DTOs;

public class TransactionStatusResult
{
    public string Status { get; set; } = string.Empty;
    public int Confirmations { get; set; }
    public string? TxHash { get; set; }
    public bool IsFinal { get; set; }
}
