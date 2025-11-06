using Magenta.Wallet.Application.DTOs.Queries;
using Magenta.Wallet.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BalancesController : ControllerBase
{
    private readonly GetBalanceHandler _balanceHandler;
    private readonly ILogger<BalancesController> _logger;

    public BalancesController(
        GetBalanceHandler balanceHandler,
        ILogger<BalancesController> logger)
    {
        _balanceHandler = balanceHandler;
        _logger = logger;
    }

    /// <summary>
    /// Gets balances for the authenticated player (no playerId in URL).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBalances(CancellationToken cancellationToken)
    {
        // Get player ID from cookie/claims
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                           User.FindFirst("playerId")?.Value ??
                           User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(playerIdClaim) || !long.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized("Player ID not found in authentication claims");
        }

        var query = new GetBalanceQuery { PlayerId = playerId };
        var response = await _balanceHandler.HandleAsync(query, cancellationToken);

        return Ok(response.Items.Select(i => new
        {
            i.Currency,
            i.Network,
            i.BalanceMinor,
            i.ReservedMinor,
            i.CashableMinor
        }));
    }
}




