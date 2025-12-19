using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class DepositRequestRepository : IDepositRequestRepository
{
    private readonly PaymentDbContext _context;

    public DepositRequestRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DepositRequest?> GetByIdAsync(Guid depositId, CancellationToken cancellationToken = default)
    {
        return await _context.DepositRequests
            .FirstOrDefaultAsync(d => d.DepositId == depositId, cancellationToken);
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

    public async Task<List<DepositRequest>> GetPendingVerificationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DepositRequests
            .Where(d => d.Status == "PENDING")
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DepositRequest>> GetConfirmedPendingSettlementAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DepositRequests
            .Where(d => d.Status == "CONFIRMED" && d.ConfirmationsReceived >= d.ConfirmationsRequired)
            .ToListAsync(cancellationToken);
    }
}
