namespace Magenta.Registration.Application.DTOs;

public class RegisterUserResponse
{
    public bool Success { get; set; }

    public string? UserId { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public List<string> Errors { get; set; } = new();

    public static RegisterUserResponse SuccessResponse(string userId, string username, string email)
    {
        return new RegisterUserResponse
        {
            Success = true,
            UserId = userId,
            Username = username,
            Email = email
        };
    }

    public static RegisterUserResponse FailureResponse(List<string> errors)
    {
        return new RegisterUserResponse
        {
            Success = false,
            Errors = errors
        };
    }

    public static RegisterUserResponse FailureResponse(string error)
    {
        return new RegisterUserResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}
