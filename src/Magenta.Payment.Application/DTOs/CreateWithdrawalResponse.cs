namespace Magenta.Payment.Application.DTOs;

public class CreateWithdrawalResponse
{
    public Guid WithdrawalId { get; set; }
    public string Status { get; set; } = string.Empty;
}
