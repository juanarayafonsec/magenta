using Magenta.Wallet.Application.DTOs.Commands;
using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;

namespace Magenta.Wallet.Application.Services;

/// <summary>
/// Service for handling wallet commands with proper transaction management.
/// </summary>
public class WalletCommandService
{
    private readonly ILedgerWriter _ledgerWriter;
    private readonly ICurrencyNetworkResolver _currencyNetworkResolver;
    private readonly ILogger<WalletCommandService> _logger;

    public WalletCommandService(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        ILogger<WalletCommandService> logger)
    {
        _ledgerWriter = ledgerWriter;
        _currencyNetworkResolver = currencyNetworkResolver;
        _logger = logger;
    }

    public async Task HandleAsync(ApplyDepositSettlementCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "payments.deposit.settled", command.IdempotencyKey, cancellationToken);
        
        if (existingTxId.HasValue)
        {
            _logger.LogInformation("Deposit already processed: {IdempotencyKey}", command.IdempotencyKey);
            return;
        }

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var houseAccountId = await _ledgerWriter.EnsureAccountAsync(
                0, currencyNetworkId, AccountType.HOUSE, cancellationToken);
            
            var playerAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.MAIN, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["source"] = "payments.deposit.settled",
                ["txHash"] = command.TxHash,
                ["network"] = command.Network
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            var postings = PostingRules.DepositSettled(command.AmountMinor)
                .Select(p => (p.AccountType == AccountType.HOUSE ? houseAccountId : playerAccountId, 
                             p.Direction, 
                             p.AmountMinor))
                .ToList();

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.DEPOSIT.ToString(),
                command.TxHash,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "payments.deposit.settled",
                command.IdempotencyKey,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleAsync(ReserveWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "wallet.withdrawal.reserved", command.RequestId, cancellationToken);
        
