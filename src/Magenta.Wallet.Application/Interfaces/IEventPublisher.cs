namespace Magenta.Wallet.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default) where T : class;
}
