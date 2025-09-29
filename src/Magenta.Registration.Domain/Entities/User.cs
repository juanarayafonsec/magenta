using Microsoft.AspNetCore.Identity;

namespace Magenta.Registration.Domain.Entities;

/// <summary>
/// User entity representing a registered user in the system.
/// Inherits from IdentityUser to leverage ASP.NET Core Identity features.
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// Gets or sets the unique username for the user.
    /// This property overrides the base UserName to ensure it's required.
    /// </summary>
    public override string? UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address for the user.
    /// This property overrides the base Email to ensure it's required.
    /// </summary>
    public override string? Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
