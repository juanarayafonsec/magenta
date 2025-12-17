using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Application.Services;
using Magenta.Wallet.Infrastructure.Configuration;
using Magenta.Wallet.Infrastructure.Data;
using Magenta.Wallet.Infrastructure.Repositories;
using Magenta.Wallet.Infrastructure.Services;
using Magenta.Wallet.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Wallet.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context with SERIALIZABLE isolation support
        services.AddDbContext<WalletDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        // Register repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<IBalanceRepository, BalanceRepository>();
        services.AddScoped<IWalletUnitOfWork, WalletUnitOfWork>();

        // Register application services
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<ICurrencyService, CurrencyService>();

        // Configure RabbitMQ
        services.Configure<RabbitMQConfiguration>(options => configuration.GetSection("RabbitMQ").Bind(options));
        services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();

        // Register background workers
        services.AddHostedService<OutboxDispatcherWorker>();
        services.AddHostedService<InboxProcessorWorker>();
        services.AddHostedService<RabbitMQInboxListener>();
        services.AddHostedService<WithdrawalReconciliationWorker>();
        services.AddHostedService<DepositReconciliationWorker>();
        services.AddHostedService<GameTransactionReconciliationWorker>();

        return services;
    }
}
