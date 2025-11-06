using Magenta.Wallet.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrenciesController : ControllerBase
{
    private readonly IAccountReadModel _accountReadModel;

    public CurrenciesController(IAccountReadModel accountReadModel)
    {
        _accountReadModel = accountReadModel;
    }

    /// <summary>
    /// Gets all active currency-network pairs (public endpoint).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken)
    {
        var currencies = await _accountReadModel.GetActiveCurrencyNetworksAsync(cancellationToken);
        
        return Ok(currencies.Select(c => new
        {
            c.CurrencyNetworkId,
            c.CurrencyCode,
            c.CurrencyDisplayName,
            c.NetworkName,
            c.Decimals,
            c.IconUrl
        }));
    }
}




