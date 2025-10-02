using Magenta.Registration.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Magenta.Registration.Application.Interfaces;

public interface IUserRepository
{
    Task<IdentityResult> CreateAsync(User user, string password, CancellationToken cancellationToken = default);

    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
