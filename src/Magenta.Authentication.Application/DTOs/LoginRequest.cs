using System.ComponentModel.DataAnnotations;

namespace Magenta.Authentication.Application.DTOs;

/// <summary>
/// Request DTO for user login.
/// Contains the credentials required for authentication.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the username or email for login.
    /// </summary>
    [Required(ErrorMessage = "Username or email is required.")]
    [StringLength(256, ErrorMessage = "Username or email cannot exceed 256 characters.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to remember the user.
    /// This affects the refresh token expiration time.
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
