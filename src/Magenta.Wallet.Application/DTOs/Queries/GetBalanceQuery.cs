namespace Magenta.Wallet.Application.DTOs.Queries;

public class GetBalanceQuery
{
    public long PlayerId { get; set; }
}

public class GetBalanceResponse
{
    public List<BalanceItem> Items { get; set; } = new();
}

public class BalanceItem
{
    public string Currency { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long BalanceMinor { get; set; }
    public long ReservedMinor { get; set; }
    public long CashableMinor { get; set; }
}




