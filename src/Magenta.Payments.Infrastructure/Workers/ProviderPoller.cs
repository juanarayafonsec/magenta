using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Enums;
using Magenta.Payments.Domain.Interfaces;
using Magenta.Payments.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Magenta.Payments.Infrastructure.Workers;

public class ProviderPoller : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProviderPoller> _logger;

    public ProviderPoller(
        IServiceProvider serviceProvider,
        ILogger<ProviderPoller> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingDepositsAsync(stoppingToken);
                await PollPendingWithdrawalsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProviderPoller");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task PollPendingDepositsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var depositRepository = scope.ServiceProvider.GetRequiredService<IDepositRequestRepository>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IPaymentProviderRepository>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IProviderFactory>();

        var pendingDeposits = await depositRepository.GetPendingDepositsAsync(cancellationToken);

        foreach (var deposit in pendingDeposits)
        {
            try
            {
                var provider = await providerRepository.GetByIdAsync(deposit.ProviderId, cancellationToken);
                if (provider == null) continue;

                var providerImpl = providerFactory.GetProvider(provider.ProviderId);
                var status = await providerImpl.GetTransactionStatusAsync(deposit.TxHash, cancellationToken);

                if (status.ConfirmationsReceived >= deposit.ConfirmationsRequired && deposit.Status != DepositRequestStatus.CONFIRMED)
                {
                    deposit.Status = DepositRequestStatus.CONFIRMED;
                    deposit.ConfirmationsReceived = status.ConfirmationsReceived;
                    await depositRepository.UpdateAsync(deposit, cancellationToken);

                    // Trigger settlement if not already settled
                    if (deposit.Status != DepositRequestStatus.SETTLED)
                    {
                        // Settlement would be handled by another handler
                        _logger.LogInformation("Deposit {DepositId} confirmed, ready for settlement", deposit.DepositId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling deposit {DepositId}", deposit.DepositId);
            }
        }
    }

    private async Task PollPendingWithdrawalsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var withdrawalRepository = scope.ServiceProvider.GetRequiredService<IWithdrawalRequestRepository>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IPaymentProviderRepository>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IProviderFactory>();

        var pendingWithdrawals = await withdrawalRepository.GetPendingWithdrawalsAsync(cancellationToken);

        foreach (var withdrawal in pendingWithdrawals)
        {
            try
            {
                if (string.IsNullOrEmpty(withdrawal.TxHash)) continue;

                var provider = await providerRepository.GetByIdAsync(withdrawal.ProviderId, cancellationToken);
                if (provider == null) continue;

                var providerImpl = providerFactory.GetProvider(provider.ProviderId);
                var status = await providerImpl.GetTransactionStatusAsync(withdrawal.TxHash, cancellationToken);

                if (status.IsFinal && withdrawal.Status != WithdrawalRequestStatus.SETTLED)
                {
                    withdrawal.Status = WithdrawalRequestStatus.SETTLED;
                    await withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

                    _logger.LogInformation("Withdrawal {WithdrawalId} settled", withdrawal.WithdrawalId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling withdrawal {WithdrawalId}", withdrawal.WithdrawalId);
            }
        }
    }
}

