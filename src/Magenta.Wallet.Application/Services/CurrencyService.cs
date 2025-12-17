using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Interfaces;

namespace Magenta.Wallet.Application.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _currencyRepository;

    public CurrencyService(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository ?? throw new ArgumentNullException(nameof(currencyRepository));
    }

    public async Task<List<CurrencyNetworkDto>> GetAllCurrencyNetworksAsync(CancellationToken cancellationToken = default)
    {
        var networks = await _currencyRepository.GetAllCurrencyNetworksAsync(cancellationToken);
        return networks.Select(n => new CurrencyNetworkDto
        {
            CurrencyNetworkId = n.CurrencyNetworkId,
            Currency = n.Currency,
            Network = n.Network,
            Decimals = n.Decimals
        }).ToList();
    }
}
