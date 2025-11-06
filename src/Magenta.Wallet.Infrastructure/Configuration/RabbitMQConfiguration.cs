namespace Magenta.Wallet.Infrastructure.Configuration;

public class RabbitMQConfiguration
{
    public string Uri { get; set; } = string.Empty;
    public string WalletExchange { get; set; } = "wallet.events";
    public string PaymentsExchange { get; set; } = "payments.events";
    public string WalletQueue { get; set; } = "wallet.consumer";
}




