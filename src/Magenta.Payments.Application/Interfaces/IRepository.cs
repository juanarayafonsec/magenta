using Magenta.Payments.Domain.Entities;

namespace Magenta.Payments.Application.Interfaces;

public interface IDepositSessionRepository
{
    Task<DepositSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<DepositSession> CreateAsync(DepositSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(DepositSession session, CancellationToken cancellationToken = default);
}

public interface IDepositRequestRepository
{
    Task<DepositRequest?> GetByIdAsync(Guid depositId, CancellationToken cancellationToken = default);
    Task<DepositRequest?> GetByTxHashAsync(string txHash, CancellationToken cancellationToken = default);
    Task<DepositRequest> CreateAsync(DepositRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(DepositRequest request, CancellationToken cancellationToken = default);
    Task<List<DepositRequest>> GetPendingDepositsAsync(CancellationToken cancellationToken = default);
}

public interface IWithdrawalRequestRepository
{
    Task<WithdrawalRequest?> GetByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default);
    Task<WithdrawalRequest> CreateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<List<WithdrawalRequest>> GetPendingWithdrawalsAsync(CancellationToken cancellationToken = default);
}

public interface IPaymentProviderRepository
{
    Task<PaymentProvider?> GetByIdAsync(int providerId, CancellationToken cancellationToken = default);
    Task<List<PaymentProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default);
}

public interface IIdempotencyRepository
{
    Task<bool> TryRecordIdempotencyKeyAsync(string source, string idempotencyKey, Guid? relatedId, CancellationToken cancellationToken = default);
    Task<Guid?> GetRelatedIdAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IOutboxRepository
{
    Task AddEventAsync(string eventType, string routingKey, object payload, CancellationToken cancellationToken = default);
    Task<List<OutboxEvent>> GetUnpublishedEventsAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsPublishedAsync(long eventId, CancellationToken cancellationToken = default);
}

public interface IInboxRepository
{
    Task<bool> TryRecordInboxEventAsync(string source, string idempotencyKey, object payload, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
}

public record OutboxEvent(
    long Id,
    string EventType,
    string RoutingKey,
    object Payload,
    DateTime CreatedAt
);

