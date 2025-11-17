namespace Magenta.Wallet.Domain.Services;

/// <summary>
/// Resolves currency_network_id from currency code and network name.
/// Implementation will be in Infrastructure layer.
/// </summary>
public interface ICurrencyNetworkResolver
{
    Task<int?> ResolveCurrencyNetworkIdAsync(string currency, string network, CancellationToken cancellationToken = default);
}

