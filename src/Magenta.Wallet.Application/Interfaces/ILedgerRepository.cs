using Magenta.Wallet.Domain.Entities;

namespace Magenta.Wallet.Application.Interfaces;

public interface ILedgerRepository
{
    Task<LedgerTransaction?> GetTransactionByReferenceAsync(string source, string referenceId, CancellationToken cancellationToken = default);
    Task<Guid> CreateTransactionAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<LedgerPosting>> GetAccountPostingsAsync(long accountId, CancellationToken cancellationToken = default);
    Task<long> CalculateAccountBalanceAsync(long accountId, CancellationToken cancellationToken = default);
}
