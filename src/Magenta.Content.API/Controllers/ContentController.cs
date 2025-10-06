using Microsoft.AspNetCore.Mvc;

namespace Magenta.Content.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    [HttpGet]
    public IActionResult GetContent()
    {
        return Ok();
    }
}
