# 🚀 BUILD THE PAYMENTS SYSTEM MICROservice (.NET 9 + GRPC + POSTGRES + RABBITMQ)

You are building the **Payments System** of the Magenta platform.

I will attach a file named **payments-documentation.md**.  
That file is the **absolute source of truth** for architecture, API behavior, flows, database schema, and integration with Wallet System.

Follow these rules STRICTLY:

---

# 🧱 1. SYSTEM GOAL

Generate a production-ready **Payments microservice** in .NET 9 using the layered architecture:

- `Magenta.Payments.Domain`
- `Magenta.Payments.Application`
- `Magenta.Payments.Infrastructure`
- `Magenta.Payments.Grpc`
- `Magenta.Payments.API`

This service MUST implement:

- Deposit sessions (QR/address generation)
- Deposit detection (webhooks & polling)
- Deposit verification
- Deposit settlement (calling Wallet)
- Withdrawal request handling
- Withdrawal reservation sync with Wallet via gRPC
- Withdrawal execution via external providers
- Withdrawal final settlement
- Full idempotency system
- Outbox & Inbox event system
- Provider abstraction layer
- Provider webhook endpoints
- Background workers (polling, reconciliation, outbox publisher)
- Minor-unit integer currency handling

EVERYTHING MUST FOLLOW the semantics, APIs, SQL tables, flows, and diagrams in **payments-documentation.md**.

This documentation overrides any default assumptions.

---

# 🧩 2. ARCHITECTURE REQUIREMENTS

Use a CLEAN ARCHITECTURE layout:

### Domain Layer
- Entities: DepositSession, DepositRequest, WithdrawalRequest, Provider, etc.
- Value Objects: CurrencyNetwork, Money (minor units), TransactionStatus, etc.
- Aggregates where appropriate.
- Domain events where applicable.

### Application Layer
- Command handlers & query handlers.
- DTOs / request models.
- Interfaces for repository and provider integrations.
- Idempotency services.
- Background jobs orchestrations (application services).

### Infrastructure Layer
- PostgreSQL EF Core or Dapper repositories.
- Outbox and inbox persistence.
- Provider gateway adapters (REST clients).
- HTTP signature validation for provider webhooks.
- RabbitMQ publisher/consumer implementations.
- gRPC client for Wallet (ReserveWithdrawal, ApplyDepositSettlement).

### Grpc Layer
- Payments gRPC service implementing any internal RPCs described in documentation.
- Use proto definitions as required.

### API Layer
- Endpoints:
  - `POST /api/deposits/sessions`
  - `POST /api/withdrawals`
  - `POST /api/providers/{providerId}/webhook`
- Authentication using secure cookie as described in Wallet docs.
- Validation & idempotency enforced here.

Everything must exactly follow the **conceptual flow** from the documentation file.

---

# 💾 3. DATABASE MIGRATIONS

Create SQL (or EF migrations) exactly matching:

- `payment_providers`
- `deposit_sessions`
- `deposit_requests`
- `withdrawal_requests`
- `idempotency_keys`
- `outbox_events`
- `inbox_events`

Follow all fields, constraints, states, and rules defined in **payments-documentation.md**.

---

# 🔁 4. EVENTING & RELIABILITY

Use the **Outbox/Inbox** pattern:

### Outbox:
- On DB commit, write event → publish asynchronously.

### Inbox:
- Deduplicate RabbitMQ consumer actions.

Follow documentation for every event:

- `payments.deposit.settled`
- `payments.withdrawal.broadcasted`
- `payments.withdrawal.settled`
- `payments.withdrawal.failed`

Incoming wallet events:
- `wallet.withdrawal.reserved`

---

# 🌐 5. PROVIDER INTEGRATION LAYER

Implement a provider abstraction:

```csharp
public interface IPaymentProvider
{
    Task<CreateDepositSessionResult> CreateDepositSessionAsync(...);
    Task<ProviderDepositResult> VerifyDepositAsync(string txHashOrRef);
    Task<ProviderWithdrawalResult> SendWithdrawalAsync(WithdrawalRequest request);
    Task<ProviderTransactionStatus> GetTransactionStatusAsync(string reference);
}
```

Then create **provider-specific adapters**:
- BitGoAdapter
- FireblocksAdapter
- MockAdapter (for dev)

Each must exactly follow behavior described in **payments-documentation.md**.

---

# 🔒 6. IDEMPOTENCY

Implement idempotency EXACTLY as in the documentation:

- Each FE action must include an `idempotencyKey`.
- Insert `(source, idempotencyKey)` on first execution.
- On repeated calls, return the stored result.
- All gRPC and webhook actions must use idempotency.

---

# 🧮 7. MONEY HANDLING

Create a `Money` value object:

- Always minor units (long)
- Never decimal or double
- Conversion helpers using currency decimals from `currency_networks`

---

# 🧪 8. TESTING REQUIREMENTS

Generate:

- Unit tests for domain logic
- Integration tests for:
  - Deposit session creation
  - Provider webhook flows
  - Deposit verification → settlement → wallet call
  - Withdrawal reservation → provider → settlement
  - Idempotency behavior
  - Outbox → event publishing
  - Inbox → deduplication
  - Provider adapters (mocked)
- Test doubles for Wallet gRPC and Providers

Follow the testing strategy that mirrors the Wallet tests.

---

# 🔧 9. CODING GUIDELINES

- Use .NET 9 minimal APIs where appropriate
- Use dependency injection
- Use logging everywhere (structured logs)
- Use cancellation tokens
- Use async/await everywhere
- Do NOT use MediatR unless explicitly asked
- Keep dependencies minimal
- Use vertical slice pattern inside Application layer
- Ensure proper exception handling & retry logic

---

# 🎯 10. DELIVERY REQUIREMENTS

When generating code, follow this order:

1. Create folder structure  
2. Generate proto files + gRPC service  
3. Generate EF Core models or Dapper repositories  
4. Generate entity classes  
5. Generate application services & command handlers  
6. Generate provider adapters  
7. Generate API controllers  
8. Generate background workers  
9. Generate tests  
10. Ensure everything builds  

---

# 📄 11. IMPORTANT

**You MUST follow the architecture, tables, sequence diagrams, flow charts, and business rules EXACTLY as defined in `payments-documentation.md`.**

The documentation = the source of truth.

Do not improvise.  
Do not skip steps.  
Do not simplify flows.  
Do not create new endpoints or flows.  
Do not change naming conventions.

---

# 🚀 Begin generating the Payments System now.
