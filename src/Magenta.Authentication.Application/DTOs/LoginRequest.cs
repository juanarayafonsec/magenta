using System.ComponentModel.DataAnnotations;

namespace Magenta.Authentication.Application.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Username or email is required.")]
    [StringLength(256, ErrorMessage = "Username or email cannot exceed 256 characters.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
