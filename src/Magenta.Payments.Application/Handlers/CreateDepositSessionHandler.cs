using Magenta.Payments.Application.DTOs;
using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Domain.Enums;
using Magenta.Payments.Domain.Interfaces;
using Magenta.Payments.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Payments.Application.Handlers;

public class CreateDepositSessionHandler
{
    private readonly IDepositSessionRepository _sessionRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly ICurrencyNetworkResolver _currencyNetworkResolver;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<CreateDepositSessionHandler> _logger;

    public CreateDepositSessionHandler(
        IDepositSessionRepository sessionRepository,
        IPaymentProviderRepository providerRepository,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyRepository idempotencyRepository,
        IProviderFactory providerFactory,
        ILogger<CreateDepositSessionHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _providerRepository = providerRepository;
        _currencyNetworkResolver = currencyNetworkResolver;
        _idempotencyRepository = idempotencyRepository;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<CreateDepositSessionResponse> HandleAsync(
        CreateDepositSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check idempotency
        var existingSessionId = await _idempotencyRepository.GetRelatedIdAsync(
            "deposit_session", command.IdempotencyKey, cancellationToken);
        
        if (existingSessionId.HasValue)
        {
            var existingSession = await _sessionRepository.GetByIdAsync(existingSessionId.Value, cancellationToken);
            if (existingSession != null)
            {
                _logger.LogInformation("Returning existing deposit session: {SessionId}", existingSessionId);
                return new CreateDepositSessionResponse(
                    existingSession.SessionId,
                    existingSession.Address,
                    GenerateQrUri(existingSession.Address, command.Currency, command.Network, command.ExpectedAmountMajor),
                    existingSession.ExpiresAt ?? DateTime.UtcNow.AddHours(1),
                    existingSession.ConfirmationsRequired
                );
            }
        }

        // Resolve currency network
        var currencyNetwork = await _currencyNetworkResolver.ResolveCurrencyNetworkAsync(
            command.Currency, command.Network, cancellationToken);
        
        if (currencyNetwork == null)
            throw new InvalidOperationException($"Currency network not found: {command.Currency}-{command.Network}");

        // Get active provider for this currency network
        var providers = await _providerRepository.GetActiveProvidersAsync(cancellationToken);
        var provider = providers.FirstOrDefault(p => p.Type == ProviderType.CRYPTO);
        
        if (provider == null)
            throw new InvalidOperationException("No active payment provider found");

        // Get provider implementation
        var providerImpl = _providerFactory.GetProvider(provider.ProviderId);

        // Convert amount to minor units
        long? expectedAmountMinor = null;
        if (command.ExpectedAmountMajor.HasValue)
        {
            expectedAmountMinor = Money.FromMajorUnits(command.ExpectedAmountMajor.Value, currencyNetwork.Decimals).MinorUnits;
        }

        // Calculate expiration
        var expiresAt = command.ExpiresInSeconds.HasValue
            ? DateTime.UtcNow.AddSeconds(command.ExpiresInSeconds.Value)
            : DateTime.UtcNow.AddMinutes(30);

        // Create deposit session with provider
        var providerResult = await providerImpl.CreateDepositSessionAsync(
            command.PlayerId,
            currencyNetwork.CurrencyNetworkId,
            expectedAmountMinor,
            null, // min amount
            currencyNetwork.Decimals > 6 ? 12 : 1, // confirmations
            expiresAt,
            cancellationToken);

        // Create session entity
        var session = new DepositSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = command.PlayerId,
            ProviderId = provider.ProviderId,
            CurrencyNetworkId = currencyNetwork.CurrencyNetworkId,
            Address = providerResult.Address,
            MemoOrTag = providerResult.MemoOrTag,
            ProviderReference = providerResult.ProviderReference,
            ExpectedAmountMinor = expectedAmountMinor,
            ConfirmationsRequired = providerResult.ConfirmationsRequired,
            Status = DepositSessionStatus.OPEN,
            ExpiresAt = expiresAt,
            Metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                currency = command.Currency,
                network = command.Network,
                idempotencyKey = command.IdempotencyKey
            }))
        };

        await _sessionRepository.CreateAsync(session, cancellationToken);

        // Record idempotency
        await _idempotencyRepository.TryRecordIdempotencyKeyAsync(
            "deposit_session", command.IdempotencyKey, session.SessionId, cancellationToken);

        _logger.LogInformation("Created deposit session: {SessionId} for player {PlayerId}",
            session.SessionId, command.PlayerId);

        return new CreateDepositSessionResponse(
            session.SessionId,
            session.Address,
            providerResult.QrUri ?? GenerateQrUri(session.Address, command.Currency, command.Network, command.ExpectedAmountMajor),
            expiresAt,
            session.ConfirmationsRequired
        );
    }

    private string? GenerateQrUri(string address, string currency, string network, decimal? amount)
    {
        if (string.IsNullOrEmpty(address)) return null;
        
        var uri = $"{network.ToLower()}:{address}";
        if (amount.HasValue)
            uri += $"?amount={amount}";
        
        return uri;
    }
}

