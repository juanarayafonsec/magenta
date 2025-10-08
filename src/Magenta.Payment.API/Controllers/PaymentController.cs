using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    [HttpGet]
    public IActionResult GetPayment()
    {
        return Ok();
    }
}

