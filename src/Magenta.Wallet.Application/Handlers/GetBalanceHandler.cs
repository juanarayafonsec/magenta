using Magenta.Wallet.Application.DTOs.Queries;
using Magenta.Wallet.Application.Interfaces;

namespace Magenta.Wallet.Application.Handlers;

public class GetBalanceHandler
{
    private readonly IAccountReadModel _accountReadModel;

    public GetBalanceHandler(IAccountReadModel accountReadModel)
    {
        _accountReadModel = accountReadModel;
    }

    public async Task<GetBalanceResponse> HandleAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        var balances = await _accountReadModel.GetPlayerBalancesAsync(query.PlayerId, cancellationToken);
        
        return new GetBalanceResponse
        {
            Items = balances.Select(b => new BalanceItem
            {
                Currency = b.CurrencyCode,
                Network = b.Network,
                BalanceMinor = b.BalanceMinor,
                ReservedMinor = b.ReservedMinor,
                CashableMinor = b.CashableMinor
            }).ToList()
        };
    }
}




