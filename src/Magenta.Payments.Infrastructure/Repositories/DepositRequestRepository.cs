using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payments.Infrastructure.Repositories;

public class DepositRequestRepository : IDepositRequestRepository
{
    private readonly PaymentsDbContext _context;

    public DepositRequestRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<DepositRequest?> GetByIdAsync(Guid depositId, CancellationToken cancellationToken = default)
    {
        return await _context.DepositRequests.FindAsync(new object[] { depositId }, cancellationToken);
    }

    public async Task<DepositRequest?> GetByTxHashAsync(string txHash, CancellationToken cancellationToken = default)
    {
        return await _context.DepositRequests
            .FirstOrDefaultAsync(d => d.TxHash == txHash, cancellationToken);
    }

    public async Task<DepositRequest> CreateAsync(DepositRequest request, CancellationToken cancellationToken = default)
    {
        _context.DepositRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateAsync(DepositRequest request, CancellationToken cancellationToken = default)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _context.DepositRequests.Update(request);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DepositRequest>> GetPendingDepositsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DepositRequests
            .Where(d => d.Status == Domain.Enums.DepositRequestStatus.PENDING ||
                       d.Status == Domain.Enums.DepositRequestStatus.CONFIRMED)
            .ToListAsync(cancellationToken);
    }
}

