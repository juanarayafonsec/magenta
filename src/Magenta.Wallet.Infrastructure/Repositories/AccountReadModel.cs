using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class AccountReadModel : IAccountReadModel
{
    private readonly WalletDbContext _context;

    public AccountReadModel(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<List<CurrencyNetworkDto>> GetActiveCurrencyNetworksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CurrencyNetworks
            .Include(cn => cn.Currency)
            .Include(cn => cn.Network)
            .Where(cn => cn.IsActive && cn.Currency.IsActive && cn.Network.IsActive)
            .Select(cn => new CurrencyNetworkDto(
                cn.CurrencyNetworkId,
                cn.Currency.Code,
                cn.Network.Name,
                cn.Currency.Decimals,
                cn.Currency.IconUrl,
                cn.Currency.SortOrder
            ))
            .OrderBy(cn => cn.SortOrder)
            .ThenBy(cn => cn.CurrencyCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PlayerBalanceDto>> GetPlayerBalancesAsync(long playerId, CancellationToken cancellationToken = default)
    {
        var balances = await _context.Database
            .SqlQueryRaw<PlayerBalanceView>($"SELECT currency_code AS CurrencyCode, network AS Network, balance_minor AS BalanceMinor, cashable_minor AS CashableMinor, reserved_minor AS ReservedMinor FROM v_player_currency_balances WHERE player_id = {playerId}")
            .ToListAsync(cancellationToken);

        return balances.Select(b => new PlayerBalanceDto(
            b.CurrencyCode,
            b.Network,
            b.BalanceMinor,
            b.ReservedMinor,
            b.CashableMinor
        )).ToList();
    }

    private class PlayerBalanceView
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public long BalanceMinor { get; set; }
        public long ReservedMinor { get; set; }
        public long CashableMinor { get; set; }
    }
}

