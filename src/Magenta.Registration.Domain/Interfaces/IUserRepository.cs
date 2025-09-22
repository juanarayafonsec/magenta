// File: src/Magenta.Registration.Domain/Interfaces/IUserRepository.cs

using Magenta.Registration.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Magenta.Registration.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity operations.
/// Defines the contract for user data access operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Creates a new user asynchronously.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <param name="password">The plain text password to hash and store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the IdentityResult.</returns>
    Task<IdentityResult> CreateAsync(User user, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by username asynchronously.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by email asynchronously.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username already exists asynchronously.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if username exists, otherwise false.</returns>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email already exists asynchronously.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if email exists, otherwise false.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
