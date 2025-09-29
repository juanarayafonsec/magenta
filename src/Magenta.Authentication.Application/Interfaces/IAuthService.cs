using Magenta.Authentication.Application.DTOs;

namespace Magenta.Authentication.Application.Interfaces;

/// <summary>
/// Service interface for authentication operations.
/// Defines the contract for login and user profile operations using cookie-based authentication.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gets the current user's profile information and claims.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user profile response.</returns>
    Task<MeResponse> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
}
