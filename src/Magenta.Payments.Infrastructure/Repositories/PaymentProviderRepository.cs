using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payments.Infrastructure.Repositories;

public class PaymentProviderRepository : IPaymentProviderRepository
{
    private readonly PaymentsDbContext _context;

    public PaymentProviderRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentProvider?> GetByIdAsync(int providerId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentProviders.FindAsync(new object[] { providerId }, cancellationToken);
    }

    public async Task<List<PaymentProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentProviders
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }
}

