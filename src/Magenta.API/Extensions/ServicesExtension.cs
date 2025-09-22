namespace Magenta.API.Extensions;
public static class ServicesExtension
{
    public static void AddServices(this IServiceCollection services)
    {
        // Add services to the container
        services.AddControllers();

        // Add API Explorer for Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwagger();
        services.AddCors();

    }

    private static void AddSwagger(this IServiceCollection services)
    {
        // Add Swagger/OpenAPI
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Magenta API",
                Version = "v1",
                Description = "A clean architecture ASP.NET Core Web API for user registration",
                Contact = new() { Name = "Magenta Team" }
            });
        });
    }

    private static void AddCors(this IServiceCollection services)
    {
        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }
}
