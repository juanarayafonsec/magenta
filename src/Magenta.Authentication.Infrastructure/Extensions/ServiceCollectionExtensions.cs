using Magenta.Authentication.Application.Interfaces;
using Magenta.Authentication.Domain.Entities;
using Magenta.Authentication.Infrastructure.Data;
using Magenta.Authentication.Infrastructure.Services;
using Magenta.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Magenta.Authentication.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds authentication services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        services.AddDbContext<AuthenticationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

               // Add Identity services with enhanced security settings
               services.AddIdentity<AuthenticationUser, IdentityRole>(options =>
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
        .AddEntityFrameworkStores<AuthenticationDbContext>()
        .AddDefaultTokenProviders();

        // Configure secure cookie authentication
        services.AddAuthentication("CookieAuth")
        .AddCookie("CookieAuth", options =>
        {
            options.Cookie.Name = "MagentaAuth";
            options.Cookie.HttpOnly = true; // Prevent XSS attacks
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
            options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
            options.Cookie.IsEssential = true; // Required for GDPR compliance
            
            // Session timeout
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // 30 minutes
            options.SlidingExpiration = false; // No automatic sliding expiration
            
            // Use built-in Identity endpoints
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            
            // Security events
            options.Events.OnSigningIn = async context =>
            {
                // Add custom claims if needed
                var user = context.Principal?.Identity?.Name;
                if (!string.IsNullOrEmpty(user))
                {
                    var claims = new List<Claim>
                    {
                        new Claim("LoginTime", DateTime.UtcNow.ToString("O")),
                        new Claim("AuthMethod", "Cookie")
                    };
                    
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    identity?.AddClaims(claims);
                }
                
                await Task.CompletedTask;
            };
            
            options.Events.OnValidatePrincipal = async context =>
            {
                // Custom validation logic can be added here
                await Task.CompletedTask;
            };
        });

        // Add authorization services
        services.AddAuthorization(options =>
        {
            // Add default policy requiring authentication
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Add custom policies for different access levels
            options.AddPolicy("RequireAuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser());

            // Example: Policy for admin users (if needed in future)
            // options.AddPolicy("RequireAdminRole", policy =>
            //     policy.RequireClaim(ClaimTypes.Role, "Admin"));
        });

        // Configure RabbitMQ options
        services.Configure<RabbitMQConfiguration>(configuration.GetSection("RabbitMQ"));

        // Add event subscription services
        services.AddSingleton<IEventSubscriber, RabbitMQEventSubscriber>();
        services.AddHostedService<RabbitMQEventSubscriber>();

        return services;
    }
}