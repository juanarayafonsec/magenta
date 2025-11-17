using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class ApplyDepositSettlementHandler : BaseHandler
{
    private readonly ILogger<ApplyDepositSettlementHandler> _logger;

    public ApplyDepositSettlementHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<ApplyDepositSettlementHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(ApplyDepositSettlementCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve currency network
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
            {
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");
            }

            // Check idempotency
            var source = "payments.deposit.settled";
            if (await IdempotencyService.IsDuplicateAsync(source, command.IdempotencyKey, cancellationToken))
            {
                _logger.LogWarning("Duplicate deposit settlement request: {IdempotencyKey}", command.IdempotencyKey);
                return new OperationResult(true, "Already processed");
            }

            // Begin transaction
            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);

            try
            {
                // Ensure accounts exist
                var houseAccount = await LedgerWriter.EnsureAccountAsync(0, currencyNetworkId.Value, AccountType.HOUSE, cancellationToken);
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);

                // Get posting rules
                var amount = new Money(command.AmountMinor);
                var postings = PostingRules.DepositSettled(houseAccount.AccountId, playerMainAccount.AccountId, amount);

                // Build metadata
                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    source = "payments.deposit.settled",
                    txHash = command.TxHash,
                    network = command.Network,
                    correlationId = command.CorrelationId
                }));

                // Get current balances for update
                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);

                // Apply postings to balances
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                // Write transaction
                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.DEPOSIT,
                    command.TxHash,
                    metadata,
                    postings.Select(p => (p.AccountId, p.Direction, p.Amount.MinorUnits)).ToList(),
                    balances,
                    cancellationToken);

                // Record idempotency
                await LedgerWriter.RecordIdempotencyKeyAsync(source, command.IdempotencyKey, txId, cancellationToken);

                // Get updated balance for event
                var updatedBalance = balances[playerMainAccount.AccountId];

                // Add outbox events
                await AddBalanceChangedEventAsync(
                    command.PlayerId,
                    command.Currency,
                    command.Network,
                    updatedBalance.BalanceMinor,
                    updatedBalance.CashableMinor,
                    updatedBalance.ReservedMinor,
                    command.CorrelationId,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Deposit settlement processed: PlayerId={PlayerId}, Amount={Amount}, TxHash={TxHash}",
                    command.PlayerId, command.AmountMinor, command.TxHash);

                return new OperationResult(true, "Deposit settled successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit settlement: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
}

