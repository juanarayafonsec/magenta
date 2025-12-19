using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class WithdrawalRequestRepository : IWithdrawalRequestRepository
{
    private readonly PaymentDbContext _context;

    public WithdrawalRequestRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WithdrawalRequest?> GetByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests
            .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId, cancellationToken);
    }

    public async Task<WithdrawalRequest> CreateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        _context.WithdrawalRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateAsync(WithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _context.WithdrawalRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<WithdrawalRequest>> GetProcessingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests
            .Where(w => w.Status == "PROCESSING")
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WithdrawalRequest>> GetBroadcastedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests
            .Where(w => w.Status == "BROADCASTED")
            .ToListAsync(cancellationToken);
    }
}
