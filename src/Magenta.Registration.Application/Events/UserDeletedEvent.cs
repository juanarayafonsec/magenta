namespace Magenta.Registration.Application.Events;

public class UserDeletedEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? DeletionReason { get; set; }
}
