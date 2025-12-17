using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BalancesController : ControllerBase
{
    private readonly IBalanceService _balanceService;
    private readonly ILogger<BalancesController> _logger;

    public BalancesController(IBalanceService balanceService, ILogger<BalancesController> logger)
    {
        _balanceService = balanceService ?? throw new ArgumentNullException(nameof(balanceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<BalanceResponse>> GetBalances(CancellationToken cancellationToken)
    {
        try
        {
            // Get player ID from claims (assumes authentication middleware sets this)
            var playerIdClaim = User.FindFirst("PlayerId")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(playerIdClaim) || !long.TryParse(playerIdClaim, out var playerId))
            {
                return Unauthorized("Player ID not found in authentication claims");
            }

            var balances = await _balanceService.GetPlayerBalancesAsync(playerId, cancellationToken);
            return Ok(balances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balances for player");
            return StatusCode(500, "An error occurred while retrieving balances");
        }
    }
}
