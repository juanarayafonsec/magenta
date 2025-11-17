namespace Magenta.Wallet.Application.Interfaces;

public interface IAccountReadModel
{
    Task<List<CurrencyNetworkDto>> GetActiveCurrencyNetworksAsync(CancellationToken cancellationToken = default);
    Task<List<PlayerBalanceDto>> GetPlayerBalancesAsync(long playerId, CancellationToken cancellationToken = default);
}

public record CurrencyNetworkDto(
    int CurrencyNetworkId,
    string CurrencyCode,
    string NetworkName,
    int Decimals,
    string? IconUrl,
    int SortOrder
);

public record PlayerBalanceDto(
    string CurrencyCode,
    string NetworkName,
    long BalanceMinor,
    long ReservedMinor,
    long CashableMinor
);

