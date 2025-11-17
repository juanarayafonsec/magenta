namespace Magenta.Payments.Domain.ValueObjects;

public record CurrencyNetwork(
    int CurrencyNetworkId,
    string CurrencyCode,
    string NetworkName,
    int Decimals
);

