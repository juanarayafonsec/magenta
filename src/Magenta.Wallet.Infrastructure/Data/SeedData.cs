using Magenta.Wallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Magenta.Wallet.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(WalletDbContext context)
    {
        if (await context.Networks.AnyAsync())
            return; // Already seeded

        // Networks
        var tronNetwork = new Network
        {
            Name = "TRON",
            NativeSymbol = "TRX",
            ConfirmationsRequired = 20,
            ExplorerUrl = "https://tronscan.org",
            IsActive = true
        };

        var ethereumNetwork = new Network
        {
            Name = "ETHEREUM",
            NativeSymbol = "ETH",
            ConfirmationsRequired = 12,
            ExplorerUrl = "https://etherscan.io",
            IsActive = true
        };

        context.Networks.AddRange(tronNetwork, ethereumNetwork);
        await context.SaveChangesAsync();

        // Currencies
        var usdtCurrency = new Currency
        {
            Code = "USDT",
            DisplayName = "Tether USD",
            Decimals = 6,
            SortOrder = 1,
            IsActive = true
        };

        var btcCurrency = new Currency
        {
            Code = "BTC",
            DisplayName = "Bitcoin",
            Decimals = 8,
            SortOrder = 2,
            IsActive = true
        };

        context.Currencies.AddRange(usdtCurrency, btcCurrency);
        await context.SaveChangesAsync();

        // Currency Networks
        var usdtTron = new CurrencyNetwork
        {
            CurrencyId = usdtCurrency.CurrencyId,
            NetworkId = tronNetwork.NetworkId,
            TokenContract = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t",
            WithdrawalFeeMinor = 1_000_000, // 1 USDT
            MinDepositMinor = 10_000_000, // 10 USDT
            MinWithdrawMinor = 20_000_000, // 20 USDT
            IsActive = true
        };

        context.CurrencyNetworks.Add(usdtTron);
        await context.SaveChangesAsync();

        // House accounts for USDT-TRON
        var houseAccount = new Domain.Entities.Account
        {
            PlayerId = 0, // System player
            CurrencyNetworkId = usdtTron.CurrencyNetworkId,
            AccountType = AccountType.HOUSE,
            Status = "ACTIVE"
        };

        var houseWagerAccount = new Domain.Entities.Account
        {
            PlayerId = 0,
            CurrencyNetworkId = usdtTron.CurrencyNetworkId,
            AccountType = AccountType.HOUSE_WAGER,
            Status = "ACTIVE"
        };

        var houseFeesAccount = new Domain.Entities.Account
        {
            PlayerId = 0,
            CurrencyNetworkId = usdtTron.CurrencyNetworkId,
            AccountType = AccountType.HOUSE_FEES,
            Status = "ACTIVE"
        };

        context.Accounts.AddRange(houseAccount, houseWagerAccount, houseFeesAccount);
        await context.SaveChangesAsync();

        // Initialize account balances for house accounts
        var houseBalance = new Domain.Entities.AccountBalance
        {
            AccountId = houseAccount.AccountId,
            BalanceMinor = 0,
            ReservedMinor = 0,
            CashableMinor = 0,
            UpdatedAt = DateTime.UtcNow
        };

        var houseWagerBalance = new Domain.Entities.AccountBalance
        {
            AccountId = houseWagerAccount.AccountId,
            BalanceMinor = 0,
            ReservedMinor = 0,
            CashableMinor = 0,
            UpdatedAt = DateTime.UtcNow
        };

        var houseFeesBalance = new Domain.Entities.AccountBalance
        {
            AccountId = houseFeesAccount.AccountId,
            BalanceMinor = 0,
            ReservedMinor = 0,
            CashableMinor = 0,
            UpdatedAt = DateTime.UtcNow
        };

        context.AccountBalances.AddRange(houseBalance, houseWagerBalance, houseFeesBalance);
        await context.SaveChangesAsync();
    }
}

