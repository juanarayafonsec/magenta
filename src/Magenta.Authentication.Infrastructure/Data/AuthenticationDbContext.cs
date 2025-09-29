using Magenta.Registration.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Authentication.Infrastructure.Data;

/// <summary>
/// Authentication database context that extends IdentityDbContext.
/// Manages database operations for authentication using cookie-based sessions.
/// </summary>
public class AuthenticationDbContext : IdentityDbContext<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the model for the database context.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User entity with custom properties
        builder.Entity<User>(entity =>
        {
            // Configure CreatedAt with default value
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configure UpdatedAt as nullable
            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);
        });
    }
}
