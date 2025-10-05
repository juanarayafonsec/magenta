# Magenta Microservices Architecture

## Current Project Structure

```
Magenta Solution
├── Registration Service (Port 7001)
│   ├── Magenta.Registration.API
│   ├── Magenta.Registration.Application
│   ├── Magenta.Registration.Infrastructure
│   └── Magenta.Registration.Domain
│
├── Authentication Service (Port 7018)
│   ├── Magenta.Authentication.API
│   ├── Magenta.Authentication.Application
│   ├── Magenta.Authentication.Infrastructure
│   └── Magenta.Authentication.Domain
│
└── Tests
    └── Magenta.Registration.Application.Tests
```

## Communication Pattern

```
┌─────────────────────┐    Events via RabbitMQ    ┌─────────────────────┐
│   Registration      │──────────────────────────►│   Authentication    │
│   Service           │                           │   Service           │
│                     │                           │                     │
│ • User Registration │                           │ • JWT Tokens        │
│ • User Management   │                           │ • Authentication    │
│ • Profile Data      │                           │ • Token Refresh     │
│                     │                           │                     │
│ Database:           │                           │ Database:           │
│ MagentaUsers        │                           │ MagentaAuthentication│
└─────────────────────┘                           └─────────────────────┘
```

## Key Points

### ✅ **No Direct Dependencies**
- Registration API only references its own layers
- Authentication API only references its own layers
- **Zero coupling** between services at the code level

### ✅ **Event-Driven Communication**
- Services communicate via **RabbitMQ events**
- **Asynchronous** and **resilient**
- Services can operate independently

### ✅ **Independent Databases**
- Registration Service: `MagentaUsers` database
- Authentication Service: `MagentaAuthentication` database
- **No shared database** dependencies

### ✅ **Independent Deployment**
- Each service can be deployed separately
- Each service can scale independently
- Each service can have its own technology stack

## Event Flow

1. **User Registration** → Registration Service
2. **UserCreatedEvent** → Published to RabbitMQ
3. **Authentication Service** → Subscribes to events
4. **User Data Sync** → Authentication Service creates local user
5. **User Authentication** → Authentication Service handles login

## Benefits

- **True Microservices**: Complete service independence
- **Resilience**: Service failures don't cascade
- **Scalability**: Services scale independently
- **Technology Flexibility**: Each service can evolve separately
- **Team Independence**: Different teams can work on different services
