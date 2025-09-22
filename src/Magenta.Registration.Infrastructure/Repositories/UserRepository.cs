// File: src/Magenta.Registration.Infrastructure/Repositories/UserRepository.cs

using Magenta.Registration.Domain.Entities;
using Magenta.Registration.Domain.Interfaces;
using Magenta.Registration.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Registration.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity operations.
/// Provides data access operations for users using Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userManager">The user manager for Identity operations.</param>
    public UserRepository(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>
    /// Creates a new user asynchronously.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <param name="password">The plain text password to hash and store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the IdentityResult.</returns>
    public async Task<IdentityResult> CreateAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        return await _userManager.CreateAsync(user, password);
    }

    /// <summary>
    /// Finds a user by username asynchronously.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByNameAsync(username);
    }

    /// <summary>
    /// Finds a user by email asynchronously.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    /// <summary>
    /// Checks if a username already exists asynchronously.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if username exists, otherwise false.</returns>
    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(username);
        return user != null;
    }

    /// <summary>
    /// Checks if an email already exists asynchronously.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if email exists, otherwise false.</returns>
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }
}
