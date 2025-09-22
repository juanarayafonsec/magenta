// File: src/Magenta.Registration.Infrastructure/Data/ApplicationDbContext.cs

using Magenta.Registration.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Registration.Infrastructure.Data;

/// <summary>
/// Application database context that extends IdentityDbContext to support ASP.NET Core Identity.
/// Manages database operations for the application.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
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

        // Configure User entity with custom properties only
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
