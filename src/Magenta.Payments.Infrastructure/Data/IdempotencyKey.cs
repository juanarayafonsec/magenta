namespace Magenta.Payments.Infrastructure.Data;

public class IdempotencyKey
{
    public string Source { get; set; } = string.Empty;
    public string IdempotencyKeyValue { get; set; } = string.Empty;
    public Guid? RelatedId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

