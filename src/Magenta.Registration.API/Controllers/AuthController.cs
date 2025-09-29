using Magenta.Registration.Application.DTOs;
using Magenta.Registration.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Registration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IUserService userService, ILogger<AuthController> logger) : ControllerBase
{
   

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration result.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Invalid request data or validation errors.</response>
    /// <response code="409">Username or email already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Registration attempt for username: {Username}", request.Username);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                logger.LogWarning("Validation failed for registration request: {Errors}", string.Join(", ", errors));
                return BadRequest(RegisterUserResponse.FailureResponse(errors));
            }

            // Attempt to register user
            var result = await userService.RegisterUserAsync(request, cancellationToken);

            if (result.Success)
            {
                logger.LogInformation("User registered successfully: {Username}", request.Username);
                return Ok(result);
            }

            // Check if it's a conflict (username/email already exists)
            if (result.Errors.Any(e => e.Contains("already") || e.Contains("taken") || e.Contains("registered")))
            {
                logger.LogWarning("Registration failed due to conflict: {Errors}", string.Join(", ", result.Errors));
                return Conflict(result);
            }

            logger.LogWarning("Registration failed: {Errors}", string.Join(", ", result.Errors));
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during user registration for username: {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }
}
