using Magenta.Payments.Application.Handlers;
using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Infrastructure.Clients;
using Magenta.Payments.Infrastructure.Data;
using Magenta.Payments.Infrastructure.Messaging;
using Magenta.Payments.Infrastructure.Providers;
using Magenta.Payments.Infrastructure.Repositories;
using Magenta.Payments.Infrastructure.Services;
using Magenta.Payments.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Payments.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("PaymentsDb");
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly("Magenta.Payments.Infrastructure")));

        // Repositories
        services.AddScoped<IDepositSessionRepository, DepositSessionRepository>();
        services.AddScoped<IDepositRequestRepository, DepositRequestRepository>();
        services.AddScoped<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
        services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();

        // Services
        services.AddScoped<ICurrencyNetworkResolver, CurrencyNetworkResolver>();
        services.AddScoped<IProviderFactory, ProviderFactory>();

        // Providers
        services.AddScoped<MockPaymentProvider>();
        services.AddScoped<IPaymentProvider, MockPaymentProvider>(sp => sp.GetRequiredService<MockPaymentProvider>());

        // Wallet gRPC Client
        var walletGrpcUrl = configuration["Wallet:GrpcUrl"] ?? "http://localhost:5001";
        services.AddSingleton<IWalletClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<WalletGrpcClient>>();
            return new WalletGrpcClient(walletGrpcUrl, logger);
        });

        // Application Handlers
        services.AddScoped<CreateDepositSessionHandler>();
        services.AddScoped<RequestWithdrawalHandler>();
        services.AddScoped<ProcessDepositWebhookHandler>();
        services.AddScoped<ProcessWithdrawalWebhookHandler>();

        // RabbitMQ Configuration
        services.Configure<RabbitMqConfiguration>(configuration.GetSection("Rabbit"));

        // Background Services
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<InboxConsumer>();
        services.AddHostedService<ProviderPoller>();

        return services;
    }
}

