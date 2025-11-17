namespace Magenta.Payments.Application.DTOs;

public record CreateDepositSessionResponse(
    Guid SessionId,
    string Address,
    string? QrUri,
    DateTime ExpiresAt,
    int ConfirmationsRequired
);

public record RequestWithdrawalResponse(
    Guid WithdrawalId,
    string Status
);

public record OperationResult(
    bool Success,
    string Message
);

