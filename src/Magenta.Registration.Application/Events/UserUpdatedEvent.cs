namespace Magenta.Registration.Application.Events;

/// <summary>
/// Event published when a user is updated in the Registration service.
/// </summary>
public class UserUpdatedEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the password hash (for synchronization).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the security stamp.
    /// </summary>
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// Gets or sets whether the email is confirmed.
    /// </summary>
    public bool? EmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets whether lockout is enabled.
    /// </summary>
    public bool? LockoutEnabled { get; set; }

    /// <summary>
    /// Gets or sets the lockout end date (if any).
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the fields that were updated.
    /// </summary>
    public List<string> UpdatedFields { get; set; } = new();
}
