namespace Magenta.Wallet.Application.DTOs;

public record OperationResult(
    bool Ok,
    string Message
);

public record BalanceItem(
    string Currency,
    string Network,
    long BalanceMinor,
    long ReservedMinor,
    long CashableMinor
);

public record GetBalanceResponse(
    List<BalanceItem> Items
);

