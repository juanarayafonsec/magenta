using Microsoft.AspNetCore.Mvc;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    [HttpGet]
    public IActionResult GetWallet()
    {
        return Ok();
    }
}

