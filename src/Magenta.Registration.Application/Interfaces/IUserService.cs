using Magenta.Registration.Application.DTOs;

namespace Magenta.Registration.Application.Interfaces;

public interface IUserService
{
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
}
