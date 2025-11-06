using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;

namespace Magenta.Wallet.Application.Interfaces;

/// <summary>
/// Interface for writing ledger transactions with SERIALIZABLE isolation.
/// All operations must be atomic within a transaction.
/// </summary>
public interface ILedgerWriter
{
    /// <summary>
    /// Begins a SERIALIZABLE transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a ledger transaction with postings and updates derived balances atomically.
    /// </summary>
    Task<Guid> CreateLedgerTransactionAsync(
        string txType,
        string? externalRef,
        Dictionary<string, object> metadata,
        List<(long AccountId, Direction Direction, long AmountMinor)> postings,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures an account exists for the given player, currency_network, and account_type.
    /// Returns the account ID.
    /// </summary>
    Task<long> EnsureAccountAsync(
        long playerId,
        int currencyNetworkId,
        AccountType accountType,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures account_balance row exists and locks it with FOR UPDATE.
    /// </summary>
    Task<AccountBalance> EnsureAndLockAccountBalanceAsync(
        long accountId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates account balance and recomputes reserved/cashable.
    /// </summary>
    Task UpdateAccountBalanceAsync(
        AccountBalance balance,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records an idempotency key.
    /// </summary>
    Task RecordIdempotencyKeyAsync(
        string source,
        string idempotencyKey,
        Guid txId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an idempotency key already exists.
    /// </summary>
    Task<bool> IdempotencyKeyExistsAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the transaction ID for an existing idempotency key.
    /// </summary>
    Task<Guid?> GetTransactionIdByKeyAsync(
        string source,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds an event to the outbox.
    /// </summary>
    Task AddOutboxEventAsync(
        string eventType,
        string routingKey,
        Dictionary<string, object> payload,
        CancellationToken cancellationToken = default);
}

