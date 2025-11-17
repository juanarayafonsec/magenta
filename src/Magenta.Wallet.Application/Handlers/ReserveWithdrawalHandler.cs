using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class ReserveWithdrawalHandler : BaseHandler
{
    private readonly ILogger<ReserveWithdrawalHandler> _logger;

    public ReserveWithdrawalHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<ReserveWithdrawalHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(ReserveWithdrawalCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");

            var source = "wallet.withdrawal.reserve";
            if (await IdempotencyService.IsDuplicateAsync(source, command.RequestId, cancellationToken))
            {
                _logger.LogWarning("Duplicate withdrawal reservation: {RequestId}", command.RequestId);
                return new OperationResult(true, "Already processed");
            }

            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);
            try
            {
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);
                var playerWithdrawHoldAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.WITHDRAW_HOLD, cancellationToken);

                var amount = new Money(command.AmountMinor);
                var postings = PostingRules.WithdrawalReserved(playerMainAccount.AccountId, playerWithdrawHoldAccount.AccountId, amount);

                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    source = "wallet.withdrawal.reserve",
                    requestId = command.RequestId,
                    network = command.Network,
                    correlationId = command.CorrelationId
                }));

                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.WITHDRAW_RESERVE,
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

                // Publish withdrawal reserved event
                var reservedPayload = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    eventId = Guid.NewGuid().ToString(),
                    occurredAt = DateTime.UtcNow,
                    requestId = command.RequestId,
                    playerId = command.PlayerId,
                    currency = command.Currency,
                    network = command.Network,
                    amountMinor = command.AmountMinor,
                    correlationId = command.CorrelationId
                }));
                await LedgerWriter.AddOutboxEventAsync("WithdrawalReserved", "wallet.withdrawal.reserved", reservedPayload, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Withdrawal reserved: PlayerId={PlayerId}, Amount={Amount}, RequestId={RequestId}",
                    command.PlayerId, command.AmountMinor, command.RequestId);

                return new OperationResult(true, "Withdrawal reserved successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving withdrawal: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

