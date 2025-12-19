namespace Magenta.Payment.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(string eventType, string routingKey, object payload, CancellationToken cancellationToken = default);
}
