namespace Magenta.Authentication.Application.Interfaces;

/// <summary>
/// Interface for subscribing to domain events from message brokers.
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Starts subscribing to events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops subscribing to events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
