using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly WalletDbContext _context;

    public CurrencyRepository(WalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CurrencyNetwork?> GetCurrencyNetworkAsync(string currency, string network, CancellationToken cancellationToken = default)
    {
        return await _context.CurrencyNetworks
            .FirstOrDefaultAsync(cn => cn.Currency == currency && cn.Network == network, cancellationToken);
    }

    public async Task<CurrencyNetwork?> GetCurrencyNetworkByIdAsync(int currencyNetworkId, CancellationToken cancellationToken = default)
    {
        return await _context.CurrencyNetworks
            .FirstOrDefaultAsync(cn => cn.CurrencyNetworkId == currencyNetworkId, cancellationToken);
    }

    public async Task<List<CurrencyNetwork>> GetAllCurrencyNetworksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CurrencyNetworks
            .OrderBy(cn => cn.Currency)
            .ThenBy(cn => cn.Network)
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> GetDecimalsAsync(int currencyNetworkId, CancellationToken cancellationToken = default)
    {
        var network = await GetCurrencyNetworkByIdAsync(currencyNetworkId, cancellationToken);
        return network?.Decimals;
    }
}
