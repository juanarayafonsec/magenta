using Magenta.Wallet.Domain.Entities;
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
        var tron = new Network
        {
            Name = "TRON",
            NativeSymbol = "TRX",
            ConfirmationsRequired = 12,
            ExplorerUrl = "https://tronscan.org",
            IsActive = true
        };

        var ethereum = new Network
        {
            Name = "ETHEREUM",
            NativeSymbol = "ETH",
            ConfirmationsRequired = 12,
            ExplorerUrl = "https://etherscan.io",
            IsActive = true
        };

        context.Networks.AddRange(tron, ethereum);
        await context.SaveChangesAsync();

        // Currencies
        var usdt = new Currency
        {
            Code = "USDT",
            DisplayName = "Tether USD",
            Decimals = 6,
            SortOrder = 1,
            IsActive = true
        };

        var btc = new Currency
        {
            Code = "BTC",
            DisplayName = "Bitcoin",
            Decimals = 8,
            SortOrder = 2,
            IsActive = true
        };

        context.Currencies.AddRange(usdt, btc);
        await context.SaveChangesAsync();

        // Currency Networks (USDT on TRON)
        var usdtTron = new CurrencyNetwork
        {
            CurrencyId = usdt.CurrencyId,
            NetworkId = tron.NetworkId,
            TokenContract = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t", // USDT on TRON
            WithdrawalFeeMinor = 1_000_000, // 1 USDT
            MinDepositMinor = 10_000_000, // 10 USDT
            MinWithdrawMinor = 10_000_000, // 10 USDT
            IsActive = true
        };

        context.CurrencyNetworks.Add(usdtTron);
        await context.SaveChangesAsync();

        // Create House accounts for USDT-TRON
        var houseAccount = new Account
        {
            PlayerId = 0, // House account
            CurrencyNetworkId = usdtTron.CurrencyNetworkId,
            AccountType = AccountType.HOUSE,
            Status = "ACTIVE"
        };

        var houseWagerAccount = new Account
        {
            PlayerId = 0,
            CurrencyNetworkId = usdtTron.CurrencyNetworkId,
            AccountType = AccountType.HOUSE_WAGER,
            Status = "ACTIVE"
        };

        var houseFeesAccount = new Account
        {
            PlayerId = 0,
            CurrencyNetworkId = usdtTron.CurrencyNetworkId,
            AccountType = AccountType.HOUSE_FEES,
            Status = "ACTIVE"
        };

        context.Accounts.AddRange(houseAccount, houseWagerAccount, houseFeesAccount);
        await context.SaveChangesAsync();

        // Create initial balances for House accounts (all zero)
        var houseBalance = new AccountBalance
        {
            AccountId = houseAccount.AccountId,
            BalanceMinor = 0,
            ReservedMinor = 0,
            CashableMinor = 0,
            UpdatedAt = DateTime.UtcNow
        };

        var houseWagerBalance = new AccountBalance
        {
            AccountId = houseWagerAccount.AccountId,
            BalanceMinor = 0,
            ReservedMinor = 0,
            CashableMinor = 0,
            UpdatedAt = DateTime.UtcNow
        };

        var houseFeesBalance = new AccountBalance
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




