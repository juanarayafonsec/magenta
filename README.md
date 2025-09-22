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

4. Run the API:

```bash
cd src/Magenta.API
dotnet run
```

5. Open your browser and navigate to `https://localhost:7xxx` (Swagger UI will be available at the root)

### 3. Run Tests

```bash
dotnet test
```

## API Endpoints

### POST /api/auth/register

Registers a new user.

**Request Body:**
```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "password123",
  "confirmPassword": "password123"
}
```

**Response (Success):**
```json
{
  "success": true,
  "userId": "user-id",
  "username": "testuser",
  "email": "test@example.com",
  "errors": []
}
```

**Response (Error):**
```json
{
  "success": false,
  "userId": null,
  "username": null,
  "email": null,
  "errors": ["Username is already taken."]
}
```

## Architecture

### Domain Layer
- `User` entity with ASP.NET Core Identity integration
- `IUserRepository` interface for data access

### Application Layer
- `UserService` for business logic
- DTOs for request/response models
- Validation logic

### Infrastructure Layer
- `ApplicationDbContext` with Entity Framework Core
- `UserRepository` implementation
- PostgreSQL configuration
- Service registration extensions

### API Layer
- `AuthController` with registration endpoint
- Swagger/OpenAPI configuration
- Dependency injection setup

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
