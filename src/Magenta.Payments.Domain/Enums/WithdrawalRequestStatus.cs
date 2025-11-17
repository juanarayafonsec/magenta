namespace Magenta.Payments.Domain.Enums;

public enum WithdrawalRequestStatus
{
    REQUESTED,
    PROCESSING,
    BROADCASTED,
    SETTLED,
    FAILED
}

