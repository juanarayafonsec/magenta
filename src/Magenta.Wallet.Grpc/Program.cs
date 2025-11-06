using Magenta.Wallet.Grpc.Services;
using Magenta.Wallet.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddWalletServices(builder.Configuration);
builder.Services.AddGrpc();

var app = builder.Build();

// Map gRPC service
app.MapGrpcService<WalletServiceImplementation>();

// Ensure database is migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Magenta.Wallet.Infrastructure.Data.WalletDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureCreatedAsync();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Wallet database tables created successfully.");
        }
        else
        {
            await context.Database.MigrateAsync();
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




