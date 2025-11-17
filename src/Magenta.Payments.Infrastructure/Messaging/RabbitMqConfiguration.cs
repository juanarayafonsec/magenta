namespace Magenta.Payments.Infrastructure.Messaging;

public class RabbitMqConfiguration
{
    public string Uri { get; set; } = string.Empty;
    public string PaymentsExchange { get; set; } = "payments.events";
    public string WalletExchange { get; set; } = "wallet.events";
    public string PaymentsQueue { get; set; } = "payments.consumer";
}

