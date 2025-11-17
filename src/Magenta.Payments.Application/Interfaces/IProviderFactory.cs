using Magenta.Payments.Domain.Interfaces;

namespace Magenta.Payments.Application.Interfaces;

public interface IProviderFactory
{
    IPaymentProvider GetProvider(int providerId);
}

