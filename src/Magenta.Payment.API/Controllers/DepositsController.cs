using Magenta.Payment.Application.DTOs;
using Magenta.Payment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepositsController : ControllerBase
{
    private readonly DepositService _depositService;
    private readonly ILogger<DepositsController> _logger;

    public DepositsController(
        DepositService depositService,
        ILogger<DepositsController> logger)
    {
        _depositService = depositService ?? throw new ArgumentNullException(nameof(depositService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new deposit session
    /// </summary>
    /// <param name="request">Deposit session creation request</param>
    /// <returns>Deposit session details including address and QR code data</returns>
    /// <response code="200">Deposit session created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="409">Idempotency key already used</response>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(CreateDepositSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateDepositSessionResponse>> CreateDepositSession(
        [FromBody] CreateDepositSessionRequest request)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues) ||
            string.IsNullOrEmpty(idempotencyKeyValues.ToString()))
        {
            return BadRequest("Idempotency-Key header is required");
        }

        var idempotencyKey = idempotencyKeyValues.ToString();

        try
        {
            var response = await _depositService.CreateDepositSessionAsync(request, idempotencyKey);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create deposit session");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating deposit session");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}
