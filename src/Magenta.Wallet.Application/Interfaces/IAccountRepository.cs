using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetAccountAsync(long? playerId, int currencyNetworkId, string accountType, CancellationToken cancellationToken = default);
    Task<Account> GetOrCreateAccountAsync(long? playerId, int currencyNetworkId, string accountType, CancellationToken cancellationToken = default);
    Task<Account> LockAccountForUpdateAsync(long accountId, CancellationToken cancellationToken = default);
    Task<List<Account>> GetPlayerAccountsAsync(long playerId, int currencyNetworkId, CancellationToken cancellationToken = default);
}
