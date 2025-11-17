using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class FinalizeWithdrawalHandler : BaseHandler
{
    private readonly ILogger<FinalizeWithdrawalHandler> _logger;

    public FinalizeWithdrawalHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<FinalizeWithdrawalHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(FinalizeWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");

            var source = "payments.withdrawal.settled";
            if (await IdempotencyService.IsDuplicateAsync(source, command.RequestId, cancellationToken))
            {
                _logger.LogWarning("Duplicate withdrawal finalization: {RequestId}", command.RequestId);
                return new OperationResult(true, "Already processed");
            }

            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);
            try
            {
                var playerWithdrawHoldAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.WITHDRAW_HOLD, cancellationToken);
                var houseAccount = await LedgerWriter.EnsureAccountAsync(0, currencyNetworkId.Value, AccountType.HOUSE, cancellationToken);
                var houseFeesAccount = await LedgerWriter.EnsureAccountAsync(0, currencyNetworkId.Value, AccountType.HOUSE_FEES, cancellationToken);

                var amount = new Money(command.AmountMinor);
                var fee = new Money(command.FeeMinor);
                var postings = PostingRules.WithdrawalSettled(
                    playerWithdrawHoldAccount.AccountId,
                    houseAccount.AccountId,
                    houseFeesAccount.AccountId,
                    amount,
                    fee);

                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    source = "payments.withdrawal.settled",
                    requestId = command.RequestId,
                    txHash = command.TxHash,
                    feeMinor = command.FeeMinor,
                    network = command.Network,
                    correlationId = command.CorrelationId
                }));

                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.WITHDRAW_FINALIZE,
                    command.RequestId,
                    metadata,
                    postings.Select(p => (p.AccountId, p.Direction, p.Amount.MinorUnits)).ToList(),
                    balances,
                    cancellationToken);

                await LedgerWriter.RecordIdempotencyKeyAsync(source, command.RequestId, txId, cancellationToken);

                // Get player main account balance for event
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);
                var playerMainBalance = await LedgerWriter.EnsureAccountBalanceAsync(playerMainAccount.AccountId, cancellationToken);
                
                await AddBalanceChangedEventAsync(
                    command.PlayerId, command.Currency, command.Network,
                    playerMainBalance.BalanceMinor, playerMainBalance.CashableMinor, playerMainBalance.ReservedMinor,
                    command.CorrelationId, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Withdrawal finalized: PlayerId={PlayerId}, Amount={Amount}, Fee={Fee}, RequestId={RequestId}",
                    command.PlayerId, command.AmountMinor, command.FeeMinor, command.RequestId);

                return new OperationResult(true, "Withdrawal finalized successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing withdrawal: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

