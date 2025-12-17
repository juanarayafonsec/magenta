using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface IBalanceRepository
{
    Task UpdateDerivedBalanceAsync(long accountId, long balanceMinor, CancellationToken cancellationToken = default);
    Task<long?> GetDerivedBalanceAsync(long accountId, CancellationToken cancellationToken = default);
    Task<Dictionary<long, long>> GetPlayerBalancesAsync(long playerId, CancellationToken cancellationToken = default);
}
