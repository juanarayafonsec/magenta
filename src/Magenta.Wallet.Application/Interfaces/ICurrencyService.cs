using Magenta.Wallet.Application.DTOs;

namespace Magenta.Wallet.Application.Interfaces;

public interface ICurrencyService
{
    Task<List<CurrencyNetworkDto>> GetAllCurrencyNetworksAsync(CancellationToken cancellationToken = default);
}
