using Magenta.Application.DTOs;

namespace Magenta.Application.Interfaces;

public interface IUserService
{
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}