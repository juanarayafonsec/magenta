using Magenta.Payments.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Magenta.Payments.Infrastructure.Services;

/// <summary>
/// Resolves currency network information.
/// In production, this would query the Wallet database or a shared service.
/// </summary>
public class CurrencyNetworkResolver : ICurrencyNetworkResolver
{
    private readonly ILogger<CurrencyNetworkResolver> _logger;
    private readonly Dictionary<string, CurrencyNetworkInfo> _cache;

    public CurrencyNetworkResolver(ILogger<CurrencyNetworkResolver> logger)
    {
        _logger = logger;
        // Mock data - in production, fetch from Wallet DB or shared service
        _cache = new Dictionary<string, CurrencyNetworkInfo>
        {
            { "USDT-TRON", new CurrencyNetworkInfo(1, "USDT", "TRON", 6) },
            { "BTC-BITCOIN", new CurrencyNetworkInfo(2, "BTC", "BITCOIN", 8) },
            { "ETH-ETHEREUM", new CurrencyNetworkInfo(3, "ETH", "ETHEREUM", 18) }
        };
    }

    public Task<CurrencyNetworkInfo?> ResolveCurrencyNetworkAsync(
        string currency,
        string network,
        CancellationToken cancellationToken = default)
    {
        var key = $"{currency}-{network}";
        if (_cache.TryGetValue(key, out var info))
        {
            return Task.FromResult<CurrencyNetworkInfo?>(info);
        }

        _logger.LogWarning("Currency network not found: {Currency}-{Network}", currency, network);
        return Task.FromResult<CurrencyNetworkInfo?>(null);
    }
}

