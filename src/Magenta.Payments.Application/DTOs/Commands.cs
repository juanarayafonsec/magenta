namespace Magenta.Payments.Application.DTOs;

public record CreateDepositSessionCommand(
    long PlayerId,
    string Currency,
    string Network,
    decimal? ExpectedAmountMajor,
    int? ExpiresInSeconds,
    string IdempotencyKey
);

public record RequestWithdrawalCommand(
    long PlayerId,
    string Currency,
    string Network,
    decimal AmountMajor,
    string TargetAddress,
    string IdempotencyKey
);

public record ProcessDepositWebhookCommand(
    int ProviderId,
    string EventType,
    Dictionary<string, object> Payload,
    string? Signature
);

public record VerifyDepositCommand(
    Guid DepositId
);

public record ProcessWithdrawalWebhookCommand(
    int ProviderId,
    string EventType,
    Dictionary<string, object> Payload,
    string? Signature
);

