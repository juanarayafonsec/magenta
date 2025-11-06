namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Domain service interface for resolving currency_network_id from currency code and network name.
/// Implementation is in Infrastructure layer.
/// </summary>
public interface ICurrencyNetworkResolver
{
    /// <summary>
    /// Resolves currency_network_id from currency code and network name.
    /// </summary>
    Task<int> ResolveCurrencyNetworkIdAsync(string currencyCode, string networkName, CancellationToken cancellationToken = default);
}




