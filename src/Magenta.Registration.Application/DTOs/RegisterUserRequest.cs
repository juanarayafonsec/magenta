// File: src/Magenta.Registration.Application/DTOs/RegisterUserRequest.cs

using System.ComponentModel.DataAnnotations;

namespace Magenta.Registration.Application.DTOs;

/// <summary>
/// Request DTO for user registration.
/// Contains the data required to register a new user.
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// Gets or sets the unique username for the new user.
    /// Must be unique and between 3-50 characters.
    /// </summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, hyphens, and underscores.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address for the new user.
    /// Must be a valid email format and unique.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for the new user.
    /// Must be between 6-100 characters.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation.
    /// Must match the Password field.
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
