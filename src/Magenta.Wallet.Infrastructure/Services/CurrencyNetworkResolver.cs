using Magenta.Wallet.Domain.Services;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Services;

public class CurrencyNetworkResolver : ICurrencyNetworkResolver
{
    private readonly WalletDbContext _context;

    public CurrencyNetworkResolver(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<int?> ResolveCurrencyNetworkIdAsync(string currency, string network, CancellationToken cancellationToken = default)
    {
        var currencyNetwork = await _context.CurrencyNetworks
            .Include(cn => cn.Currency)
            .Include(cn => cn.Network)
            .Where(cn => cn.Currency.Code == currency && 
                        cn.Network.Name == network && 
                        cn.IsActive && 
                        cn.Currency.IsActive && 
                        cn.Network.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return currencyNetwork?.CurrencyNetworkId;
    }
}

