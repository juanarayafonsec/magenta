using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Magenta.Payments.Infrastructure.Providers;

public class ProviderFactory : IProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<int, Type> _providerTypes;

    public ProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _providerTypes = new Dictionary<int, Type>
        {
            // Map provider IDs to implementation types
            // For now, all use MockProvider
            // In production, you'd map: 1 -> BitGoProvider, 2 -> FireblocksProvider, etc.
        };
    }

    public IPaymentProvider GetProvider(int providerId)
    {
        // For now, always return mock provider
        // In production, resolve based on providerId
        return _serviceProvider.GetRequiredService<MockPaymentProvider>();
    }
}

