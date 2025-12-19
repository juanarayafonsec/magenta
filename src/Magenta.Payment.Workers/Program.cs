using Hangfire;
using Hangfire.PostgreSql;
using Magenta.Payment.Infrastructure.Data;
using Magenta.Payment.Infrastructure.Extensions;
using Magenta.Payment.Workers.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Add Hangfire
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    });
});

builder.Services.AddHangfireServer();

// Register jobs
builder.Services.AddScoped<ProviderPollerJob>();
builder.Services.AddScoped<WebhookInboxProcessorJob>();
builder.Services.AddScoped<ReconciliationJob>();
builder.Services.AddScoped<OutboxPublisherJob>();

var app = builder.Build();

// Initialize Payment database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    try
    {
        if (environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
        }
        else
        {
            context.Database.Migrate();
        }
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Payment database initialized for Workers host.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize payment database: {ErrorMessage}", ex.Message);
        throw;
    }
}

// Schedule recurring jobs
RecurringJob.AddOrUpdate<ProviderPollerJob>(
    "provider-poller",
    job => job.ExecuteAsync(CancellationToken.None),
    "*/5 * * * *"); // Every 5 minutes

RecurringJob.AddOrUpdate<WebhookInboxProcessorJob>(
    "webhook-inbox-processor",
    job => job.ExecuteAsync(CancellationToken.None),
    "*/1 * * * *"); // Every minute

RecurringJob.AddOrUpdate<ReconciliationJob>(
    "reconciliation",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 */1 * * *"); // Every hour

RecurringJob.AddOrUpdate<OutboxPublisherJob>(
    "outbox-publisher",
    job => job.ExecuteAsync(CancellationToken.None),
    "*/1 * * * *"); // Every minute (runs continuously internally)

app.Run();
