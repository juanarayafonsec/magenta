using Magenta.Authentication.Application.DTOs;
using Magenta.Authentication.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Magenta.Registration.Domain.Entities;
using System.Security.Claims;

namespace Magenta.Authentication.API.Controllers;

/// <summary>
/// Controller for authentication and authorization operations.
/// Handles login, logout, token refresh, and user profile endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService, 
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
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
                    Id = user.Id,
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var loginTime = User.FindFirst("LoginTime")?.Value;

                return Ok(new
                {
                    IsAuthenticated = true,
                    UserId = userId,
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
            _logger.LogInformation("Logout attempt for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

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

    /// <summary>
    /// Gets the current user's profile information and claims.
    /// </summary>
    /// <returns>The user profile information and claims.</returns>
    /// <response code="200">User profile retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            // Get user ID from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                return Unauthorized("User not authenticated.");
            }

            _logger.LogInformation("Get user profile for {UserId}", userId);

            // Get user profile
            var result = await _authService.GetUserProfileAsync(userId);

            if (result.Success)
            {
                _logger.LogInformation("User profile retrieved for {UserId}", userId);
                return Ok(result);
            }

            _logger.LogWarning("User profile not found for {UserId}", userId);
            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting user profile");
            return StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred.");
        }
    }

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    /// <returns>The client IP address.</returns>
    private string? GetClientIpAddress()
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
