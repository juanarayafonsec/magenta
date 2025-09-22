using Magenta.Domain.Entities;
using Magenta.Domain.Interfaces;
using Magenta.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace Magenta.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public UserRepository(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<IdentityResult> CreateAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByNameAsync(username);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(username);
        return user != null;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }
}
