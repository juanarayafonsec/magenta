using Magenta.Registration.Application.Interfaces;
using Magenta.Registration.Application.Services;
using Magenta.Registration.Domain.Entities;
using Magenta.Registration.Domain.Interfaces;
using Magenta.Registration.Infrastructure.Data;
using Magenta.Registration.Infrastructure.Repositories;
using Magenta.Registration.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Registration.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring services in the Infrastructure layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Add Identity services with enhanced security settings (matching Authentication service)
        services.AddIdentity<User, IdentityRole>(options =>
        {
            // Enhanced password settings for security
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true; // Require special characters
            options.Password.RequiredLength = 8; // Minimum 8 characters

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Enhanced lockout settings for security
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // 15 minutes lockout
            options.Lockout.MaxFailedAccessAttempts = 5; // 5 failed attempts
            options.Lockout.AllowedForNewUsers = true;

            // Sign-in settings
            options.SignIn.RequireConfirmedEmail = false; // No email confirmation required
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Add event publishing services
        services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();

        // Add application services
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
