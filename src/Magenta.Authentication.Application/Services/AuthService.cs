using Magenta.Authentication.Application.DTOs;
using Magenta.Authentication.Application.Interfaces;
using Magenta.Registration.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Magenta.Authentication.Application.Services;

/// <summary>
/// Service implementation for authentication operations.
/// Handles user profile operations using cookie-based authentication.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        ILogger<AuthService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <summary>
    /// Gets the current user's profile information and claims.
    /// </summary>
    public async Task<MeResponse> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get user profile for {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User profile not found for {UserId}", userId);
                return MeResponse.FailureResponse("User not found.");
            }

            // Get user claims
            var claims = await _userManager.GetClaimsAsync(user);
            var claimInfos = claims.Select(c => new ClaimInfo
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            // Create user info
            var userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt
            };

            _logger.LogInformation("User profile retrieved for {UserId}", userId);

            return MeResponse.SuccessResponse(userInfo, claimInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting user profile for {UserId}", userId);
            return MeResponse.FailureResponse("An internal server error occurred while retrieving user profile.");
        }
    }

}
