namespace Magenta.Wallet.Application.Interfaces;

/// <summary>
/// Read-only interface for querying account balances.
/// </summary>
public interface IAccountReadModel
{
    /// <summary>
    /// Gets all balances for a player from v_player_currency_balances view.
    /// </summary>
    Task<List<PlayerCurrencyBalance>> GetPlayerBalancesAsync(
        long playerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active currency-network pairs.
    /// </summary>
    Task<List<CurrencyNetworkDto>> GetActiveCurrencyNetworksAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for player currency balance from view.
/// </summary>
public class PlayerCurrencyBalance
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long BalanceMinor { get; set; }
    public long ReservedMinor { get; set; }
    public long CashableMinor { get; set; }
}

/// <summary>
/// DTO for currency-network pair.
/// </summary>
public class CurrencyNetworkDto
{
    public int CurrencyNetworkId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyDisplayName { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty;
    public int Decimals { get; set; }
    public string? IconUrl { get; set; }
}




