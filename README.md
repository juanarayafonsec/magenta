# Magenta - Clean Architecture ASP.NET Core Web API

A production-ready C# .NET 9 microservices solution implementing user registration, authentication, and content management following Clean Architecture principles.

## Solution Structure

```
Magenta/
├── src/
│   ├── Magenta.Registration.API/           # Registration API - user registration endpoints
│   ├── Magenta.Registration.Application/   # Registration business logic and DTOs
│   ├── Magenta.Registration.Domain/        # Registration domain entities
│   ├── Magenta.Registration.Infrastructure/ # Registration data access and external services
│   ├── Magenta.Authentication.API/         # Authentication API - login/logout endpoints
│   ├── Magenta.Authentication.Application/ # Authentication business logic and DTOs
│   ├── Magenta.Authentication.Domain/      # Authentication domain entities
│   ├── Magenta.Authentication.Infrastructure/ # Authentication data access and external services
│   ├── Magenta.Content.API/                # Content API - content management endpoints
│   ├── Magenta.Content.Application/        # Content business logic and DTOs
│   ├── Magenta.Content.Domain/             # Content domain entities
│   └── Magenta.Content.Infrastructure/     # Content data access and external services
├── test/
│   └── Magenta.Application.Tests/  # Unit tests
└── Magenta.sln                 # Solution file
```

## Features

- **User Registration**: Complete user registration with validation
- **User Authentication**: Cookie-based secure authentication with login/logout
- **Content Management**: Basic content management endpoints
- **CDN with Nginx**: Static file serving with caching, compression, and CORS support
- **Microservices Architecture**: Independent services with event-driven communication
- **ASP.NET Core Identity**: Built-in authentication and user management
- **PostgreSQL**: Database provider with Entity Framework Core
- **RabbitMQ**: Message broker for inter-service communication
- **Swagger/OpenAPI**: API documentation and testing interface
- **Clean Architecture**: Separation of concerns with dependency inversion
- **Unit Tests**: Comprehensive test coverage for business logic
- **Validation**: Manual validation with detailed error messages

## Prerequisites

- .NET 9 SDK
- Docker and Docker Compose
- PostgreSQL database (or use Docker)
- Visual Studio 2022 or VS Code (optional)

## Setup Instructions

### 1. Docker Setup (Recommended)

1. Start all infrastructure services with Docker Compose:

```bash
docker-compose up -d
```

This will start:
- **PostgreSQL** on port 5432
- **RabbitMQ** on port 5672 (Management UI on 15672)
- **Nginx CDN** on port 8080

2. Access the services:
   - RabbitMQ Management: http://localhost:15672 (guest/guest)
   - CDN: http://localhost:8080

### 2. Manual Database Setup (Alternative)

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

### 3. Build and Run

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

# Terminal 3 - Content Service
cd src/Magenta.Content.API
dotnet run
```

5. Open your browser and navigate to:
   - Registration API: `https://localhost:7001/swagger`
   - Authentication API: `https://localhost:7018/swagger`
   - Content API: `https://localhost:5127/swagger`
   - CDN: `http://localhost:8080`

### 4. Run Tests

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
Authenticates a user and sets secure authentication cookies.

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
  "message": "Login successful"
}
```

### Content Service (Port 5127)

#### GET /api/content
Gets all content items.

**Response:**
```json
200 OK
```

### CDN Service (Port 8080)

#### Static File Serving
Serves static files with caching, compression, and CORS support.

**Features:**
- Long-term caching for static assets (1 year)
- Gzip compression
- CORS headers for cross-origin requests
- Health check endpoint: `/healthz`

### File Management

#### Local File Management
Static files are managed directly in the `nginx/html/` folder.

**How to add files:**
1. Copy files to `nginx/html/` folder
2. Access them at `http://localhost:8080/filename.ext`
3. Create subfolders as needed (e.g., `nginx/html/images/`)

## Architecture

This project uses a **microservices architecture** with event-driven communication:

### Registration Service
- **Domain**: User registration entities and business rules
- **Application**: Registration business logic and DTOs
- **Infrastructure**: Database access and event publishing
- **API**: Registration endpoints and Swagger documentation

### Authentication Service  
- **Domain**: Authentication entities and cookie-based authentication
- **Application**: Authentication business logic and DTOs
- **Infrastructure**: Database access and event subscription
- **API**: Authentication endpoints and Swagger documentation

### Content Service
- **Domain**: Content entities and business rules
- **Application**: Content business logic and DTOs
- **Infrastructure**: Database access and external services
- **API**: Content management endpoints and Swagger documentation

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
