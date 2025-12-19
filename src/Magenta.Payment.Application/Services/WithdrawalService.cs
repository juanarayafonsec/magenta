using Magenta.Payment.Application.DTOs;
using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Payment.Application.Services;

public class WithdrawalService
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;
    private readonly IPaymentProviderRepository _providerRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IWalletGrpcClient _walletClient;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly ILogger<WithdrawalService> _logger;
    private readonly IEnumerable<IPaymentProvider> _providerAdapters;

    public WithdrawalService(
        IWithdrawalRequestRepository withdrawalRepository,
        IPaymentProviderRepository providerRepository,
        IIdempotencyRepository idempotencyRepository,
        IWalletGrpcClient walletClient,
        IOutboxRepository outboxRepository,
        IPaymentUnitOfWork unitOfWork,
        ILogger<WithdrawalService> logger,
        IEnumerable<IPaymentProvider> providerAdapters)
    {
        _withdrawalRepository = withdrawalRepository ?? throw new ArgumentNullException(nameof(withdrawalRepository));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _idempotencyRepository = idempotencyRepository ?? throw new ArgumentNullException(nameof(idempotencyRepository));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerAdapters = providerAdapters ?? throw new ArgumentNullException(nameof(providerAdapters));
    }

    public async Task<CreateWithdrawalResponse> CreateWithdrawalAsync(
        CreateWithdrawalRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // Check idempotency
        if (await _idempotencyRepository.ExistsAsync("payments", idempotencyKey, cancellationToken))
        {
            var existingTxId = await _idempotencyRepository.GetTxIdAsync("payments", idempotencyKey, cancellationToken);
            if (existingTxId.HasValue)
            {
                var existing = await _withdrawalRepository.GetByIdAsync(existingTxId.Value, cancellationToken);
                if (existing != null)
                {
                    return new CreateWithdrawalResponse
                    {
                        WithdrawalId = existing.WithdrawalId,
                        Status = existing.Status
                    };
                }
            }
        }

        var provider = await _providerRepository.GetByIdAsync(request.ProviderId, cancellationToken);
        if (provider == null || !provider.IsActive)
        {
            throw new InvalidOperationException($"Provider {request.ProviderId} not found or inactive");
        }

        // Reserve withdrawal in Wallet (synchronous gRPC call)
        var walletResult = await _walletClient.ReserveWithdrawalAsync(
            request.PlayerId,
            request.CurrencyNetworkId,
            request.AmountMinor,
            idempotencyKey,
            cancellationToken);

        if (!walletResult.Success)
        {
            throw new InvalidOperationException($"Failed to reserve withdrawal: {walletResult.ErrorMessage}");
        }

        var withdrawal = new WithdrawalRequest
        {
            WithdrawalId = Guid.NewGuid(),
            PlayerId = request.PlayerId,
            ProviderId = request.ProviderId,
            CurrencyNetworkId = request.CurrencyNetworkId,
            AmountMinor = request.AmountMinor,
            TargetAddress = request.TargetAddress,
            Status = "PROCESSING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _withdrawalRepository.CreateAsync(withdrawal, cancellationToken);
            await _idempotencyRepository.CreateAsync(new IdempotencyKey
            {
                Source = "payments",
                IdempotencyKeyValue = idempotencyKey,
                TxId = withdrawal.WithdrawalId
            }, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Send withdrawal to provider (fire and forget, will be polled)
        _ = Task.Run(async () =>
        {
            try
            {
                await SendWithdrawalToProviderAsync(withdrawal, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send withdrawal {WithdrawalId} to provider", withdrawal.WithdrawalId);
            }
        }, cancellationToken);

        return new CreateWithdrawalResponse
        {
            WithdrawalId = withdrawal.WithdrawalId,
            Status = withdrawal.Status
        };
    }

    private async Task SendWithdrawalToProviderAsync(
        WithdrawalRequest withdrawal,
        CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(withdrawal.ProviderId, cancellationToken);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {withdrawal.ProviderId} not found");
        }

        var providerAdapter = GetProviderAdapter(provider.ProviderId);
        if (providerAdapter == null)
        {
            throw new InvalidOperationException($"Provider adapter not found for provider {provider.ProviderId}");
        }

        var result = await providerAdapter.SendWithdrawalAsync(
            withdrawal.CurrencyNetworkId,
            withdrawal.TargetAddress,
            withdrawal.AmountMinor,
            cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (result.Success)
            {
                withdrawal.Status = "BROADCASTED";
                withdrawal.ProviderReference = result.ProviderReference;
                withdrawal.TxHash = result.TxHash;
                withdrawal.UpdatedAt = DateTime.UtcNow;

                // Create informational outbox event
                var outboxEvent = new OutboxEvent
                {
                    EventType = "WithdrawalBroadcasted",
                    RoutingKey = "payments.withdrawal.broadcasted",
                    Payload = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        withdrawalId = withdrawal.WithdrawalId,
                        playerId = withdrawal.PlayerId,
                        currencyNetworkId = withdrawal.CurrencyNetworkId,
                        amountMinor = withdrawal.AmountMinor
                    })),
                    CreatedAt = DateTime.UtcNow
                };
                await _outboxRepository.CreateAsync(outboxEvent, cancellationToken);
            }
            else
            {
                withdrawal.Status = "FAILED";
                withdrawal.FailReason = result.ErrorMessage;
                withdrawal.UpdatedAt = DateTime.UtcNow;

                // Create failure outbox event
                var outboxEvent = new OutboxEvent
                {
                    EventType = "WithdrawalFailed",
                    RoutingKey = "payments.withdrawal.failed",
                    Payload = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        withdrawalId = withdrawal.WithdrawalId,
                        playerId = withdrawal.PlayerId,
                        currencyNetworkId = withdrawal.CurrencyNetworkId,
                        amountMinor = withdrawal.AmountMinor,
                        idempotencyKey = withdrawal.WithdrawalId.ToString()
                    })),
                    CreatedAt = DateTime.UtcNow
                };
                await _outboxRepository.CreateAsync(outboxEvent, cancellationToken);
            }

            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ProcessWithdrawalSettlementAsync(
        Guid withdrawalId,
        string? txHash,
        CancellationToken cancellationToken = default)
    {
        var withdrawal = await _withdrawalRepository.GetByIdAsync(withdrawalId, cancellationToken);
        if (withdrawal == null)
        {
            throw new InvalidOperationException($"Withdrawal {withdrawalId} not found");
        }

        if (withdrawal.Status != "BROADCASTED")
        {
            return;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            withdrawal.Status = "SETTLED";
            if (!string.IsNullOrEmpty(txHash))
            {
                withdrawal.TxHash = txHash;
            }
            withdrawal.UpdatedAt = DateTime.UtcNow;
            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

            // Create outbox event for Wallet
            var outboxEvent = new OutboxEvent
            {
                EventType = "WithdrawalSettled",
                RoutingKey = "payments.withdrawal.settled",
                Payload = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    withdrawalId = withdrawal.WithdrawalId,
                    playerId = withdrawal.PlayerId,
                    currencyNetworkId = withdrawal.CurrencyNetworkId,
                    amountMinor = withdrawal.AmountMinor,
                    feeMinor = withdrawal.FeeMinor,
                    idempotencyKey = withdrawal.WithdrawalId.ToString()
                })),
                CreatedAt = DateTime.UtcNow
            };
            await _outboxRepository.CreateAsync(outboxEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ProcessWithdrawalFailureAsync(
        Guid withdrawalId,
        string failReason,
        CancellationToken cancellationToken = default)
    {
        var withdrawal = await _withdrawalRepository.GetByIdAsync(withdrawalId, cancellationToken);
        if (withdrawal == null)
        {
            throw new InvalidOperationException($"Withdrawal {withdrawalId} not found");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            withdrawal.Status = "FAILED";
            withdrawal.FailReason = failReason;
            withdrawal.UpdatedAt = DateTime.UtcNow;
            await _withdrawalRepository.UpdateAsync(withdrawal, cancellationToken);

            // Create outbox event for Wallet to release hold
            var outboxEvent = new OutboxEvent
            {
                EventType = "WithdrawalFailed",
                RoutingKey = "payments.withdrawal.failed",
                Payload = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    withdrawalId = withdrawal.WithdrawalId,
                    playerId = withdrawal.PlayerId,
                    currencyNetworkId = withdrawal.CurrencyNetworkId,
                    idempotencyKey = withdrawal.WithdrawalId.ToString()
                })),
                CreatedAt = DateTime.UtcNow
            };
            await _outboxRepository.CreateAsync(outboxEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private IPaymentProvider? GetProviderAdapter(int providerId)
    {
        // Simplified - in production, use a proper factory that maps provider IDs to adapters
        return _providerAdapters.FirstOrDefault();
    }
}
