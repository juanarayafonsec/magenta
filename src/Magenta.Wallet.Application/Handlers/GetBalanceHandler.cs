using Magenta.Wallet.Application.DTOs;
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
        
        var items = balances.Select(b => new BalanceItem(
            b.CurrencyCode,
            b.NetworkName,
            b.BalanceMinor,
            b.ReservedMinor,
            b.CashableMinor
        )).ToList();

        return new GetBalanceResponse(items);
    }
}

