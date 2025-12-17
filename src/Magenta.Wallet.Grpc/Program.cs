using Magenta.Wallet.Grpc.Services;
using Magenta.Wallet.Infrastructure.Data;
using Magenta.Wallet.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// Add infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Map gRPC services
app.MapGrpcService<WalletService>();

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
