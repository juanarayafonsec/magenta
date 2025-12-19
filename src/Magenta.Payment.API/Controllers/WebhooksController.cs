using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Application.Services;
using Magenta.Payment.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Magenta.Payment.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IInboxRepository _inboxRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly IEnumerable<IPaymentProvider> _providerAdapters;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IInboxRepository inboxRepository,
        IPaymentProviderRepository providerRepository,
        IEnumerable<IPaymentProvider> providerAdapters,
        ILogger<WebhooksController> logger)
    {
        _inboxRepository = inboxRepository ?? throw new ArgumentNullException(nameof(inboxRepository));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _providerAdapters = providerAdapters ?? throw new ArgumentNullException(nameof(providerAdapters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Receive webhook from payment provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>200 OK if webhook received successfully</returns>
    [HttpPost("providers/{providerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveWebhook(int providerId)
    {
        try
        {
            // Read request body
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();

            // Get provider
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                _logger.LogWarning("Unknown provider {ProviderId}", providerId);
                return BadRequest("Unknown provider");
            }

            // Verify signature
            var signature = Request.Headers["X-Signature"].ToString();
            var providerAdapter = _providerAdapters.FirstOrDefault(); // Simplified - use factory in production
            if (providerAdapter != null)
            {
                var secret = Environment.GetEnvironmentVariable($"PROVIDER_{providerId}_SECRET") ?? "secret";
                if (!providerAdapter.VerifyWebhookSignature(payload, signature, secret))
                {
                    _logger.LogWarning("Invalid webhook signature for provider {ProviderId}", providerId);
                    return BadRequest("Invalid signature");
                }
            }

            // Extract idempotency key from payload (provider-specific)
            var idempotencyKey = ExtractIdempotencyKey(payload, providerId);

            // Store in inbox (idempotent)
            var existing = await _inboxRepository.GetBySourceAndKeyAsync(
                provider.Name, idempotencyKey);
            
            if (existing == null)
            {
                var inboxEvent = new InboxEvent
                {
                    Source = provider.Name,
                    IdempotencyKey = idempotencyKey,
                    Payload = JsonDocument.Parse(payload),
                    ReceivedAt = DateTime.UtcNow
                };

                await _inboxRepository.CreateAsync(inboxEvent);
            }

            // Return 200 immediately (processing happens asynchronously)
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from provider {ProviderId}", providerId);
            // Still return 200 to prevent provider retries
            return Ok();
        }
    }

    private string ExtractIdempotencyKey(string payload, int providerId)
    {
        // Simplified - in production, parse provider-specific payload format
        try
        {
            var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("id", out var id))
            {
                return id.GetString() ?? Guid.NewGuid().ToString();
            }
            if (doc.RootElement.TryGetProperty("txHash", out var txHash))
            {
                return txHash.GetString() ?? Guid.NewGuid().ToString();
            }
        }
        catch
        {
            // Fallback
        }

        return Guid.NewGuid().ToString();
    }
}
