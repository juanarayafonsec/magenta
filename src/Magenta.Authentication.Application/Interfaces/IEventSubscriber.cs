namespace Magenta.Authentication.Application.Interfaces;

public interface IEventSubscriber
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
