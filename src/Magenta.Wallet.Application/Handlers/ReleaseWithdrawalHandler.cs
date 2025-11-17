using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class ReleaseWithdrawalHandler : BaseHandler
{
    private readonly ILogger<ReleaseWithdrawalHandler> _logger;

    public ReleaseWithdrawalHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<ReleaseWithdrawalHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(ReleaseWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");

            var source = "payments.withdrawal.failed";
            if (await IdempotencyService.IsDuplicateAsync(source, command.RequestId, cancellationToken))
            {
                _logger.LogWarning("Duplicate withdrawal release: {RequestId}", command.RequestId);
                return new OperationResult(true, "Already processed");
            }

            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);
            try
            {
                var playerWithdrawHoldAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.WITHDRAW_HOLD, cancellationToken);
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);

                var amount = new Money(command.AmountMinor);
                var postings = PostingRules.WithdrawalFailed(playerWithdrawHoldAccount.AccountId, playerMainAccount.AccountId, amount);

                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    source = "payments.withdrawal.failed",
                    requestId = command.RequestId,
                    network = command.Network,
                    correlationId = command.CorrelationId
                }));

                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.WITHDRAW_RELEASE,
                    command.RequestId,
                    metadata,
                    postings.Select(p => (p.AccountId, p.Direction, p.Amount.MinorUnits)).ToList(),
                    balances,
                    cancellationToken);

                await LedgerWriter.RecordIdempotencyKeyAsync(source, command.RequestId, txId, cancellationToken);

                var updatedBalance = balances[playerMainAccount.AccountId];
                await AddBalanceChangedEventAsync(
                    command.PlayerId, command.Currency, command.Network,
                    updatedBalance.BalanceMinor, updatedBalance.CashableMinor, updatedBalance.ReservedMinor,
                    command.CorrelationId, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Withdrawal released: PlayerId={PlayerId}, Amount={Amount}, RequestId={RequestId}",
                    command.PlayerId, command.AmountMinor, command.RequestId);

                return new OperationResult(true, "Withdrawal released successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing withdrawal: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

