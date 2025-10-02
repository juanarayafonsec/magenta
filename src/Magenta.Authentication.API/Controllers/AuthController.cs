using Magenta.Authentication.Application.DTOs;
using Magenta.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
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

    /// <summary>
    /// Authenticates a user and creates a secure cookie session.
    /// </summary>
    /// <param name="request">The login request containing user credentials.</param>
    /// <returns>The login result with user information.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Invalid request data or validation errors.</response>
    /// <response code="401">Invalid credentials or account locked.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for {UsernameOrEmail}", request.UsernameOrEmail);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Validation failed for login request: {Errors}", string.Join(", ", errors));
                return BadRequest(LoginResponse.FailureResponse(errors));
            }

            // Find user by username or email
            var user = await _userManager.FindByNameAsync(request.UsernameOrEmail)
                       ?? await _userManager.FindByEmailAsync(request.UsernameOrEmail);

            if (user == null)
            {
                _logger.LogWarning("User not found for {UsernameOrEmail}", request.UsernameOrEmail);
                return Unauthorized(LoginResponse.FailureResponse(new List<string> { "Invalid credentials." }));
            }

            // Verify password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim("LoginTime", DateTime.UtcNow.ToString("O")),
                    new Claim("AuthMethod", "Cookie")
                };

                // Add roles if any
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Create claims identity
                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in the user with cookie authentication
                await HttpContext.SignInAsync("CookieAuth", claimsPrincipal, new AuthenticationProperties
                {
                    IsPersistent = request.RememberMe, // Use RememberMe from request
                    ExpiresUtc = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddMinutes(30),
                    AllowRefresh = true
                });

                _logger.LogInformation("Login successful for {UsernameOrEmail}", request.UsernameOrEmail);

                // Return success response
                var userInfo = new UserInfo
                {
                    Username = user.UserName ?? "",
                    Email = user.Email ?? "",
                    CreatedAt = user.CreatedAt
                };

                var loginResponse = LoginResponse.SuccessResponse(1800, userInfo); // 30 minutes in seconds

                return Ok(loginResponse);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked for {UsernameOrEmail}", request.UsernameOrEmail);
                return Unauthorized(LoginResponse.FailureResponse(new List<string> { "Account is locked. Please try again later." }));
            }

            _logger.LogWarning("Login failed for {UsernameOrEmail}: {Result}", request.UsernameOrEmail, result);
            return Unauthorized(LoginResponse.FailureResponse(new List<string> { "Invalid credentials." }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login for {UsernameOrEmail}", request.UsernameOrEmail);
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }

    /// <summary>
    /// Checks if the current user is authenticated via cookie.
    /// </summary>
    /// <returns>Authentication status.</returns>
    /// <response code="200">User is authenticated.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("status")]
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

    /// <summary>
    /// Logs out a user by clearing their cookie session.
    /// </summary>
    /// <returns>The logout result.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            _logger.LogInformation("Logout attempt");

            // Sign out the user from cookie authentication
            await HttpContext.SignOutAsync("CookieAuth");

            _logger.LogInformation("Logout successful");

            var logoutResponse = new LogoutResponse
            {
                Success = true,
                Message = "Logout successful"
            };

            return Ok(logoutResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }
}
