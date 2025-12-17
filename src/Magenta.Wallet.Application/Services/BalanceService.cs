using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Interfaces;

namespace Magenta.Wallet.Application.Services;

public class BalanceService : IBalanceService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceRepository _balanceRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ILedgerRepository _ledgerRepository;

    public BalanceService(
        IAccountRepository accountRepository,
        IBalanceRepository balanceRepository,
        ICurrencyRepository currencyRepository,
        ILedgerRepository ledgerRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _balanceRepository = balanceRepository ?? throw new ArgumentNullException(nameof(balanceRepository));
        _currencyRepository = currencyRepository ?? throw new ArgumentNullException(nameof(currencyRepository));
        _ledgerRepository = ledgerRepository ?? throw new ArgumentNullException(nameof(ledgerRepository));
    }

    public async Task<BalanceResponse> GetPlayerBalancesAsync(long playerId, CancellationToken cancellationToken = default)
    {
        // Get all currency networks first
        var allNetworks = await _currencyRepository.GetAllCurrencyNetworksAsync(cancellationToken);
        var response = new BalanceResponse();

        // For each currency network, get the MAIN account balance
        foreach (var network in allNetworks)
        {
            var mainAccount = await _accountRepository.GetAccountAsync(playerId, network.CurrencyNetworkId, "MAIN", cancellationToken);
            
            if (mainAccount != null)
            {
                // Calculate balance from ledger (source of truth)
                var balanceMinor = await _ledgerRepository.CalculateAccountBalanceAsync(mainAccount.AccountId, cancellationToken);
                
                // Update derived balance for performance
                await _balanceRepository.UpdateDerivedBalanceAsync(mainAccount.AccountId, balanceMinor, cancellationToken);
                
                response.Balances.Add(new CurrencyBalanceDto
                {
                    CurrencyNetworkId = network.CurrencyNetworkId,
                    Currency = network.Currency,
                    Network = network.Network,
                    BalanceMinor = balanceMinor,
                    Decimals = network.Decimals
                });
            }
        }

        return response;
    }
}
