using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Enums;
using Magenta.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Magenta.Payments.Application.Handlers;

public class ProcessWithdrawalWebhookHandler
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly IProviderFactory _providerFactory;
    private readonly IWalletClient _walletClient;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<ProcessWithdrawalWebhookHandler> _logger;

    public ProcessWithdrawalWebhookHandler(
        IWithdrawalRequestRepository withdrawalRepository,
        IPaymentProviderRepository providerRepository,
        IProviderFactory providerFactory,
        IWalletClient walletClient,
        IOutboxRepository outboxRepository,
        ILogger<ProcessWithdrawalWebhookHandler> logger)
    {
        _withdrawalRepository = withdrawalRepository;
        _providerRepository = providerRepository;
        _providerFactory = providerFactory;
        _walletClient = walletClient;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task HandleAsync(
        ProcessWithdrawalWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing withdrawal webhook from provider {ProviderId}: {EventType}",
            command.ProviderId, command.EventType);

        // Extract withdrawal reference
        var withdrawalId = ExtractWithdrawalId(command.Payload);
        var txHash = command.Payload.GetValueOrDefault("txHash")?.ToString();

        if (withdrawalId == null)
        {
            _logger.LogWarning("No withdrawal ID found in webhook payload");
            return;
        }

        var withdrawal = await _withdrawalRepository.GetByIdAsync(withdrawalId.Value, cancellationToken);
        if (withdrawal == null)
        {
            _logger.LogWarning("Withdrawal not found: {WithdrawalId}", withdrawalId);
            return;
        }

        // Update status based on event type
        if (command.EventType.Contains("settled") || command.EventType.Contains("confirmed"))
        {
            withdrawal.Status = WithdrawalRequestStatus.SETTLED;
            if (!string.IsNullOrEmpty(txHash))
                withdrawal.TxHash = txHash;
            
            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

            // Publish event
            await _outboxRepository.AddEventAsync(
                "WithdrawalSettled",
                "payments.withdrawal.settled",
                new
                {
                    playerId = withdrawal.PlayerId,
                    amountMinor = withdrawal.AmountMinor,
                    feeMinor = withdrawal.FeeMinor,
                    txHash = withdrawal.TxHash,
                    requestId = withdrawal.WithdrawalId.ToString()
                },
                cancellationToken);
        }
        else if (command.EventType.Contains("failed"))
        {
            withdrawal.Status = WithdrawalRequestStatus.FAILED;
            withdrawal.FailReason = command.Payload.GetValueOrDefault("reason")?.ToString();
            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

            // Publish event
            await _outboxRepository.AddEventAsync(
                "WithdrawalFailed",
                "payments.withdrawal.failed",
                new
                {
                    playerId = withdrawal.PlayerId,
                    amountMinor = withdrawal.AmountMinor,
                    requestId = withdrawal.WithdrawalId.ToString(),
                    reason = withdrawal.FailReason
                },
                cancellationToken);
        }
    }

    private Guid? ExtractWithdrawalId(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("withdrawalId", out var idObj) && Guid.TryParse(idObj.ToString(), out var id))
            return id;
        
        if (payload.TryGetValue("requestId", out var reqIdObj) && Guid.TryParse(reqIdObj.ToString(), out var reqId))
            return reqId;
        
        return null;
    }
}

