# Magenta.Authentication.API

A secure authentication and authorization API built with ASP.NET Core 9, featuring JWT tokens, refresh tokens, and comprehensive security measures following Clean Architecture principles.

## 🏗️ **Clean Architecture Structure**

```
src/Magenta.Authentication.API/
├── Controllers/           # API Controllers
├── Program.cs            # Application startup
└── appsettings.json     # Configuration

src/Magenta.Authentication.Application/
├── DTOs/                 # Data Transfer Objects
├── Interfaces/           # Service interfaces
└── Services/            # Application services

src/Magenta.Authentication.Domain/
├── Entities/            # Domain entities
└── Interfaces/          # Domain interfaces

src/Magenta.Authentication.Infrastructure/
├── Data/               # Database context
├── Services/           # Infrastructure services
└── Extensions/         # Service configuration
```

## 🔐 **Security Features**

- **JWT Bearer Token Authentication** with secure token validation
- **Refresh Token Rotation** for enhanced security
- **Password Security** with PBKDF2 hashing and strong policies
- **Account Lockout** protection against brute force attacks
- **Security Headers** (HSTS, CSP, XSS Protection, etc.)
- **HTTPS Enforcement** with secure cookie settings
- **Claims-based Authorization** with custom policies
- **IP Address Tracking** for audit trails

## 🚀 **API Endpoints**

### Authentication Endpoints

| Method | Endpoint | Description | Authentication Required |
|--------|----------|-------------|------------------------|
| POST | `/api/auth/login` | User login with JWT + refresh token | No |
| POST | `/api/auth/refresh-token` | Refresh access token | No |
| POST | `/api/auth/logout` | Revoke refresh token | No |
| GET | `/api/auth/me` | Get user profile and claims | Yes |

### Request/Response Examples

#### Login Request
```json
POST /api/auth/login
{
  "usernameOrEmail": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

#### Login Response
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-refresh-token",
  "expiresIn": 900,
  "refreshExpiresIn": 604800,
  "tokenType": "Bearer",
  "user": {
    "id": "user-id",
    "username": "user@example.com",
    "email": "user@example.com",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

#### Refresh Token Request
```json
POST /api/auth/refresh-token
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

#### User Profile Response
```json
GET /api/auth/me
Authorization: Bearer <access-token>

{
  "success": true,
  "user": {
    "id": "user-id",
    "username": "user@example.com",
    "email": "user@example.com",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "claims": [
    {
      "type": "sub",
      "value": "user-id"
    },
    {
      "type": "email",
      "value": "user@example.com"
    }
  ]
}
```

## ⚙️ **Configuration**

### JWT Settings
```json
{
  "Jwt": {
    "Issuer": "Magenta.Authentication",
    "Audience": "Magenta.Users",
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForSecurity123456789",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MagentaAuthentication;Username=postgres;Password=postgres"
  }
}
```

## 🔒 **Security Policies**

### Password Requirements
- Minimum 8 characters
- Must contain uppercase letters
- Must contain lowercase letters
- Must contain digits
- Must contain special characters

### Account Lockout
- 5 failed attempts trigger lockout
- 15-minute lockout duration
- Automatic lockout for new users

### Token Security
- Access tokens expire in 15 minutes
- Refresh tokens expire in 7 days
- Refresh token rotation on each use
- Secure token storage with hashing

## 🛡️ **Security Headers**

The API automatically adds security headers:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`

## 🚀 **Getting Started**

1. **Configure Database**: Update connection string in `appsettings.json`
2. **Set JWT Secret**: Change the `Jwt:SecretKey` to a secure random string
3. **Run the API**: `dotnet run` in the API project directory
4. **Access Swagger**: Navigate to `https://localhost:7018/swagger`

## 📝 **Usage Examples**

### Using with HttpClient
```csharp
// Login
var loginRequest = new
{
    usernameOrEmail = "user@example.com",
    password = "SecurePassword123!",
    rememberMe = false
};

var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();

// Use access token for authenticated requests
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

// Get user profile
var profileResponse = await httpClient.GetAsync("/api/auth/me");
var profile = await profileResponse.Content.ReadFromJsonAsync<MeResponse>();
```

### Token Refresh
```csharp
// When access token expires, use refresh token
var refreshRequest = new
{
    refreshToken = loginResult.RefreshToken
};

var refreshResponse = await httpClient.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);
var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();

// Update authorization header with new token
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);
```

## 🔧 **Architecture**

- **Clean Architecture** with separation of concerns
- **Dependency Injection** using built-in .NET DI
- **Repository Pattern** for data access
- **Service Layer** for business logic
- **DTOs** for request/response models
- **Claims-based Authorization** for security

## 🧪 **Testing**

The API includes comprehensive error handling and validation:
- Input validation with detailed error messages
- Secure error responses (no sensitive data exposure)
- Comprehensive logging for security auditing
- Rate limiting protection through account lockout

## 🔐 **Security Best Practices**

1. **Always use HTTPS** in production
2. **Rotate JWT secret keys** regularly
3. **Monitor failed login attempts**
4. **Implement rate limiting** at the API gateway level
5. **Use secure password policies**
6. **Regular security audits** of token usage
7. **Implement proper CORS policies**
8. **Monitor and log security events**

## 📚 **Dependencies**

- ASP.NET Core 9
- Microsoft.AspNetCore.Identity
- Microsoft.AspNetCore.Authentication.JwtBearer
- Entity Framework Core with PostgreSQL
- Swagger/OpenAPI for documentation
- System.IdentityModel.Tokens.Jwt

## 🚨 **Important Security Notes**

- **Never log sensitive data** (passwords, tokens)
- **Use secure random keys** for JWT signing
- **Implement proper CORS** for your frontend domains
- **Monitor token usage** and implement token revocation
- **Regular security updates** of all dependencies
- **Implement proper error handling** to avoid information leakage

## 🎯 **Project Structure**

### Domain Layer (`Magenta.Authentication.Domain`)
- **Entities**: `RefreshToken` entity for token management
- **Interfaces**: `ITokenService` for token operations

### Application Layer (`Magenta.Authentication.Application`)
- **DTOs**: Request/Response models for API endpoints
- **Interfaces**: `IAuthService` for authentication operations
- **Services**: `AuthService` implementing authentication logic

### Infrastructure Layer (`Magenta.Authentication.Infrastructure`)
- **Data**: `AuthenticationDbContext` for database operations
- **Services**: `TokenService` implementing JWT operations
- **Extensions**: Service configuration and DI setup

### API Layer (`Magenta.Authentication.API`)
- **Controllers**: `AuthController` with authentication endpoints
- **Program.cs**: Application startup and configuration
- **Configuration**: JWT and database settings

This structure follows Clean Architecture principles with clear separation of concerns and dependency inversion.
