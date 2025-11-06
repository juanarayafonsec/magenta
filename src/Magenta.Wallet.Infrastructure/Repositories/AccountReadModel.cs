using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Magenta.Wallet.Infrastructure.Repositories;

public class AccountReadModel : IAccountReadModel
{
    private readonly WalletDbContext _context;

    public AccountReadModel(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlayerCurrencyBalance>> GetPlayerBalancesAsync(
        long playerId,
        CancellationToken cancellationToken = default)
    {
        // Query the view using raw SQL
        var sql = @"
            SELECT 
                currency_code AS CurrencyCode,
                network AS Network,
                player_id AS PlayerId,
                balance_minor AS BalanceMinor,
                cashable_minor AS CashableMinor,
                reserved_minor AS ReservedMinor
            FROM v_player_currency_balances
            WHERE player_id = @p0";

        // Use Set<PlayerCurrencyBalanceDto> with FromSqlRaw, or use ExecuteSqlRaw with manual mapping
        // For simplicity, we'll use a direct SQL approach
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var param = command.CreateParameter();
            param.ParameterName = "@p0";
            param.Value = playerId;
            command.Parameters.Add(param);

            var results = new List<PlayerCurrencyBalance>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new PlayerCurrencyBalance
                {
                    CurrencyCode = reader.GetString(0),
                    Network = reader.GetString(1),
                    BalanceMinor = reader.GetInt64(3),
                    ReservedMinor = reader.GetInt64(5),
                    CashableMinor = reader.GetInt64(4)
                });
            }
            
            return results;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<CurrencyNetworkDto>> GetActiveCurrencyNetworksAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.CurrencyNetworks
            .Where(cn => cn.IsActive && cn.Currency.IsActive && cn.Network.IsActive)
            .Select(cn => new CurrencyNetworkDto
            {
                CurrencyNetworkId = cn.CurrencyNetworkId,
                CurrencyCode = cn.Currency.Code,
                CurrencyDisplayName = cn.Currency.DisplayName,
                NetworkName = cn.Network.Name,
                Decimals = cn.Currency.Decimals,
                IconUrl = cn.Currency.IconUrl
            })
            .ToListAsync(cancellationToken);
    }

    private class PlayerCurrencyBalanceDto
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public long PlayerId { get; set; }
        public long BalanceMinor { get; set; }
        public long CashableMinor { get; set; }
        public long ReservedMinor { get; set; }
    }
}

