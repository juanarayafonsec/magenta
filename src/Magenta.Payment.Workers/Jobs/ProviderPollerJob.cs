using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Application.Services;
using Magenta.Payment.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Magenta.Payment.Workers.Jobs;

public class ProviderPollerJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProviderPollerJob> _logger;

    public ProviderPollerJob(
        IServiceProvider serviceProvider,
        ILogger<ProviderPollerJob> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var depositRepository = scope.ServiceProvider.GetRequiredService<IDepositRequestRepository>();
        var withdrawalRepository = scope.ServiceProvider.GetRequiredService<IWithdrawalRequestRepository>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IPaymentProviderRepository>();
        var depositService = scope.ServiceProvider.GetRequiredService<DepositService>();
        var withdrawalService = scope.ServiceProvider.GetRequiredService<WithdrawalService>();
        var providerAdapters = scope.ServiceProvider.GetServices<IPaymentProvider>();

        _logger.LogInformation("Starting provider poller job");

        try
        {
            // Poll pending deposits
            var pendingDeposits = await depositRepository.GetPendingVerificationAsync(cancellationToken);
            foreach (var deposit in pendingDeposits)
            {
                await PollDepositAsync(deposit, providerRepository, providerAdapters, depositService, cancellationToken);
            }

            // Poll confirmed deposits pending settlement
            var confirmedDeposits = await depositRepository.GetConfirmedPendingSettlementAsync(cancellationToken);
            foreach (var deposit in confirmedDeposits)
            {
                await depositService.TrySettleDepositAsync(deposit, cancellationToken);
            }

            // Poll processing withdrawals
            var processingWithdrawals = await withdrawalRepository.GetProcessingAsync(cancellationToken);
            foreach (var withdrawal in processingWithdrawals)
            {
                await PollWithdrawalAsync(withdrawal, providerRepository, providerAdapters, cancellationToken);
            }

            // Poll broadcasted withdrawals
            var broadcastedWithdrawals = await withdrawalRepository.GetBroadcastedAsync(cancellationToken);
            foreach (var withdrawal in broadcastedWithdrawals)
            {
                await PollWithdrawalStatusAsync(withdrawal, providerRepository, providerAdapters, withdrawalService, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in provider poller job");
        }
    }

    private async Task PollDepositAsync(
        DepositRequest deposit,
        IPaymentProviderRepository providerRepository,
        IEnumerable<IPaymentProvider> providerAdapters,
        DepositService depositService,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = await providerRepository.GetByIdAsync(deposit.ProviderId, cancellationToken);
            if (provider == null) return;

            var providerAdapter = providerAdapters.FirstOrDefault(); // Simplified
            if (providerAdapter == null) return;

            var verification = await providerAdapter.VerifyDepositAsync(deposit.TxHash, cancellationToken);
            if (verification.IsValid && verification.Confirmations >= deposit.ConfirmationsRequired)
            {
                deposit.Status = "CONFIRMED";
                deposit.ConfirmationsReceived = verification.Confirmations;
                await depositService.TrySettleDepositAsync(deposit, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling deposit {DepositId}", deposit.DepositId);
        }
    }

    private async Task PollWithdrawalAsync(
        WithdrawalRequest withdrawal,
        IPaymentProviderRepository providerRepository,
        IEnumerable<IPaymentProvider> providerAdapters,
        CancellationToken cancellationToken)
    {
        // This would check if withdrawal was sent to provider
        // For now, we assume it's handled in WithdrawalService
    }

    private async Task PollWithdrawalStatusAsync(
        WithdrawalRequest withdrawal,
        IPaymentProviderRepository providerRepository,
        IEnumerable<IPaymentProvider> providerAdapters,
        WithdrawalService withdrawalService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(withdrawal.ProviderReference)) return;

            var provider = await providerRepository.GetByIdAsync(withdrawal.ProviderId, cancellationToken);
            if (provider == null) return;

            var providerAdapter = providerAdapters.FirstOrDefault();
            if (providerAdapter == null) return;

            var status = await providerAdapter.GetTransactionStatusAsync(withdrawal.ProviderReference, cancellationToken);
            if (status.IsFinal && status.Status == "SETTLED")
            {
                await withdrawalService.ProcessWithdrawalSettlementAsync(
                    withdrawal.WithdrawalId, status.TxHash, cancellationToken);
            }
            else if (status.IsFinal && status.Status == "FAILED")
            {
                await withdrawalService.ProcessWithdrawalFailureAsync(
                    withdrawal.WithdrawalId, "Provider reported failure", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling withdrawal status {WithdrawalId}", withdrawal.WithdrawalId);
        }
    }
}
