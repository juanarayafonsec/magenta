namespace Magenta.Payment.Domain.Enums;

public enum WithdrawalRequestStatus
{
    REQUESTED,
    PROCESSING,
    BROADCASTED,
    SETTLED,
    FAILED
}
