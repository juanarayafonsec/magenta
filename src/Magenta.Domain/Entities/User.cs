using Microsoft.AspNetCore.Identity;

namespace Magenta.Domain.Entities;

public class User : IdentityUser
{
    public override string? UserName { get; set; } = string.Empty;
    public override string? Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
