using Magenta.Wallet.Application.DTOs;

namespace Magenta.Wallet.Application.Interfaces;

public interface IBalanceService
{
    Task<BalanceResponse> GetPlayerBalancesAsync(long playerId, CancellationToken cancellationToken = default);
}
