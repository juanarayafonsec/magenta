using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class PlaceBetHandler : BaseHandler
{
    private readonly ILogger<PlaceBetHandler> _logger;

    public PlaceBetHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<PlaceBetHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(PlaceBetCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");

            var source = "game.bet";
            if (await IdempotencyService.IsDuplicateAsync(source, command.BetId, cancellationToken))
            {
                _logger.LogWarning("Duplicate bet: {BetId}", command.BetId);
                return new OperationResult(true, "Already processed");
            }

            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);
            try
            {
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);
                var houseWagerAccount = await LedgerWriter.EnsureAccountAsync(0, currencyNetworkId.Value, AccountType.HOUSE_WAGER, cancellationToken);

                var amount = new Money(command.AmountMinor);
                var postings = PostingRules.Bet(playerMainAccount.AccountId, houseWagerAccount.AccountId, amount);

                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    provider = command.Provider,
                    betId = command.BetId,
                    roundId = command.RoundId,
                    gameCode = command.GameCode,
                    correlationId = command.CorrelationId
                }));

                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.BET,
                    command.BetId,
                    metadata,
                    postings.Select(p => (p.AccountId, p.Direction, p.Amount.MinorUnits)).ToList(),
                    balances,
                    cancellationToken);

                await LedgerWriter.RecordIdempotencyKeyAsync(source, command.BetId, txId, cancellationToken);

                var updatedBalance = balances[playerMainAccount.AccountId];
                await AddBalanceChangedEventAsync(
                    command.PlayerId, command.Currency, command.Network,
                    updatedBalance.BalanceMinor, updatedBalance.CashableMinor, updatedBalance.ReservedMinor,
                    command.CorrelationId, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Bet placed: PlayerId={PlayerId}, Amount={Amount}, BetId={BetId}",
                    command.PlayerId, command.AmountMinor, command.BetId);

                return new OperationResult(true, "Bet placed successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing bet: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

