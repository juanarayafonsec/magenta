using Magenta.Payments.Application.Interfaces;
using Magenta.Payments.Domain.Entities;
using Magenta.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payments.Infrastructure.Repositories;

public class DepositSessionRepository : IDepositSessionRepository
{
    private readonly PaymentsDbContext _context;

    public DepositSessionRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<DepositSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.DepositSessions.FindAsync(new object[] { sessionId }, cancellationToken);
    }

    public async Task<DepositSession> CreateAsync(DepositSession session, CancellationToken cancellationToken = default)
    {
        _context.DepositSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task UpdateAsync(DepositSession session, CancellationToken cancellationToken = default)
    {
        session.UpdatedAt = DateTime.UtcNow;
        _context.DepositSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

