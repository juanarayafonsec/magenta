// File: src/Magenta.Registration.Application/Interfaces/IUserService.cs

using Magenta.Registration.Application.DTOs;

namespace Magenta.Registration.Application.Interfaces;

/// <summary>
/// Service interface for user-related operations.
/// Defines the contract for user business logic operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user asynchronously.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the registration response.</returns>
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}