        if (existingTxId.HasValue)
            return;

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var mainAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.MAIN, cancellationToken);
            
            var withdrawHoldAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.WITHDRAW_HOLD, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["requestId"] = command.RequestId,
                ["network"] = command.Network
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            var postings = PostingRules.WithdrawalReserved(command.AmountMinor)
                .Select(p => (p.AccountType == AccountType.MAIN ? mainAccountId : withdrawHoldAccountId,
                             p.Direction,
                             p.AmountMinor))
                .ToList();

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.WITHDRAW_RESERVE.ToString(),
                command.RequestId,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "wallet.withdrawal.reserved",
                command.RequestId,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.withdrawal.reserved",
                "wallet.withdrawal.reserved",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["requestId"] = command.RequestId,
                    ["playerId"] = command.PlayerId,
                    ["currency"] = command.Currency,
                    ["network"] = command.Network,
                    ["amountMinor"] = command.AmountMinor,
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleAsync(FinalizeWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "payments.withdrawal.settled", command.RequestId, cancellationToken);
        
        if (existingTxId.HasValue)
            return;

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var withdrawHoldAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.WITHDRAW_HOLD, cancellationToken);
            
            var houseAccountId = await _ledgerWriter.EnsureAccountAsync(
                0, currencyNetworkId, AccountType.HOUSE, cancellationToken);
            
            var houseFeesAccountId = await _ledgerWriter.EnsureAccountAsync(
                0, currencyNetworkId, AccountType.HOUSE_FEES, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["source"] = "payments.withdrawal.settled",
                ["requestId"] = command.RequestId,
                ["txHash"] = command.TxHash,
                ["feeMinor"] = command.FeeMinor,
                ["network"] = command.Network
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            var postingRules = PostingRules.WithdrawalSettled(command.AmountMinor, command.FeeMinor);
            var postings = postingRules
                .Select(p => (p.AccountType switch
                {
                    AccountType.WITHDRAW_HOLD => withdrawHoldAccountId,
                    AccountType.HOUSE => houseAccountId,
                    AccountType.HOUSE_FEES => houseFeesAccountId,
                    _ => throw new InvalidOperationException($"Unexpected account type: {p.AccountType}")
                }, p.Direction, p.AmountMinor))
                .ToList();

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.WITHDRAW_FINALIZE.ToString(),
                command.RequestId,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "payments.withdrawal.settled",
                command.RequestId,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleAsync(ReleaseWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "payments.withdrawal.failed", command.RequestId, cancellationToken);
        
        if (existingTxId.HasValue)
            return;

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var withdrawHoldAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.WITHDRAW_HOLD, cancellationToken);
            
            var mainAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.MAIN, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["source"] = "payments.withdrawal.failed",
                ["requestId"] = command.RequestId,
                ["network"] = command.Network
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            var postings = PostingRules.WithdrawalFailed(command.AmountMinor)
                .Select(p => (p.AccountType == AccountType.WITHDRAW_HOLD ? withdrawHoldAccountId : mainAccountId,
                             p.Direction,
                             p.AmountMinor))
                .ToList();

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.WITHDRAW_RELEASE.ToString(),
                command.RequestId,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "payments.withdrawal.failed",
                command.RequestId,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleAsync(PlaceBetCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "game.bet", command.BetId, cancellationToken);
        
        if (existingTxId.HasValue)
            return;

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var mainAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.MAIN, cancellationToken);
            
            var houseWagerAccountId = await _ledgerWriter.EnsureAccountAsync(
                0, currencyNetworkId, AccountType.HOUSE_WAGER, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["provider"] = command.Provider,
                ["betId"] = command.BetId,
                ["roundId"] = command.RoundId,
                ["gameCode"] = command.GameCode
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            var postings = PostingRules.Bet(command.AmountMinor)
                .Select(p => (p.AccountType == AccountType.MAIN ? mainAccountId : houseWagerAccountId,
                             p.Direction,
                             p.AmountMinor))
                .ToList();

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.BET.ToString(),
                command.BetId,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "game.bet",
                command.BetId,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleAsync(SettleWinCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "game.win", command.WinId, cancellationToken);
        
        if (existingTxId.HasValue)
            return;

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var houseWagerAccountId = await _ledgerWriter.EnsureAccountAsync(
                0, currencyNetworkId, AccountType.HOUSE_WAGER, cancellationToken);
            
            var mainAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.MAIN, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["provider"] = command.Provider,
                ["winId"] = command.WinId,
                ["roundId"] = command.RoundId,
                ["betId"] = command.BetId
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            var postings = PostingRules.Win(command.AmountMinor)
                .Select(p => (p.AccountType == AccountType.HOUSE_WAGER ? houseWagerAccountId : mainAccountId,
                             p.Direction,
                             p.AmountMinor))
                .ToList();

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.WIN.ToString(),
                command.WinId,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "game.win",
                command.WinId,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task HandleAsync(RollbackCommand command, CancellationToken cancellationToken = default)
    {
        var currencyNetworkId = await _currencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
            command.Currency, command.Network, cancellationToken);

        var existingTxId = await _ledgerWriter.GetTransactionIdByKeyAsync(
            "game.rollback", command.RollbackId, cancellationToken);
        
        if (existingTxId.HasValue)
            return;

        await _ledgerWriter.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var mainAccountId = await _ledgerWriter.EnsureAccountAsync(
                command.PlayerId, currencyNetworkId, AccountType.MAIN, cancellationToken);
            
            var houseWagerAccountId = await _ledgerWriter.EnsureAccountAsync(
                0, currencyNetworkId, AccountType.HOUSE_WAGER, cancellationToken);

            var metadata = new Dictionary<string, object>
            {
                ["referenceType"] = command.ReferenceType,
                ["referenceId"] = command.ReferenceId,
                ["reason"] = command.Reason
            };
            
            if (!string.IsNullOrEmpty(command.CorrelationId))
                metadata["correlationId"] = command.CorrelationId;

            // Rollback requires looking up the original transaction amount
            // For now, we'll need to query the ledger - this is a simplification
            // In production, you'd query the original transaction by referenceId
            // For now, we'll throw an error indicating this needs implementation
            throw new NotImplementedException(
                "Rollback requires querying the original transaction amount. " +
                "Implement lookup by referenceId in RollbackCommand or add AmountMinor to command.");

            var txId = await _ledgerWriter.CreateLedgerTransactionAsync(
                TxType.ROLLBACK.ToString(),
                command.ReferenceId,
                metadata,
                postings,
                cancellationToken);

            await _ledgerWriter.RecordIdempotencyKeyAsync(
                "game.rollback",
                command.RollbackId,
                txId,
                cancellationToken);

            await _ledgerWriter.AddOutboxEventAsync(
                "wallet.balance.changed",
                "wallet.balance.changed",
                new Dictionary<string, object>
                {
                    ["eventId"] = Guid.NewGuid().ToString(),
                    ["occurredAt"] = DateTime.UtcNow,
                    ["playerId"] = command.PlayerId,
                    ["changes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["currency"] = command.Currency,
                            ["network"] = command.Network
                        }
                    },
                    ["correlationId"] = command.CorrelationId ?? string.Empty
                },
                cancellationToken);

            await _ledgerWriter.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _ledgerWriter.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

