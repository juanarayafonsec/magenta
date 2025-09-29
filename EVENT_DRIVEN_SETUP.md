# Event-Driven Architecture Setup

This document explains how to set up and run the event-driven microservices architecture.

## 🏗️ Architecture Overview

```
┌─────────────────────┐    Events    ┌─────────────────────┐
│   Registration      │─────────────►│   Authentication    │
│   Service           │              │   Service           │
│                     │              │                     │
│ • User Management   │              │ • JWT Tokens        │
│ • User Creation     │              │ • Refresh Tokens    │
│ • Profile Data      │              │ • Authentication    │
│                     │              │                     │
│ Database:           │              │ Database:           │
│ MagentaUsers        │              │ MagentaAuthentication│
└─────────────────────┘              └─────────────────────┘
            │                                    │
            └─────────── RabbitMQ ───────────────┘
```

## 🚀 Quick Start

### 1. Start Infrastructure Services

```bash
# Start RabbitMQ and PostgreSQL
docker-compose up -d

# Verify services are running
docker-compose ps
```

### 2. Access Management UIs

- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **PostgreSQL**: localhost:5432 (postgres/postgres)

### 3. Run the Services

```bash
# Terminal 1 - Registration Service
cd src/Magenta.Registration.API
dotnet run

# Terminal 2 - Authentication Service  
cd src/Magenta.Authentication.API
dotnet run
```

## 📋 Event Flow

### User Registration Flow

1. **User registers** via Registration API
2. **Registration Service** creates user in `MagentaUsers` database
3. **Registration Service** publishes `UserCreatedEvent` to RabbitMQ
4. **Authentication Service** receives event and creates user in `MagentaAuthentication` database
5. **User can now authenticate** via Authentication API

### User Authentication Flow

1. **User logs in** via Authentication API
2. **Authentication Service** validates credentials against local user data
3. **Authentication Service** generates JWT tokens
4. **Response** includes access and refresh tokens

## 🔧 Configuration

### RabbitMQ Configuration

Both services use the same RabbitMQ configuration:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "ExchangeName": "magenta.events",
    "QueueName": "authentication.user.events"
  }
}
```

### Database Configuration

**Registration Service:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MagentaUsers;Username=postgres;Password=postgres"
  }
}
```

**Authentication Service:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MagentaAuthentication;Username=postgres;Password=postgres"
  }
}
```

## 📊 Event Types

### UserCreatedEvent
- **Routing Key**: `user.created`
- **Published When**: New user registers
- **Data**: User profile, credentials, timestamps

### UserUpdatedEvent
- **Routing Key**: `user.updated`
- **Published When**: User profile is updated
- **Data**: Changed fields, timestamps

### UserDeletedEvent
- **Routing Key**: `user.deleted`
- **Published When**: User account is deleted
- **Data**: User ID, audit information

## 🛡️ Resilience Features

### Service Independence
- ✅ **Registration Service Down**: Authentication service continues with existing users
- ✅ **Authentication Service Down**: Registration service continues accepting new registrations
- ✅ **RabbitMQ Down**: Services degrade gracefully, users can still authenticate

### Event Processing
- ✅ **Message Acknowledgment**: Events are acknowledged only after successful processing
- ✅ **Retry Logic**: Failed messages are requeued for retry
- ✅ **Error Handling**: Comprehensive logging for troubleshooting

### Data Consistency
- ✅ **Eventual Consistency**: Services eventually synchronize via events
- ✅ **Duplicate Prevention**: Events include unique IDs to prevent duplicate processing
- ✅ **Audit Trail**: All events are logged with timestamps

## 🧪 Testing the Setup

### 1. Register a New User

```bash
curl -X POST https://localhost:7001/api/registration/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "TestPassword123!",
    "confirmPassword": "TestPassword123!"
  }'
```

### 2. Verify Event Processing

Check RabbitMQ Management UI:
1. Go to http://localhost:15672
2. Login with guest/guest
3. Navigate to "Exchanges" → "magenta.events"
4. Check message rates and routing

### 3. Authenticate the User

```bash
curl -X POST https://localhost:7018/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "testuser",
    "password": "TestPassword123!",
    "rememberMe": false
  }'
```

## 🔍 Monitoring

### RabbitMQ Monitoring
- **Management UI**: http://localhost:15672
- **Queue Metrics**: Monitor queue depth and processing rates
- **Connection Status**: Verify both services are connected

### Application Logs
Both services log event processing:
- Event publication (Registration service)
- Event consumption (Authentication service)
- Error handling and retries

## 🚨 Troubleshooting

### Common Issues

1. **RabbitMQ Connection Failed**
   - Verify Docker container is running: `docker-compose ps`
   - Check firewall settings for port 5672

2. **Events Not Processing**
   - Check RabbitMQ Management UI for queue status
   - Verify exchange and queue bindings
   - Check service logs for errors

3. **User Authentication Fails**
   - Verify user exists in both databases
   - Check event processing logs
   - Ensure password hashes are synchronized

### Debug Commands

```bash
# Check RabbitMQ status
docker-compose logs rabbitmq

# Check service logs
docker-compose logs postgres

# Verify database connections
psql -h localhost -U postgres -d MagentaUsers
psql -h localhost -U postgres -d MagentaAuthentication
```

## 🎯 Benefits

1. **True Microservices**: Complete service independence
2. **Scalability**: Services can scale independently
3. **Resilience**: Service failures don't cascade
4. **Eventual Consistency**: Data synchronizes automatically
5. **Audit Trail**: Complete event history
6. **Technology Flexibility**: Each service can use different tech stacks

This event-driven architecture provides the ultimate in microservices independence while maintaining data consistency and system reliability.
