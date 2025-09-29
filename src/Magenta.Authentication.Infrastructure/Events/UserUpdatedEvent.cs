namespace Magenta.Authentication.Infrastructure.Events;

/// <summary>
/// Event model for user updates.
/// </summary>
public class UserUpdatedEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public bool? EmailConfirmed { get; set; }
    public bool? LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public List<string> UpdatedFields { get; set; } = new();
}
