namespace Magenta.Authentication.Infrastructure.Events;

/// <summary>
/// Event model for user deletion.
/// </summary>
public class UserDeletedEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DeletionReason { get; set; }
}
