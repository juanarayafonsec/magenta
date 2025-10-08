# Magenta.Wallet.API

## Overview

The Magenta Wallet API is a placeholder microservice for managing cryptocurrency wallets and transactions within the Magenta platform.

## Project Structure

This API follows the Clean Architecture pattern with the following layers:

- **Magenta.Wallet.API** - Web API layer (presentation)
- **Magenta.Wallet.Application** - Application logic and DTOs
- **Magenta.Wallet.Domain** - Domain entities and business logic
- **Magenta.Wallet.Infrastructure** - Data access and external services

## Configuration

### Endpoints

The API runs on the following ports:
- **HTTP**: `http://localhost:5200`
- **HTTPS**: `https://localhost:7200`

### Default Route

- `GET /weatherforecast` - Placeholder endpoint for testing

## Running the API

```bash
# From the project directory
cd src/Magenta.Wallet.API
dotnet run

# Or from the solution root
dotnet run --project src/Magenta.Wallet.API/Magenta.Wallet.API.csproj
```

## Development

### Testing the API

You can use the included `.http` file for testing:

```bash
# Using the Magenta.Wallet.API.http file
@Magenta.Wallet.API_HostAddress = http://localhost:5200

GET {{Magenta.Wallet.API_HostAddress}}/weatherforecast/
Accept: application/json
```

### Adding Controllers

Create new controllers in the `Controllers/` folder:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Magenta.Wallet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    [HttpGet]
    public IActionResult GetWallet()
    {
        return Ok(new { message = "Wallet API" });
    }
}
```

## Integration with Cookie Authentication

To enable cookie authentication for this API (similar to Content API):

1. Create `Extensions/ApplicationServiceExtension.cs` with authentication configuration
2. Update `Program.cs` to use authentication middleware
3. Ensure cookie name matches: `"MagentaAuth"`
4. Add `[Authorize]` attributes to protected endpoints
5. Access user info via `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`

See the `COOKIE_AUTH_SUMMARY.md` in the project root for detailed instructions.

## Future Development

This is a placeholder project. Future development may include:

- **Wallet Management**: Create, read, update, delete crypto wallets
- **Balance Tracking**: Track balances for multiple cryptocurrencies
- **Transaction History**: Record and query transaction history
- **Address Generation**: Generate unique addresses for deposits
- **Security**: Multi-signature support, encryption at rest
- **Integration**: Connect with blockchain networks and payment gateways

## Dependencies

- ASP.NET Core 9.0
- OpenAPI/Swagger support

## Related Projects

- `Magenta.Authentication.API` - User authentication and authorization
- `Magenta.Registration.API` - User registration
- `Magenta.Content.API` - Content management

## Notes

This is a placeholder project created to match the structure of other microservices in the Magenta platform. It includes the basic setup needed to get started but does not implement actual wallet functionality yet.

