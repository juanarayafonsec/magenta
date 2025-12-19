using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Application.Services;
using Magenta.Payment.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Magenta.Payment.Workers.Jobs;

public class ReconciliationJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReconciliationJob> _logger;

    public ReconciliationJob(
        IServiceProvider serviceProvider,
        ILogger<ReconciliationJob> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var depositRepository = scope.ServiceProvider.GetRequiredService<IDepositRequestRepository>();
        var withdrawalRepository = scope.ServiceProvider.GetRequiredService<IWithdrawalRequestRepository>();
        var depositService = scope.ServiceProvider.GetRequiredService<DepositService>();

        _logger.LogInformation("Starting reconciliation job");

        try
        {
            // Find stuck deposits (CONFIRMED but not SETTLED for too long)
            var confirmedDeposits = await depositRepository.GetConfirmedPendingSettlementAsync(cancellationToken);
            var stuckDeposits = confirmedDeposits
                .Where(d => (DateTime.UtcNow - d.UpdatedAt).TotalHours > 1)
                .ToList();

            foreach (var deposit in stuckDeposits)
            {
                _logger.LogWarning("Found stuck deposit {DepositId}, attempting settlement", deposit.DepositId);
                await depositService.TrySettleDepositAsync(deposit, cancellationToken);
            }

            // Find stuck withdrawals (PROCESSING or BROADCASTED for too long)
            var processingWithdrawals = await withdrawalRepository.GetProcessingAsync(cancellationToken);
            var broadcastedWithdrawals = await withdrawalRepository.GetBroadcastedAsync(cancellationToken);
            var stuckWithdrawals = processingWithdrawals
                .Concat(broadcastedWithdrawals)
                .Where(w => (DateTime.UtcNow - w.UpdatedAt).TotalHours > 24)
                .ToList();

            foreach (var withdrawal in stuckWithdrawals)
            {
                _logger.LogWarning("Found stuck withdrawal {WithdrawalId}, may need manual review", withdrawal.WithdrawalId);
                // In production, flag for manual review or query provider status
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in reconciliation job");
        }
    }
}
