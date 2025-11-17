using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payments.Infrastructure.Repositories;

public class WithdrawalRequestRepository : IWithdrawalRequestRepository
{
    private readonly PaymentsDbContext _context;

    public WithdrawalRequestRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<WithdrawalRequest?> GetByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests.FindAsync(new object[] { withdrawalId }, cancellationToken);
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

    public async Task<List<WithdrawalRequest>> GetPendingWithdrawalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WithdrawalRequests
            .Where(w => w.Status == Domain.Enums.WithdrawalRequestStatus.BROADCASTED ||
                       w.Status == Domain.Enums.WithdrawalRequestStatus.PROCESSING)
            .ToListAsync(cancellationToken);
    }
}

