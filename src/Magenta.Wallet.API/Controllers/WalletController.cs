using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Handlers;
using Magenta.Wallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IAccountReadModel _accountReadModel;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        IAccountReadModel accountReadModel,
        ILogger<WalletController> logger)
    {
        _accountReadModel = accountReadModel;
        _logger = logger;
    }

    [HttpGet("currencies")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CurrencyNetworkDto>>> GetCurrencies(CancellationToken cancellationToken)
    {
        var currencies = await _accountReadModel.GetActiveCurrencyNetworksAsync(cancellationToken);
        return Ok(currencies);
    }

    [HttpGet("balances")]
    [Authorize] // Cookie authentication
    public async Task<ActionResult<GetBalanceResponse>> GetBalances(CancellationToken cancellationToken)
    {
        // Extract playerId from cookie/claims - simplified for now
        // In production, this would come from the authentication cookie
        var playerIdClaim = User.FindFirst("playerId")?.Value ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(playerIdClaim) || !long.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized("Player ID not found in authentication context");
        }

        var handler = new GetBalanceHandler(_accountReadModel);
        var query = new GetBalanceQuery(playerId);
        var result = await handler.HandleAsync(query, cancellationToken);
        
        return Ok(result);
    }
}
