using Microsoft.AspNetCore.Identity;

namespace Magenta.Authentication.Domain.Entities;

public class AuthenticationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
