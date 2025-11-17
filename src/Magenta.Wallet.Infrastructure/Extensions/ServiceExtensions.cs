using Magenta.Wallet.Application.Handlers;
using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Infrastructure.Data;
using Magenta.Wallet.Infrastructure.Messaging;
using Magenta.Wallet.Infrastructure.Repositories;
using Magenta.Wallet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Wallet.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("WalletDb");
        services.AddDbContext<WalletDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly("Magenta.Wallet.Infrastructure")));

        // Repositories
        services.AddScoped<ILedgerWriter, LedgerWriter>();
        services.AddScoped<IAccountReadModel, AccountReadModel>();
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<IInboxStore, InboxStore>();

        // Domain Services
        services.AddScoped<ICurrencyNetworkResolver, CurrencyNetworkResolver>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // Application Handlers
        services.AddScoped<ApplyDepositSettlementHandler>();
        services.AddScoped<ReserveWithdrawalHandler>();
        services.AddScoped<FinalizeWithdrawalHandler>();
        services.AddScoped<ReleaseWithdrawalHandler>();
        services.AddScoped<PlaceBetHandler>();
        services.AddScoped<SettleWinHandler>();
        services.AddScoped<RollbackHandler>();
        services.AddScoped<GetBalanceHandler>();

        // Payments Event Handlers
        services.AddScoped<PaymentsDepositSettledHandler>();
        services.AddScoped<PaymentsWithdrawalSettledHandler>();
        services.AddScoped<PaymentsWithdrawalFailedHandler>();
        services.AddScoped<PaymentsWithdrawalBroadcastedHandler>();

        // RabbitMQ Configuration
        services.Configure<RabbitMqConfiguration>(configuration.GetSection("Rabbit"));

        // Background Services
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<PaymentsEventsConsumer>();

        return services;
    }
}

