using Magenta.Wallet.Grpc.Services;
using Magenta.Wallet.Infrastructure.Data;
using Magenta.Wallet.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddGrpc();

var app = builder.Build();

// Configure gRPC
app.MapGrpcService<WalletService>();

// Health check
app.MapGet("/health", () => "OK");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
            await SeedData.SeedAsync(context);
            Log.Information("Wallet database tables created and seeded successfully.");
        }
        else
        {
            context.Database.Migrate();
            Log.Information("Wallet database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize wallet database: {ErrorMessage}", ex.Message);
        throw;
    }
}

app.Run();

public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Correlation ID would be extracted from context in production
    }
}

