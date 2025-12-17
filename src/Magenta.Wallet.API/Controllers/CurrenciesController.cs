using Magenta.Wallet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<CurrenciesController> _logger;

    public CurrenciesController(ICurrencyService currencyService, ILogger<CurrenciesController> logger)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all supported currency networks.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Application.DTOs.CurrencyNetworkDto>>> GetCurrencies(CancellationToken cancellationToken)
    {
        try
        {
            var currencies = await _currencyService.GetAllCurrencyNetworksAsync(cancellationToken);
            return Ok(currencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currencies");
            return StatusCode(500, "An error occurred while retrieving currencies");
        }
    }
}
