using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Application.Services;
using Magenta.Payment.Infrastructure.Configuration;
using Magenta.Payment.Infrastructure.Data;
using Magenta.Payment.Infrastructure.Providers;
using Magenta.Payment.Infrastructure.Repositories;
using Magenta.Payment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Payment.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        // Register repositories
        services.AddScoped<IDepositSessionRepository, DepositSessionRepository>();
        services.AddScoped<IDepositRequestRepository, DepositRequestRepository>();
        services.AddScoped<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<IPaymentProviderRepository, PaymentProviderRepository>();
        services.AddScoped<IPaymentUnitOfWork, PaymentUnitOfWork>();

        // Register application services
        services.AddScoped<DepositService>();
        services.AddScoped<WithdrawalService>();

        // Configure RabbitMQ
        services.Configure<RabbitMQConfiguration>(options => configuration.GetSection("RabbitMQ").Bind(options));
        services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();

        // Register gRPC client
        services.AddScoped<IWalletGrpcClient, WalletGrpcClient>();

        // Register payment providers (example - replace with actual providers)
        services.AddScoped<IPaymentProvider, ExamplePaymentProvider>();

        return services;
    }
}
