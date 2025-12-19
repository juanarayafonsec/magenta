using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Application.Services;
using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Workers.Jobs;

public class WebhookInboxProcessorJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookInboxProcessorJob> _logger;

    public WebhookInboxProcessorJob(
        IServiceProvider serviceProvider,
        ILogger<WebhookInboxProcessorJob> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var depositService = scope.ServiceProvider.GetRequiredService<DepositService>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IPaymentProviderRepository>();

        var unprocessedEvents = await inboxRepository.GetUnprocessedEventsAsync(limit: 100, cancellationToken);

        foreach (var evt in unprocessedEvents)
        {
            try
            {
                await ProcessWebhookEventAsync(evt, depositService, providerRepository, cancellationToken);
                await inboxRepository.MarkAsProcessedAsync(evt.InboxEventId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process inbox event {EventId}", evt.InboxEventId);
                await inboxRepository.MarkAsFailedAsync(evt.InboxEventId, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessWebhookEventAsync(
        InboxEvent evt,
        DepositService depositService,
        IPaymentProviderRepository providerRepository,
        CancellationToken cancellationToken)
    {
        var payload = evt.Payload.RootElement;

        // Extract provider ID and transaction hash from payload (provider-specific)
        if (payload.TryGetProperty("providerId", out var providerIdElement) &&
            payload.TryGetProperty("txHash", out var txHashElement))
        {
            var providerId = providerIdElement.GetInt32();
            var txHash = txHashElement.GetString();

            if (!string.IsNullOrEmpty(txHash))
            {
                await depositService.ProcessDepositWebhookAsync(providerId, txHash, playerId: null, cancellationToken: cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Invalid webhook payload format for event {EventId}", evt.InboxEventId);
        }
    }
}
