using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface ICurrencyRepository
{
    Task<CurrencyNetwork?> GetCurrencyNetworkAsync(string currency, string network, CancellationToken cancellationToken = default);
    Task<CurrencyNetwork?> GetCurrencyNetworkByIdAsync(int currencyNetworkId, CancellationToken cancellationToken = default);
    Task<List<CurrencyNetwork>> GetAllCurrencyNetworksAsync(CancellationToken cancellationToken = default);
    Task<int?> GetDecimalsAsync(int currencyNetworkId, CancellationToken cancellationToken = default);
}
