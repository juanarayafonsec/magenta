namespace Magenta.Application.DTOs;
public class RegisterUserResponse
{
    public bool Success { get; set; }

    public string? UserId { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful registration response.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="username">The username.</param>
    /// <param name="email">The email.</param>
    /// <returns>A successful registration response.</returns>
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

    /// <summary>
    /// Creates a failed registration response.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed registration response.</returns>
    public static RegisterUserResponse FailureResponse(List<string> errors)
    {
        return new RegisterUserResponse
        {
            Success = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed registration response with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed registration response.</returns>
    public static RegisterUserResponse FailureResponse(string error)
    {
        return new RegisterUserResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}
