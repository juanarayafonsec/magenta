namespace Magenta.Wallet.Application.DTOs;

public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? TransactionId { get; set; }

    public static OperationResult SuccessResult(Guid transactionId) => new()
    {
        Success = true,
        TransactionId = transactionId
    };

    public static OperationResult FailureResult(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
