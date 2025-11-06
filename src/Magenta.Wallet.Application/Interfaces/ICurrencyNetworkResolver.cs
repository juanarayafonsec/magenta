namespace Magenta.Wallet.Application.Interfaces;

/// <summary>
/// Application layer interface for resolving currency_network_id.
/// </summary>
public interface ICurrencyNetworkResolver
{
    Task<int> ResolveCurrencyNetworkIdAsync(string currencyCode, string networkName, CancellationToken cancellationToken = default);
}




