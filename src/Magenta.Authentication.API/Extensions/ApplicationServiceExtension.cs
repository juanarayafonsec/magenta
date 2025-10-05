using Magenta.Authentication.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Magenta.Authentication.API.Extensions;
public static class ApplicationServiceExtension
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container with global authorization policy
        services.AddControllers(options =>
        {
            // Create a global authorization policy that requires authentication
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            
            // Apply the policy globally
            options.Filters.Add(new AuthorizeFilter(policy));
        });


        // Add API Explorer for Swagger
        services.AddEndpointsApiExplorer();

        // Add Swagger/OpenAPI with enhanced security documentation
        services.AddSwagger();

        // Add CORS with enhanced security for cookie authentication
        services.AddCors();

        // Add authentication services
        services.AddAuthenticationServices(configuration);
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Magenta Authentication API",
                Version = "v1",
                Description = "A secure authentication and authorization API with cookie-based authentication support",
                Contact = new() { Name = "Magenta Team" }
            });

            // Include XML comments for better documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

        });

    }

    private static void AddCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SecureCors", policy =>
            {
                policy.WithOrigins("https://localhost:3000", "https://localhost:3001", "http://localhost:3000", "http://localhost:3001") // Add your frontend URLs
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials() // Required for cookie authentication
                      .SetIsOriginAllowed(origin => true); // Allow credentials with any origin (adjust for production)
            });
        });
    }
}
