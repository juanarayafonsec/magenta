using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Application.Services;
using Magenta.Wallet.Application.Handlers;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Infrastructure.Configuration;
using Magenta.Wallet.Infrastructure.Data;
using Magenta.Wallet.Infrastructure.Repositories;
using Magenta.Wallet.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Wallet.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWalletServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("WalletDb");
        services.AddDbContext<WalletDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Configuration
        services.Configure<RabbitMQConfiguration>(
            configuration.GetSection("Rabbit"));

        // Repositories
        services.AddScoped<ILedgerWriter, LedgerWriter>();
        services.AddScoped<IAccountReadModel, AccountReadModel>();
        services.AddScoped<IInboxStore, InboxStore>();
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<ICurrencyNetworkResolver, CurrencyNetworkResolver>();

        // Application Services
        services.AddScoped<WalletCommandService>();
        services.AddScoped<GetBalanceHandler>();

        // Background Services
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<PaymentsEventsConsumer>();

        return services;
    }
}




