namespace Magenta.Wallet.Application.Events;

public class WithdrawalReservedEvent
{
    public long PlayerId { get; set; }
    public int CurrencyNetworkId { get; set; }
    public long AmountMinor { get; set; }
    public Guid TransactionId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
