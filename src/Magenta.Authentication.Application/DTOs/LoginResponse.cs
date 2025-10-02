namespace Magenta.Authentication.Application.DTOs;

public class LoginResponse
{
    public bool Success { get; set; }
    public int ExpiresIn { get; set; }
    public string AuthMethod { get; set; } = "Cookie";

    public UserInfo? User { get; set; }

    public List<string> Errors { get; set; } = new();

    public static LoginResponse SuccessResponse(int expiresIn, UserInfo user)
    {
        return new LoginResponse
        {
            Success = true,
            ExpiresIn = expiresIn,
            AuthMethod = "Cookie",
            User = user
        };
    }

    public static LoginResponse FailureResponse(List<string> errors)
    {
        return new LoginResponse
        {
            Success = false,
            Errors = errors
        };
    }

    public static LoginResponse FailureResponse(string error)
    {
        return new LoginResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
