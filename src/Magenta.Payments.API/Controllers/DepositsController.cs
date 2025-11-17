using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payments.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Cookie authentication
public class DepositsController : ControllerBase
{
    private readonly CreateDepositSessionHandler _createSessionHandler;
    private readonly ILogger<DepositsController> _logger;

    public DepositsController(
        CreateDepositSessionHandler createSessionHandler,
        ILogger<DepositsController> logger)
    {
        _createSessionHandler = createSessionHandler;
        _logger = logger;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<CreateDepositSessionResponse>> CreateSession(
        [FromBody] CreateDepositSessionRequest request,
        CancellationToken cancellationToken)
    {
        // Extract playerId from cookie/claims
        var playerIdClaim = User.FindFirst("playerId")?.Value ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(playerIdClaim) || !long.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized("Player ID not found in authentication context");
        }

        var command = new CreateDepositSessionCommand(
            playerId,
            request.Currency,
            request.Network,
            request.ExpectedAmountMajor,
            request.ExpiresInSeconds,
            request.IdempotencyKey
        );

        try
        {
            var result = await _createSessionHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deposit session");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateDepositSessionRequest(
    string Currency,
    string Network,
    decimal? ExpectedAmountMajor,
    int? ExpiresInSeconds,
    string IdempotencyKey
);

