using RabbitMQ.Client;

namespace Magenta.Wallet.Infrastructure.Messaging;

public class RabbitMqConfiguration
{
    public string Uri { get; set; } = string.Empty;
    public string WalletExchange { get; set; } = "wallet.events";
    public string PaymentsExchange { get; set; } = "payments.events";
    public string WalletQueue { get; set; } = "wallet.consumer";
}

public static class RabbitMqSetup
{
    public static void DeclareExchanges(IModel channel, RabbitMqConfiguration config)
    {
        // Declare topic exchanges
        channel.ExchangeDeclare(config.WalletExchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(config.PaymentsExchange, ExchangeType.Topic, durable: true, autoDelete: false);

        // Declare queue and bind to payments exchange
        channel.QueueDeclare(config.WalletQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(config.WalletQueue, config.PaymentsExchange, "payments.deposit.settled");
        channel.QueueBind(config.WalletQueue, config.PaymentsExchange, "payments.withdrawal.broadcasted");
        channel.QueueBind(config.WalletQueue, config.PaymentsExchange, "payments.withdrawal.settled");
        channel.QueueBind(config.WalletQueue, config.PaymentsExchange, "payments.withdrawal.failed");
    }
}

