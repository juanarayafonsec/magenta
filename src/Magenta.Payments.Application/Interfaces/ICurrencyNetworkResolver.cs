namespace Magenta.Payments.Application.Interfaces;

public interface ICurrencyNetworkResolver
{
    Task<CurrencyNetworkInfo?> ResolveCurrencyNetworkAsync(
        string currency,
        string network,
        CancellationToken cancellationToken = default);
}

public record CurrencyNetworkInfo(
    int CurrencyNetworkId,
    string CurrencyCode,
    string NetworkName,
    int Decimals
);

