using Magenta.Wallet.Infrastructure.Data;
using Magenta.Wallet.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Magenta Wallet API",
        Version = "v1",
        Description = "Read-only API for Wallet balances and currencies"
    });
});

// Add infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Add authentication (aligned with Authentication.API)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "MagentaAuth";
        options.Cookie.HttpOnly = true; // Prevent XSS attacks
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // HTTPS only
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict; // CSRF protection
        options.Cookie.IsEssential = true; // Required for GDPR compliance
        
        // Session timeout (aligned with Authentication.API)
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // 30 minutes
        options.SlidingExpiration = false; // No automatic sliding expiration
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SecureCors", policy =>
    {
        policy.WithOrigins("https://localhost:3000", "https://localhost:3001", "http://localhost:3000", "http://localhost:3001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for cookie authentication
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Magenta Wallet API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("SecureCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Wallet database tables created successfully.");
        }
        else
        {
            context.Database.Migrate();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Wallet database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize wallet database: {ErrorMessage}", ex.Message);
        throw;
    }
}

app.Run();
