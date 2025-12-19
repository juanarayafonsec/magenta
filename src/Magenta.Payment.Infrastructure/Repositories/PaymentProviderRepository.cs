using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class PaymentProviderRepository : IPaymentProviderRepository
{
    private readonly PaymentDbContext _context;

    public PaymentProviderRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PaymentProvider?> GetByIdAsync(int providerId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentProviders
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, cancellationToken);
    }

    public async Task<List<PaymentProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentProviders
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }
}
