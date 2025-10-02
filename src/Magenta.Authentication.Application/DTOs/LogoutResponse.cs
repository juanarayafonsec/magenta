namespace Magenta.Authentication.Application.DTOs;

public class LogoutResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<string> Errors { get; set; } = new();

    public static LogoutResponse SuccessResponse(string message = "Logout successful.")
    {
        return new LogoutResponse
        {
            Success = true,
            Message = message
        };
    }

    public static LogoutResponse FailureResponse(List<string> errors)
    {
        return new LogoutResponse
        {
            Success = false,
            Errors = errors
        };
    }

    public static LogoutResponse FailureResponse(string error)
    {
        return new LogoutResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}
