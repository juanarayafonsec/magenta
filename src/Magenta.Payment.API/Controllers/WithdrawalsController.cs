using Magenta.Payment.Application.DTOs;
using Magenta.Payment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WithdrawalsController : ControllerBase
{
    private readonly WithdrawalService _withdrawalService;
    private readonly ILogger<WithdrawalsController> _logger;

    public WithdrawalsController(
        WithdrawalService withdrawalService,
        ILogger<WithdrawalsController> logger)
    {
        _withdrawalService = withdrawalService ?? throw new ArgumentNullException(nameof(withdrawalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new withdrawal request
    /// </summary>
    /// <param name="request">Withdrawal creation request</param>
    /// <returns>Withdrawal request details</returns>
    /// <response code="200">Withdrawal request created successfully</response>
    /// <response code="400">Invalid request or insufficient balance</response>
    /// <response code="409">Idempotency key already used</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWithdrawalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateWithdrawalResponse>> CreateWithdrawal(
        [FromBody] CreateWithdrawalRequest request)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues) ||
            string.IsNullOrEmpty(idempotencyKeyValues.ToString()))
        {
            return BadRequest("Idempotency-Key header is required");
        }

        var idempotencyKey = idempotencyKeyValues.ToString();

        try
        {
            var response = await _withdrawalService.CreateWithdrawalAsync(request, idempotencyKey);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create withdrawal");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating withdrawal");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}
