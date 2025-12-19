namespace Magenta.Payment.Domain.Entities;

public class PaymentProvider
{
    public int ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // CRYPTO or FIAT
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
