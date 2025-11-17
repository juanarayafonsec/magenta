namespace Magenta.Payments.Domain.Entities;

public class PaymentProvider
{
    public int ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Enums.ProviderType Type { get; set; }
    public string? ApiBaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

