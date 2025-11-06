using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class CurrencyNetworkResolver : ICurrencyNetworkResolver
{
    private readonly WalletDbContext _context;

    public CurrencyNetworkResolver(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<int> ResolveCurrencyNetworkIdAsync(
        string currencyCode, 
        string networkName, 
        CancellationToken cancellationToken = default)
    {
        var currencyNetwork = await _context.CurrencyNetworks
            .Include(cn => cn.Currency)
            .Include(cn => cn.Network)
            .Where(cn => cn.Currency.Code == currencyCode && 
                        cn.Network.Name == networkName &&
                        cn.IsActive &&
                        cn.Currency.IsActive &&
                        cn.Network.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (currencyNetwork == null)
            throw new InvalidOperationException(
                $"Currency network not found: {currencyCode} on {networkName}");

        return currencyNetwork.CurrencyNetworkId;
    }
}




