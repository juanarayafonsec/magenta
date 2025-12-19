using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Domain.Entities;
using Magenta.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Payment.Infrastructure.Repositories;

public class DepositSessionRepository : IDepositSessionRepository
{
    private readonly PaymentDbContext _context;

    public DepositSessionRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DepositSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.DepositSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
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

    public async Task<List<DepositSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DepositSessions
            .Where(s => s.Status == "OPEN" && s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
