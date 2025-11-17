using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class RollbackHandler : BaseHandler
{
    private readonly ILogger<RollbackHandler> _logger;

    public RollbackHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<RollbackHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(RollbackCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");

            var source = "game.rollback";
            if (await IdempotencyService.IsDuplicateAsync(source, command.RollbackId, cancellationToken))
            {
                _logger.LogWarning("Duplicate rollback: {RollbackId}", command.RollbackId);
                return new OperationResult(true, "Already processed");
            }

            // For rollback, we need to look up the original transaction to get the amount
            // This is simplified - in production, you'd fetch from ledger_transactions
            // For now, we'll require the amount to be passed or fetched
            
            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);
            try
            {
                var houseWagerAccount = await LedgerWriter.EnsureAccountAsync(0, currencyNetworkId.Value, AccountType.HOUSE_WAGER, cancellationToken);
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);

                // Fetch original transaction to get amount
                var originalTx = await DbContext.LedgerTransactions
                    .Where(t => t.ExternalRef == command.ReferenceId && 
                               (t.TxType == Domain.Enums.TxType.BET || t.TxType == Domain.Enums.TxType.WIN))
                    .FirstOrDefaultAsync(cancellationToken);

                if (originalTx == null)
                    return new OperationResult(false, $"Original transaction not found: {command.ReferenceId}");

                // Get posting amount from original transaction
                var originalPosting = await DbContext.LedgerPostings
                    .Where(p => p.TxId == originalTx.TxId && 
                               ((command.ReferenceType == "BET" && p.AccountId == playerMainAccount.AccountId && p.Direction == Direction.DEBIT) ||
                                (command.ReferenceType == "WIN" && p.AccountId == playerMainAccount.AccountId && p.Direction == Direction.CREDIT)))
                    .FirstOrDefaultAsync(cancellationToken);

                if (originalPosting == null)
                    return new OperationResult(false, $"Original posting not found for reference: {command.ReferenceId}");

                var amount = new Money(originalPosting.AmountMinor);
                List<PostingRules.PostingRule> postings;

                if (command.ReferenceType == "BET")
                {
                    postings = PostingRules.RollbackBet(houseWagerAccount.AccountId, playerMainAccount.AccountId, amount);
                }
                else // WIN
                {
                    postings = PostingRules.RollbackWin(playerMainAccount.AccountId, houseWagerAccount.AccountId, amount);
                }

                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    referenceType = command.ReferenceType,
                    referenceId = command.ReferenceId,
                    reason = command.Reason,
                    correlationId = command.CorrelationId
                }));

                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.ROLLBACK,
                    command.RollbackId,
                    metadata,
                    postings.Select(p => (p.AccountId, p.Direction, p.Amount.MinorUnits)).ToList(),
                    balances,
                    cancellationToken);

                await LedgerWriter.RecordIdempotencyKeyAsync(source, command.RollbackId, txId, cancellationToken);

                var updatedBalance = balances[playerMainAccount.AccountId];
                await AddBalanceChangedEventAsync(
                    command.PlayerId, command.Currency, command.Network,
                    updatedBalance.BalanceMinor, updatedBalance.CashableMinor, updatedBalance.ReservedMinor,
                    command.CorrelationId, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Rollback processed: PlayerId={PlayerId}, ReferenceType={ReferenceType}, ReferenceId={ReferenceId}",
                    command.PlayerId, command.ReferenceType, command.ReferenceId);

                return new OperationResult(true, "Rollback processed successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rollback: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

