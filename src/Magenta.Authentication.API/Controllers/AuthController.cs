using Magenta.Authentication.Application.DTOs;
using Magenta.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Magenta.Authentication.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AuthenticationUser> _userManager;
    private readonly SignInManager<AuthenticationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AuthenticationUser> userManager,
        SignInManager<AuthenticationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for {UsernameOrEmail}", request.UsernameOrEmail);

            // Find user by username or email
            var user = await _userManager.FindByNameAsync(request.UsernameOrEmail)
                       ?? await _userManager.FindByEmailAsync(request.UsernameOrEmail);

            if (user == null)
            {
                _logger.LogWarning("User not found for {UsernameOrEmail}", request.UsernameOrEmail);
                return Unauthorized(new { message = "Invalid credentials." });
            }

            // Verify password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Sign in the user with cookie authentication (no persistent login)
                await _signInManager.SignInAsync(user, isPersistent: false);

                _logger.LogInformation("Login successful for {UsernameOrEmail}", request.UsernameOrEmail);

                return Ok(new 
                { 
                    message = "Login successful",
                    user = new 
                    {
                        id = user.Id,
                        email = user.Email,
                        username = user.UserName
                    }
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked for {UsernameOrEmail}", request.UsernameOrEmail);
                return Unauthorized(new { message = "Account is locked. Please try again later." });
            }

            _logger.LogWarning("Login failed for {UsernameOrEmail}: {Result}", request.UsernameOrEmail, result);
            return Unauthorized(new { message = "Invalid credentials." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login for {UsernameOrEmail}", request.UsernameOrEmail);
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }

   
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAuthStatus()
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var loginTime = User.FindFirst("LoginTime")?.Value;

                return Ok(new
                {
                    IsAuthenticated = true,
                    Username = username,
                    Email = email,
                    LoginTime = loginTime,
                    AuthMethod = "Cookie"
                });
            }

            return Unauthorized(new { IsAuthenticated = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking authentication status");
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Logout()
    {
        try
        {
            _logger.LogInformation("Logout attempt");
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Logout successful");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }
}
