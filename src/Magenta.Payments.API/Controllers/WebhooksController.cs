using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payments.API.Controllers;

[ApiController]
[Route("api/providers/{providerId}/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly ProcessDepositWebhookHandler _depositWebhookHandler;
    private readonly ProcessWithdrawalWebhookHandler _withdrawalWebhookHandler;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        ProcessDepositWebhookHandler depositWebhookHandler,
        ProcessWithdrawalWebhookHandler withdrawalWebhookHandler,
        ILogger<WebhooksController> logger)
    {
        _depositWebhookHandler = depositWebhookHandler;
        _withdrawalWebhookHandler = withdrawalWebhookHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessWebhook(
        int providerId,
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        // Extract event type from payload
        var eventType = payload.GetValueOrDefault("eventType")?.ToString() 
            ?? payload.GetValueOrDefault("type")?.ToString()
            ?? "unknown";

        // Extract signature if present
        var signature = Request.Headers["X-Signature"].FirstOrDefault();

        if (eventType.Contains("deposit"))
        {
            var command = new ProcessDepositWebhookCommand(
                providerId,
                eventType,
                payload,
                signature
            );
            await _depositWebhookHandler.HandleAsync(command, cancellationToken);
        }
        else if (eventType.Contains("withdrawal"))
        {
            var command = new ProcessWithdrawalWebhookCommand(
                providerId,
                eventType,
                payload,
                signature
            );
            await _withdrawalWebhookHandler.HandleAsync(command, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown webhook event type: {EventType}", eventType);
        }

        return Ok();
    }
}

