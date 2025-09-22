using Magenta.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(e => e.UserName)
                .IsUnique()
                .HasDatabaseName("IX_Users_UserName");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });
    }
}
