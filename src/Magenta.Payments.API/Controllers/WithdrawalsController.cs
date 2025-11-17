using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payments.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Cookie authentication
public class WithdrawalsController : ControllerBase
{
    private readonly RequestWithdrawalHandler _requestWithdrawalHandler;
    private readonly ILogger<WithdrawalsController> _logger;

    public WithdrawalsController(
        RequestWithdrawalHandler requestWithdrawalHandler,
        ILogger<WithdrawalsController> logger)
    {
        _requestWithdrawalHandler = requestWithdrawalHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<RequestWithdrawalResponse>> RequestWithdrawal(
        [FromBody] RequestWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        // Extract playerId from cookie/claims
        var playerIdClaim = User.FindFirst("playerId")?.Value ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(playerIdClaim) || !long.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized("Player ID not found in authentication context");
        }

        var command = new RequestWithdrawalCommand(
            playerId,
            request.Currency,
            request.Network,
            request.AmountMajor,
            request.TargetAddress,
            request.IdempotencyKey
        );

        try
        {
            var result = await _requestWithdrawalHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting withdrawal");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record RequestWithdrawalRequest(
    string Currency,
    string Network,
    decimal AmountMajor,
    string TargetAddress,
    string IdempotencyKey
);

