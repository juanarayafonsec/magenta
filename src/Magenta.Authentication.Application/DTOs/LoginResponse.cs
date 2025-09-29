namespace Magenta.Authentication.Application.DTOs;

/// <summary>
/// Response DTO for successful login.
/// Contains authentication session information and user data.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the login was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the session expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the authentication method used.
    /// </summary>
    public string AuthMethod { get; set; } = "Cookie";

    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Gets or sets the list of errors if login failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful login response.
    /// </summary>
    /// <param name="expiresIn">Session expiration in seconds.</param>
    /// <param name="user">The user information.</param>
    /// <returns>A successful login response.</returns>
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

    /// <summary>
    /// Creates a failed login response.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed login response.</returns>
    public static LoginResponse FailureResponse(List<string> errors)
    {
        return new LoginResponse
        {
            Success = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed login response with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed login response.</returns>
    public static LoginResponse FailureResponse(string error)
    {
        return new LoginResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// User information DTO.
/// Contains basic user profile information.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
