using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Domain.Enums;
using Magenta.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Payments.Application.Handlers;

public class ProcessDepositWebhookHandler
{
    private readonly IDepositRequestRepository _depositRepository;
    private readonly IDepositSessionRepository _sessionRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly IProviderFactory _providerFactory;
    private readonly IWalletClient _walletClient;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<ProcessDepositWebhookHandler> _logger;

    public ProcessDepositWebhookHandler(
        IDepositRequestRepository depositRepository,
        IDepositSessionRepository sessionRepository,
        IPaymentProviderRepository providerRepository,
        IProviderFactory providerFactory,
        IWalletClient walletClient,
        IOutboxRepository outboxRepository,
        ILogger<ProcessDepositWebhookHandler> logger)
    {
        _depositRepository = depositRepository;
        _sessionRepository = sessionRepository;
        _providerRepository = providerRepository;
        _providerFactory = providerFactory;
        _walletClient = walletClient;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task HandleAsync(
        ProcessDepositWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing deposit webhook from provider {ProviderId}: {EventType}",
            command.ProviderId, command.EventType);

        if (command.EventType != "deposit_detected")
        {
            _logger.LogWarning("Unknown webhook event type: {EventType}", command.EventType);
            return;
        }

        // Extract txHash from payload
        var txHash = command.Payload.GetValueOrDefault("txHash")?.ToString() 
            ?? command.Payload.GetValueOrDefault("transactionHash")?.ToString();
        
        if (string.IsNullOrEmpty(txHash))
        {
            _logger.LogWarning("No txHash found in webhook payload");
            return;
        }

        // Check if deposit already exists
        var existingDeposit = await _depositRepository.GetByTxHashAsync(txHash, cancellationToken);
        if (existingDeposit != null)
        {
            _logger.LogInformation("Deposit already exists for txHash: {TxHash}", txHash);
            return;
        }

        // Get provider and verify deposit
        var provider = await _providerRepository.GetByIdAsync(command.ProviderId, cancellationToken);
        if (provider == null)
        {
            _logger.LogError("Provider not found: {ProviderId}", command.ProviderId);
            return;
        }

        var providerImpl = _providerFactory.GetProvider(provider.ProviderId);
        var verifyResult = await providerImpl.VerifyDepositAsync(txHash, cancellationToken);

        if (!verifyResult.IsValid)
        {
            _logger.LogWarning("Deposit verification failed for txHash: {TxHash}", txHash);
            return;
        }

        // Extract player info from payload or session
        var playerId = ExtractPlayerId(command.Payload);
        var currencyNetworkId = ExtractCurrencyNetworkId(command.Payload);

        if (playerId == null || currencyNetworkId == null)
        {
            _logger.LogWarning("Missing playerId or currencyNetworkId in webhook payload");
            return;
        }

        // Create deposit request
        var deposit = new DepositRequest
        {
            DepositId = Guid.NewGuid(),
            PlayerId = playerId.Value,
            ProviderId = command.ProviderId,
            CurrencyNetworkId = currencyNetworkId.Value,
            TxHash = txHash,
            AmountMinor = verifyResult.AmountMinor,
            ConfirmationsReceived = verifyResult.ConfirmationsReceived,
            ConfirmationsRequired = verifyResult.ConfirmationsRequired,
            Status = verifyResult.ConfirmationsReceived >= verifyResult.ConfirmationsRequired
                ? DepositRequestStatus.CONFIRMED
                : DepositRequestStatus.PENDING,
            Metadata = JsonDocument.Parse(JsonSerializer.Serialize(command.Payload))
        };

        await _depositRepository.CreateAsync(deposit, cancellationToken);

        // If confirmed, settle immediately
        if (deposit.Status == DepositRequestStatus.CONFIRMED)
        {
            await SettleDepositAsync(deposit, cancellationToken);
        }

        _logger.LogInformation("Processed deposit webhook: DepositId={DepositId}, TxHash={TxHash}",
            deposit.DepositId, txHash);
    }

    private async Task SettleDepositAsync(
        DepositRequest deposit,
        CancellationToken cancellationToken)
    {
        // Get currency/network info (simplified - would need resolver)
        var currency = "USDT"; // Would resolve from currencyNetworkId
        var network = "TRON";

        // Call Wallet to settle
        var idempotencyKey = $"deposit_{deposit.DepositId}";
        var result = await _walletClient.ApplyDepositSettlementAsync(
            deposit.PlayerId,
            currency,
            network,
            deposit.AmountMinor,
            deposit.TxHash,
            idempotencyKey,
            null,
            cancellationToken);

        if (result.Ok)
        {
            deposit.Status = DepositRequestStatus.SETTLED;
            await _depositRepository.UpdateAsync(deposit, cancellationToken);

            // Publish event
            await _outboxRepository.AddEventAsync(
                "DepositSettled",
                "payments.deposit.settled",
                new
                {
                    playerId = deposit.PlayerId,
                    currency = currency,
                    network = network,
                    amountMinor = deposit.AmountMinor,
                    txHash = deposit.TxHash,
                    idempotencyKey = idempotencyKey
                },
                cancellationToken);
        }
    }

    private long? ExtractPlayerId(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("playerId", out var playerIdObj) && playerIdObj is long playerId)
            return playerId;
        
        if (payload.TryGetValue("playerId", out var playerIdStr) && long.TryParse(playerIdStr.ToString(), out var parsed))
            return parsed;
        
        return null;
    }

    private int? ExtractCurrencyNetworkId(Dictionary<string, object> payload)
    {
        if (payload.TryGetValue("currencyNetworkId", out var cnIdObj) && cnIdObj is int cnId)
            return cnId;
        
        return null;
    }
}

