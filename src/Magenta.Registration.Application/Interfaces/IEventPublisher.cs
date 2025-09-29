namespace Magenta.Registration.Application.Interfaces;

/// <summary>
/// Interface for publishing domain events to message brokers.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the message broker.
    /// </summary>
    /// <typeparam name="T">The type of event to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
