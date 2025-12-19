using Magenta.Payment.Domain.Entities;

namespace Magenta.Payment.Application.Interfaces;

public interface IPaymentProviderRepository
{
    Task<PaymentProvider?> GetByIdAsync(int providerId, CancellationToken cancellationToken = default);
    Task<List<PaymentProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default);
}
