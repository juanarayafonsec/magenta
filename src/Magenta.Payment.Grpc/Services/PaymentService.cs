namespace Magenta.Payment.Grpc.Services;

public class PaymentService : Grpc.PaymentService.PaymentServiceBase
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    // Payment service methods will be implemented here
}
