using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using System.Text.Json;

namespace Magenta.Wallet.Application.Interfaces;

/// <summary>
/// Writes ledger transactions and postings, updates derived balances, and manages idempotency and outbox.
/// All operations must be within a SERIALIZABLE transaction.
/// </summary>
public interface ILedgerWriter
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<Account> EnsureAccountAsync(long playerId, int currencyNetworkId, AccountType accountType, CancellationToken cancellationToken = default);
    Task<AccountBalance> EnsureAccountBalanceAsync(long accountId, CancellationToken cancellationToken = default);
    Task<Guid> WriteTransactionAsync(
        TxType txType,
        string? externalRef,
        JsonDocument metadata,
        List<(long AccountId, Direction Direction, long AmountMinor)> postings,
        Dictionary<long, AccountBalance> balanceUpdates,
        CancellationToken cancellationToken = default);
    Task RecordIdempotencyKeyAsync(string source, string idempotencyKey, Guid txId, CancellationToken cancellationToken = default);
    Task AddOutboxEventAsync(string eventType, string routingKey, JsonDocument payload, CancellationToken cancellationToken = default);
}

public interface IDbTransaction : IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

