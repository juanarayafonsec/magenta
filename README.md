# Magenta - Clean Architecture ASP.NET Core Web API

A production-ready C# .NET 9 solution implementing user registration functionality following Clean Architecture principles.

## Solution Structure

```
Magenta/
├── src/
│   ├── Magenta.Domain/          # Domain layer - entities and interfaces
│   ├── Magenta.Application/     # Application layer - business logic and DTOs
│   ├── Magenta.Infrastructure/  # Infrastructure layer - data access and external services
│   └── Magenta.API/            # API layer - controllers and endpoints
├── test/
│   └── Magenta.Application.Tests/  # Unit tests
└── Magenta.sln                 # Solution file
```

## Features

- **User Registration**: Complete user registration with validation
- **ASP.NET Core Identity**: Built-in authentication and user management
- **PostgreSQL**: Database provider with Entity Framework Core
- **Swagger/OpenAPI**: API documentation and testing interface
- **Clean Architecture**: Separation of concerns with dependency inversion
- **Unit Tests**: Comprehensive test coverage for business logic
- **Validation**: Manual validation with detailed error messages

## Prerequisites

- .NET 9 SDK
- PostgreSQL database
- Visual Studio 2022 or VS Code (optional)

## Setup Instructions

### 1. Database Setup

1. Install PostgreSQL on your system
2. Create a database named `MagentaDb` (or update connection string in `appsettings.json`)
3. Update the connection string in `src/Magenta.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MagentaDb;Username=your_username;Password=your_password"
  }
}
```

### 2. Build and Run

1. Clone the repository
2. Navigate to the solution directory
3. Restore packages and build:

```bash
dotnet restore
dotnet build
```

4. Run the APIs:

```bash
# Terminal 1 - Registration Service
cd src/Magenta.Registration.API
dotnet run

# Terminal 2 - Authentication Service  
cd src/Magenta.Authentication.API
dotnet run
```

5. Open your browser and navigate to:
   - Registration API: `https://localhost:7001/swagger`
   - Authentication API: `https://localhost:7018/swagger`

### 3. Run Tests

```bash
dotnet test
```

## API Endpoints

### Registration Service (Port 7001)

#### POST /api/registration/register
Registers a new user.

**Request Body:**
```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "TestPassword123!",
  "confirmPassword": "TestPassword123!"
}
```

### Authentication Service (Port 7018)

#### POST /api/auth/login
Authenticates a user and returns JWT tokens.

**Request Body:**
```json
{
  "usernameOrEmail": "testuser",
  "password": "TestPassword123!",
  "rememberMe": false
}
```

**Response:**
```json
{
  "success": true,
  "accessToken": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresAt": "2024-01-01T00:00:00Z"
}
```

## Architecture

This project uses a **microservices architecture** with event-driven communication:

### Registration Service
- **Domain**: User registration entities and business rules
- **Application**: Registration business logic and DTOs
- **Infrastructure**: Database access and event publishing
- **API**: Registration endpoints and Swagger documentation

### Authentication Service  
- **Domain**: Authentication entities and JWT token management
- **Application**: Authentication business logic and DTOs
- **Infrastructure**: Database access and event subscription
- **API**: Authentication endpoints and Swagger documentation

### Event-Driven Communication
- **RabbitMQ**: Message broker for inter-service communication
- **Events**: UserCreatedEvent, UserUpdatedEvent, UserDeletedEvent
- **Resilience**: Services can operate independently with eventual consistency

## Technologies Used

- .NET 9
- ASP.NET Core Web API
- ASP.NET Core Identity
- Entity Framework Core
- PostgreSQL
- Swagger/OpenAPI
- xUnit (testing)
- Moq (mocking)

## Validation Rules

- **Username**: 3-50 characters, alphanumeric with hyphens and underscores only
- **Email**: Valid email format, max 256 characters
- **Password**: 6-100 characters
- **Confirm Password**: Must match password
- **Uniqueness**: Username and email must be unique

## Error Handling

The API provides detailed error responses for:
- Validation errors
- Duplicate username/email
- Server errors

## Development

### Adding New Features

1. Add domain entities and interfaces in `Magenta.Domain`
2. Implement business logic in `Magenta.Application`
3. Add data access in `Magenta.Infrastructure`
4. Create API endpoints in `Magenta.API`
5. Write unit tests in `Magenta.Application.Tests`

### Code Style

The project uses `.editorconfig` for consistent code formatting. Key rules:
- 4 spaces for indentation
- UTF-8 encoding
- Trim trailing whitespace
- Insert final newline

## License

This project is for educational purposes.
