using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Domain.Enums;
using Magenta.Payments.Domain.Interfaces;
using Magenta.Payments.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Payments.Application.Handlers;

public class RequestWithdrawalHandler
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly ICurrencyNetworkResolver _currencyNetworkResolver;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IWalletClient _walletClient;
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<RequestWithdrawalHandler> _logger;

    public RequestWithdrawalHandler(
        IWithdrawalRequestRepository withdrawalRepository,
        IPaymentProviderRepository providerRepository,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyRepository idempotencyRepository,
        IWalletClient walletClient,
        IProviderFactory providerFactory,
        ILogger<RequestWithdrawalHandler> logger)
    {
        _withdrawalRepository = withdrawalRepository;
        _providerRepository = providerRepository;
        _currencyNetworkResolver = currencyNetworkResolver;
        _idempotencyRepository = idempotencyRepository;
        _walletClient = walletClient;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<RequestWithdrawalResponse> HandleAsync(
        RequestWithdrawalCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check idempotency
        var existingWithdrawalId = await _idempotencyRepository.GetRelatedIdAsync(
            "withdrawal", command.IdempotencyKey, cancellationToken);
        
        if (existingWithdrawalId.HasValue)
        {
            var existing = await _withdrawalRepository.GetByIdAsync(existingWithdrawalId.Value, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Returning existing withdrawal: {WithdrawalId}", existingWithdrawalId);
                return new RequestWithdrawalResponse(
                    existing.WithdrawalId,
                    existing.Status?.ToString() ?? "REQUESTED"
                );
            }
        }

        // Resolve currency network
        var currencyNetwork = await _currencyNetworkResolver.ResolveCurrencyNetworkAsync(
            command.Currency, command.Network, cancellationToken);
        
        if (currencyNetwork == null)
            throw new InvalidOperationException($"Currency network not found: {command.Currency}-{command.Network}");

        // Convert to minor units
        var amountMinor = Money.FromMajorUnits(command.AmountMajor, currencyNetwork.Decimals).MinorUnits;

        // Get active provider
        var providers = await _providerRepository.GetActiveProvidersAsync(cancellationToken);
        var provider = providers.FirstOrDefault(p => p.Type == ProviderType.CRYPTO);
        
        if (provider == null)
            throw new InvalidOperationException("No active payment provider found");

        // Reserve funds in Wallet
        var withdrawalId = Guid.NewGuid().ToString();
        var reserveResult = await _walletClient.ReserveWithdrawalAsync(
            command.PlayerId,
            command.Currency,
            command.Network,
            amountMinor,
            withdrawalId,
            null,
            cancellationToken);

        if (!reserveResult.Ok)
            throw new InvalidOperationException($"Failed to reserve withdrawal: {reserveResult.Message}");

        // Create withdrawal request
        var withdrawal = new WithdrawalRequest
        {
            WithdrawalId = Guid.NewGuid(),
            PlayerId = command.PlayerId,
            ProviderId = provider.ProviderId,
            CurrencyNetworkId = currencyNetwork.CurrencyNetworkId,
            AmountMinor = amountMinor,
            TargetAddress = command.TargetAddress,
            Status = WithdrawalRequestStatus.REQUESTED,
            Metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                currency = command.Currency,
                network = command.Network,
                idempotencyKey = command.IdempotencyKey
            }))
        };

        await _withdrawalRepository.CreateAsync(withdrawal, cancellationToken);

        // Record idempotency
        await _idempotencyRepository.TryRecordIdempotencyKeyAsync(
            "withdrawal", command.IdempotencyKey, withdrawal.WithdrawalId, cancellationToken);

        // Execute withdrawal with provider (async)
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteWithdrawalAsync(withdrawal, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing withdrawal {WithdrawalId}", withdrawal.WithdrawalId);
            }
        }, cancellationToken);

        _logger.LogInformation("Created withdrawal request: {WithdrawalId} for player {PlayerId}",
            withdrawal.WithdrawalId, command.PlayerId);

        return new RequestWithdrawalResponse(
            withdrawal.WithdrawalId,
            withdrawal.Status?.ToString() ?? "REQUESTED"
        );
    }

    private async Task ExecuteWithdrawalAsync(
        WithdrawalRequest withdrawal,
        CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(withdrawal.ProviderId, cancellationToken);
        if (provider == null) return;

        var providerImpl = _providerFactory.GetProvider(provider.ProviderId);

        withdrawal.Status = WithdrawalRequestStatus.PROCESSING;
        await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

        var result = await providerImpl.SendWithdrawalAsync(withdrawal, cancellationToken);

        if (result.Success)
        {
            withdrawal.Status = WithdrawalRequestStatus.BROADCASTED;
            withdrawal.TxHash = result.TxHash;
            withdrawal.ProviderReference = result.ProviderReference;
            withdrawal.UpdatedAt = DateTime.UtcNow;
            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

            // Publish withdrawal.broadcasted event
            // This will be handled by outbox
        }
        else
        {
            withdrawal.Status = WithdrawalRequestStatus.FAILED;
            withdrawal.FailReason = result.ErrorMessage;
            withdrawal.UpdatedAt = DateTime.UtcNow;
            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);
        }
    }
}

