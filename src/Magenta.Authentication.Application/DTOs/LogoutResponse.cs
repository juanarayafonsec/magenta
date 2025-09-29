namespace Magenta.Authentication.Application.DTOs;

/// <summary>
/// Response DTO for user logout.
/// Contains the result of the logout operation.
/// </summary>
public class LogoutResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the logout was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of errors if logout failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful logout response.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A successful logout response.</returns>
    public static LogoutResponse SuccessResponse(string message = "Logout successful.")
    {
        return new LogoutResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failed logout response.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed logout response.</returns>
    public static LogoutResponse FailureResponse(List<string> errors)
    {
        return new LogoutResponse
        {
            Success = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed logout response with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed logout response.</returns>
    public static LogoutResponse FailureResponse(string error)
    {
        return new LogoutResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}
