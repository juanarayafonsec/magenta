using Magenta.Registration.Infrastructure.Extensions;

namespace Magenta.Registration.API.Extensions
{
    public static class ApplicationServiceExtension
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add services to the container
            services.AddControllers();

            // Add API Explorer for Swagger
            services.AddEndpointsApiExplorer();

            // Add Swagger/OpenAPI with enhanced security documentation
            services.AddSwagger();

            // Add CORS with enhanced security for cookie authentication
            services.AddCors();

            // Add infrastructure services
            services.AddInfrastructure(configuration);

        }

        private static void AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "Magenta Registration API",
                    Version = "v1",
                    Description = "A clean architecture ASP.NET Core Web API for user registration",
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
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins("https://localhost:3001")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials(); // This is required for withCredentials: true
                });
            });
        }
    }
}
