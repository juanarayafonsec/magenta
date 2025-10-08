# Magenta.Payment.API

## Overview

The Magenta Payment API is a placeholder microservice for managing payment processing, transactions, and payment gateway integrations within the Magenta platform.

## Project Structure

This API follows the Clean Architecture pattern with the following layers:

- **Magenta.Payment.API** - Web API layer (presentation)
- **Magenta.Payment.Application** - Application logic and DTOs
- **Magenta.Payment.Domain** - Domain entities and business logic
- **Magenta.Payment.Infrastructure** - Data access and external services

## Configuration

### Endpoints

The API runs on the following ports:
- **HTTP**: `http://localhost:5201`
- **HTTPS**: `https://localhost:7201`

### Default Route

- `GET /weatherforecast` - Placeholder endpoint for testing
- `GET /api/payment` - Payment controller placeholder

## Running the API

```bash
# From the project directory
cd src/Magenta.Payment.API
dotnet run

# Or from the solution root
dotnet run --project src/Magenta.Payment.API/Magenta.Payment.API.csproj
```

## Development

### Testing the API

You can use the included `.http` file for testing:

```bash
# Using the Magenta.Payment.API.http file
@Magenta.Payment.API_HostAddress = http://localhost:5201

GET {{Magenta.Payment.API_HostAddress}}/weatherforecast/
Accept: application/json
```

### Adding Controllers

Create new controllers in the `Controllers/` folder:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetPayment(string id)
    {
        return Ok(new { message = "Payment API", paymentId = id });
    }
}
```

## Integration with Cookie Authentication

To enable cookie authentication for this API:

1. Create `Extensions/ApplicationServiceExtension.cs` with authentication configuration
2. Update `Program.cs` to use authentication middleware
3. Ensure cookie name matches: `"MagentaAuth"`
4. Add `[Authorize]` attributes to protected endpoints
5. Access user info via `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`

Example protected endpoint:

```csharp
[HttpPost("process")]
[Authorize]
public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
{
    // Get authenticated user ID from cookie
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Process payment for this user
    var result = await _paymentService.ProcessPaymentAsync(userId, request);
    
    return Ok(result);
}
```

## Future Development

This is a placeholder project. Future development may include:

- **Payment Processing**: Integration with payment gateways (Stripe, PayPal, etc.)
- **Transaction Management**: Create, track, and manage payment transactions
- **Payment Methods**: Support for credit cards, crypto payments, bank transfers
- **Refunds & Chargebacks**: Handle refund requests and dispute management
- **Payment History**: Query and report on payment transactions
- **Webhook Handling**: Process payment gateway callbacks and notifications
- **Security**: PCI compliance, tokenization, encryption
- **Multi-currency**: Support for multiple fiat and crypto currencies
- **Recurring Payments**: Subscription and recurring payment support
- **Payment Status**: Track payment states (pending, processing, completed, failed)

## Potential Entities

```csharp
// Domain Layer
- Payment.cs - Payment transaction entity
- PaymentMethod.cs - Payment method details
- PaymentStatus.cs - Payment status enum
- Transaction.cs - Financial transaction record
- Refund.cs - Refund request entity

// Application Layer DTOs
- PaymentRequest.cs - Initiate payment
- PaymentResponse.cs - Payment result
- TransactionDto.cs - Transaction details
- RefundRequest.cs - Request refund
```

## Integration Points

- **Wallet API**: Check balances, update wallet after payments
- **Authentication API**: Verify user identity
- **Event Bus**: Publish payment events for other services

## Dependencies

- ASP.NET Core 9.0
- OpenAPI/Swagger support

## Related Projects

- `Magenta.Authentication.API` - User authentication and authorization
- `Magenta.Wallet.API` - Wallet and balance management
- `Magenta.Registration.API` - User registration

## Notes

This is a placeholder project created to match the structure of other microservices in the Magenta platform. It includes the basic setup needed to get started but does not implement actual payment processing functionality yet.

## Security Considerations

When implementing payment processing:
- ✅ Never store raw credit card numbers
- ✅ Use tokenization for sensitive data
- ✅ Implement PCI DSS compliance
- ✅ Use HTTPS for all communication
- ✅ Validate all payment amounts server-side
- ✅ Implement rate limiting for payment endpoints
- ✅ Log all payment transactions for audit
- ✅ Use idempotency keys to prevent duplicate charges

