namespace Magenta.Wallet.Application.DTOs;

public record ApplyDepositSettlementCommand(
    long PlayerId,
    string Currency,
    string Network,
    long AmountMinor,
    string TxHash,
    string IdempotencyKey,
    string? CorrelationId = null
);

public record ReserveWithdrawalCommand(
    long PlayerId,
    string Currency,
    string Network,
    long AmountMinor,
    string RequestId, // idempotency key
    string? CorrelationId = null
);

public record FinalizeWithdrawalCommand(
    long PlayerId,
    string Currency,
    string Network,
    long AmountMinor,
    long FeeMinor,
    string RequestId, // idempotency key
    string TxHash,
    string? CorrelationId = null
);

public record ReleaseWithdrawalCommand(
    long PlayerId,
    string Currency,
    string Network,
    long AmountMinor,
    string RequestId, // idempotency key
    string? CorrelationId = null
);

public record PlaceBetCommand(
    long PlayerId,
    string Currency,
    string Network,
    long AmountMinor,
    string BetId, // idempotency key
    string Provider,
    string RoundId,
    string? GameCode = null,
    string? CorrelationId = null
);

public record SettleWinCommand(
    long PlayerId,
    string Currency,
    string Network,
    long AmountMinor,
    string WinId, // idempotency key
    string BetId,
    string RoundId,
    string Provider,
    string? CorrelationId = null
);

public record RollbackCommand(
    long PlayerId,
    string Currency,
    string Network,
    string ReferenceType, // "BET" | "WIN"
    string ReferenceId, // betId or winId
    string RollbackId, // idempotency key
    string Reason,
    string? CorrelationId = null
);

public record GetBalanceQuery(
    long PlayerId
);

