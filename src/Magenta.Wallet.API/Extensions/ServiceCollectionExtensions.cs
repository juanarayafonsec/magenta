using Magenta.Wallet.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Magenta.Wallet.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWalletApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add wallet services
        services.AddWalletServices(configuration);

        // Authentication (cookie-based, matching Authentication service pattern)
        services.AddAuthentication("CookieAuth")
            .AddCookie("CookieAuth", options =>
            {
                options.Cookie.Name = "MagentaAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = false;
            });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}




