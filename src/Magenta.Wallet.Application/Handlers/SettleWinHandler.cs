using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Domain.Enums;
using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Domain.ValueObjects;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Magenta.Wallet.Application.Handlers;

public class SettleWinHandler : BaseHandler
{
    private readonly ILogger<SettleWinHandler> _logger;

    public SettleWinHandler(
        ILedgerWriter ledgerWriter,
        ICurrencyNetworkResolver currencyNetworkResolver,
        IIdempotencyService idempotencyService,
        WalletDbContext dbContext,
        ILogger<SettleWinHandler> logger)
        : base(ledgerWriter, currencyNetworkResolver, idempotencyService, dbContext)
    {
        _logger = logger;
    }

    public async Task<OperationResult> HandleAsync(SettleWinCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var currencyNetworkId = await CurrencyNetworkResolver.ResolveCurrencyNetworkIdAsync(
                command.Currency, command.Network, cancellationToken);
            
            if (currencyNetworkId == null)
                return new OperationResult(false, $"Currency network not found: {command.Currency}-{command.Network}");

            var source = "game.win";
            if (await IdempotencyService.IsDuplicateAsync(source, command.WinId, cancellationToken))
            {
                _logger.LogWarning("Duplicate win: {WinId}", command.WinId);
                return new OperationResult(true, "Already processed");
            }

            using var transaction = await LedgerWriter.BeginTransactionAsync(cancellationToken);
            try
            {
                var houseWagerAccount = await LedgerWriter.EnsureAccountAsync(0, currencyNetworkId.Value, AccountType.HOUSE_WAGER, cancellationToken);
                var playerMainAccount = await LedgerWriter.EnsureAccountAsync(command.PlayerId, currencyNetworkId.Value, AccountType.MAIN, cancellationToken);

                var amount = new Money(command.AmountMinor);
                var postings = PostingRules.Win(houseWagerAccount.AccountId, playerMainAccount.AccountId, amount);

                var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    provider = command.Provider,
                    winId = command.WinId,
                    roundId = command.RoundId,
                    betId = command.BetId,
                    correlationId = command.CorrelationId
                }));

                var accountIds = postings.Select(p => p.AccountId).Distinct().ToList();
                var balances = await GetOrCreateBalancesAsync(accountIds, cancellationToken);
                await UpdateDerivedBalancesAsync(balances, postings, cancellationToken);

                var txId = await LedgerWriter.WriteTransactionAsync(
                    TxType.WIN,
                    command.WinId,
                    metadata,
                    postings.Select(p => (p.AccountId, p.Direction, p.Amount.MinorUnits)).ToList(),
                    balances,
                    cancellationToken);

                await LedgerWriter.RecordIdempotencyKeyAsync(source, command.WinId, txId, cancellationToken);

                var updatedBalance = balances[playerMainAccount.AccountId];
                await AddBalanceChangedEventAsync(
                    command.PlayerId, command.Currency, command.Network,
                    updatedBalance.BalanceMinor, updatedBalance.CashableMinor, updatedBalance.ReservedMinor,
                    command.CorrelationId, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Win settled: PlayerId={PlayerId}, Amount={Amount}, WinId={WinId}",
                    command.PlayerId, command.AmountMinor, command.WinId);

                return new OperationResult(true, "Win settled successfully");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error settling win: {Error}", ex.Message);
            return new OperationResult(false, $"Error: {ex.Message}");
        }
    }
}

