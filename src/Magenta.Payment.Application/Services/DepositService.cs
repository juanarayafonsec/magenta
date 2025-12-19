using Magenta.Payment.Application.DTOs;
using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Payment.Application.Services;

public class DepositService
{
    private readonly IDepositSessionRepository _sessionRepository;
    private readonly IDepositRequestRepository _depositRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IWalletGrpcClient _walletClient;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<DepositService> _logger;
    private readonly IEnumerable<IPaymentProvider> _providerAdapters;

    public DepositService(
        IDepositSessionRepository sessionRepository,
        IDepositRequestRepository depositRepository,
        IPaymentProviderRepository providerRepository,
        IIdempotencyRepository idempotencyRepository,
        IWalletGrpcClient walletClient,
        IOutboxRepository outboxRepository,
        IPaymentUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<DepositService> logger,
        IEnumerable<IPaymentProvider> providerAdapters)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _depositRepository = depositRepository ?? throw new ArgumentNullException(nameof(depositRepository));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _idempotencyRepository = idempotencyRepository ?? throw new ArgumentNullException(nameof(idempotencyRepository));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerAdapters = providerAdapters ?? throw new ArgumentNullException(nameof(providerAdapters));
    }

    public async Task<CreateDepositSessionResponse> CreateDepositSessionAsync(
        CreateDepositSessionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // Check idempotency
        if (await _idempotencyRepository.ExistsAsync("payments", idempotencyKey, cancellationToken))
        {
            var existingTxId = await _idempotencyRepository.GetTxIdAsync("payments", idempotencyKey, cancellationToken);
            if (existingTxId.HasValue)
            {
                var existingSession = await _sessionRepository.GetByIdAsync(existingTxId.Value, cancellationToken);
                if (existingSession != null)
                {
                    return new CreateDepositSessionResponse
                    {
                        SessionId = existingSession.SessionId,
                        Address = existingSession.Address,
                        MemoOrTag = existingSession.MemoOrTag,
                        ExpiresAt = existingSession.ExpiresAt,
                        Status = existingSession.Status
                    };
                }
            }
        }

        var provider = await _providerRepository.GetByIdAsync(request.ProviderId, cancellationToken);
        if (provider == null || !provider.IsActive)
        {
            throw new InvalidOperationException($"Provider {request.ProviderId} not found or inactive");
        }

        // Get provider adapter (simplified - in production, use a factory)
        var providerAdapter = GetProviderAdapter(provider.ProviderId);
        if (providerAdapter == null)
        {
            throw new InvalidOperationException($"Provider adapter not found for provider {provider.ProviderId}");
        }

        // Create deposit session with provider
        var sessionResult = await providerAdapter.CreateDepositSessionAsync(
            request.CurrencyNetworkId,
            request.ExpectedAmountMinor,
            request.ConfirmationsRequired,
            cancellationToken);

        var expiresAt = DateTime.UtcNow.AddHours(24); // Default 24 hours expiry

        var session = new DepositSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = request.PlayerId,
            ProviderId = request.ProviderId,
            CurrencyNetworkId = request.CurrencyNetworkId,
            Address = sessionResult.Address,
            MemoOrTag = sessionResult.MemoOrTag,
            ProviderReference = sessionResult.ProviderReference,
            ExpectedAmountMinor = request.ExpectedAmountMinor,
            ConfirmationsRequired = request.ConfirmationsRequired,
            Status = "OPEN",
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _sessionRepository.CreateAsync(session, cancellationToken);
            await _idempotencyRepository.CreateAsync(new IdempotencyKey
            {
                Source = "payments",
                IdempotencyKeyValue = idempotencyKey,
                TxId = session.SessionId
            }, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new CreateDepositSessionResponse
        {
            SessionId = session.SessionId,
            Address = session.Address,
            MemoOrTag = session.MemoOrTag,
            ExpiresAt = session.ExpiresAt,
            Status = session.Status
        };
    }

    public async Task ProcessDepositWebhookAsync(
        int providerId,
        string txHash,
        long? playerId = null,
        CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {providerId} not found");
        }

        var providerAdapter = GetProviderAdapter(provider.ProviderId);
        if (providerAdapter == null)
        {
            throw new InvalidOperationException($"Provider adapter not found for provider {provider.ProviderId}");
        }

        // Verify deposit
        var verification = await providerAdapter.VerifyDepositAsync(txHash, cancellationToken);
        if (!verification.IsValid)
        {
            _logger.LogWarning("Deposit verification failed for txHash {TxHash}: {Error}",
                txHash, verification.ErrorMessage);
            return;
        }

        // Check if deposit already exists
        var existingDeposit = await _depositRepository.GetByTxHashAsync(txHash, cancellationToken);
        if (existingDeposit != null)
        {
            _logger.LogInformation("Deposit already exists for txHash {TxHash}", txHash);
            // Still try to settle if it's confirmed but not settled
            if (existingDeposit.Status == "CONFIRMED")
            {
                await TrySettleDepositAsync(existingDeposit, cancellationToken);
            }
            return;
        }

        // Try to find session by address (if playerId not provided, extract from session)
        DepositSession? session = null;
        if (playerId.HasValue)
        {
            // Find session by player and provider
            // Note: This is simplified - in production, you'd match by address from verification
        }

        // Find or create deposit request
        var deposit = new DepositRequest
        {
            DepositId = Guid.NewGuid(),
            SessionId = session?.SessionId,
            PlayerId = playerId ?? session?.PlayerId ?? 0, // Extract from session or webhook payload
            ProviderId = providerId,
            CurrencyNetworkId = verification.CurrencyNetworkId,
            TxHash = txHash,
            AmountMinor = verification.AmountMinor,
            ConfirmationsReceived = verification.Confirmations,
            ConfirmationsRequired = session?.ConfirmationsRequired ?? 1,
            Status = verification.Confirmations >= 1 ? "CONFIRMED" : "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (deposit.PlayerId == 0)
        {
            _logger.LogWarning("Cannot create deposit without playerId for txHash {TxHash}", txHash);
            return;
        }

        await _depositRepository.CreateAsync(deposit, cancellationToken);

        // If confirmed, try to settle
        if (deposit.Status == "CONFIRMED")
        {
            await TrySettleDepositAsync(deposit, cancellationToken);
        }
    }

    public async Task TrySettleDepositAsync(
        DepositRequest deposit,
        CancellationToken cancellationToken = default)
    {
        if (deposit.Status != "CONFIRMED")
        {
            return;
        }

        // Call Wallet to settle deposit
        var walletResult = await _walletClient.ApplyDepositSettlementAsync(
            deposit.PlayerId,
            deposit.CurrencyNetworkId,
            deposit.AmountMinor,
            deposit.TxHash,
            cancellationToken);

        if (!walletResult.Success)
        {
            _logger.LogError("Failed to settle deposit {DepositId} with Wallet: {Error}",
                deposit.DepositId, walletResult.ErrorMessage);
            return;
        }

        // Update deposit status and session in transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            deposit.Status = "SETTLED";
            deposit.UpdatedAt = DateTime.UtcNow;
            await _depositRepository.UpdateAsync(deposit, cancellationToken);

            // Update session if exists
            if (deposit.SessionId.HasValue)
            {
                var session = await _sessionRepository.GetByIdAsync(deposit.SessionId.Value, cancellationToken);
                if (session != null)
                {
                    session.Status = "COMPLETED";
                    session.UpdatedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(session, cancellationToken);
                }
            }

            // Create outbox event
            var outboxEvent = new OutboxEvent
            {
                EventType = "DepositSettled",
                RoutingKey = "payments.deposit.settled",
                Payload = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    depositId = deposit.DepositId,
                    playerId = deposit.PlayerId,
                    currencyNetworkId = deposit.CurrencyNetworkId,
                    amountMinor = deposit.AmountMinor,
                    transactionHash = deposit.TxHash
                })),
                CreatedAt = DateTime.UtcNow
            };
            await _outboxRepository.CreateAsync(outboxEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Deposit {DepositId} settled successfully", deposit.DepositId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update deposit status after wallet settlement");
            // Note: Wallet already credited, so we need reconciliation to fix this
        }
    }

    private IPaymentProvider? GetProviderAdapter(int providerId)
    {
        // Simplified - in production, use a proper factory that maps provider IDs to adapters
        // For now, return the first available adapter (assuming single provider setup)
        return _providerAdapters.FirstOrDefault();
    }
}
