# Payments System — Build Prompt (Magenta.Payment.*)

You are an AI coding assistant working in a .NET 9 solution that already contains:

- Magenta.Wallet.* (wallet system with double-entry ledger, RabbitMQ outbox/inbox)
- PostgreSQL
- RabbitMQ
- ASP.NET Core with cookie-based authentication (ASP.NET Identity)

Your task is to implement the Payments System.  
This system orchestrates deposits and withdrawals between:

- Frontend (players)
- External payment providers (crypto gateways)
- Wallet system (balances and liabilities)

The Payments system MUST follow SOLID principles, clean architecture, and must not introduce unnecessary third-party libraries EXCEPT Hangfire (explicitly required for the Workers host). Use only essential infrastructure libraries already present in the solution (e.g., Npgsql, ASP.NET Core, Swagger). Use Hangfire with PostgreSQL storage for job persistence and distributed locking.

Namespaces/projects MUST use:

- Magenta.Payment.Domain
- Magenta.Payment.Application
- Magenta.Payment.Infrastructure
- Magenta.Payment.API
- Magenta.Payment.Workers (separate process: Hangfire host only)

---

## 1. Responsibilities of the Payments System

Payments is responsible for:

1) Creating and managing deposit sessions (QR / address generation)
2) Verifying deposits with external providers
3) Initiating withdrawals through providers
4) Coordinating with Wallet to reserve and finalize funds
5) Emitting domain events via RabbitMQ using outbox pattern
6) Tracking payment state machines
7) Guaranteeing idempotency and correctness
8) Providing APIs for Frontend and provider webhooks
9) Running background jobs for polling, reconciliation, and outbox publishing (via Hangfire Workers host)

Payments MUST NOT:
- Modify wallet balances directly
- Query wallet database directly
- Track player balances itself

Wallet remains the source of truth for balances and liabilities.

---

## 2. Communication Rules (Very Important)

### REST
Used for:
- Frontend → Payments
- Providers → Payments (webhooks)

### gRPC
Used ONLY for synchronous internal operations:
- Payments → Wallet

Examples:
- ReserveWithdrawal
- ApplyDepositSettlement

### RabbitMQ
Used ONLY for:
- Asynchronous state propagation
- Retryable processing
- Decoupling

RabbitMQ MUST NOT be used for immediate decision making.

---

## 3. Accounting Model Clarification (CRITICAL)

Definitions:

- HOUSE is a liability mirror account, NOT casino profit or treasury
- Deposits increase player balance AND casino liability
- Withdrawals decrease player liability
- Casino treasury is external and tracked separately (providers/custody/banks)

Correct ledger directions (applied by Wallet):

Deposit settlement:
- DR HOUSE
- CR PLAYER:MAIN

Withdrawal reserve:
- DR PLAYER:MAIN
- CR PLAYER:WITHDRAW_HOLD

Withdrawal settlement:
- DR PLAYER:WITHDRAW_HOLD
- CR HOUSE (liability reduced)

Payments MUST NOT reinterpret ledger directions.

---

## 4. Core Flows

### 4.1 Deposit Flow (Player → Casino)

Model separation:
- deposit_sessions = intent to deposit (address/QR lifecycle)
- deposit_requests = detected transaction (actual money movement lifecycle)

Flow:

1) Frontend calls Payments API to create a deposit session (Idempotency-Key header required)
2) Payments requests address / invoice from provider
3) Payments stores deposit_session status=OPEN and returns address/QR/memo + expiresAt
4) Player sends funds on-chain
5) Provider notifies Payments via webhook (deposit_detected / txHash)
6) Webhook is stored via inbox pattern (see section 9) and then processed asynchronously
7) Payments verifies tx with provider (VerifyDeposit)
8) Payments upserts deposit_request (tx_hash unique) and sets status=CONFIRMED when verified
9) Payments calls Wallet via gRPC ApplyDepositSettlement (synchronous) using an idempotency reference (deposit_id or tx_hash)
10) Wallet applies ledger postings (DR HOUSE / CR PLAYER:MAIN) and returns OK (idempotent on reference)
11) Payments marks deposit_request status=SETTLED and deposit_session status=COMPLETED inside a DB transaction, and writes outbox event payments.deposit.settled in the same transaction
12) Outbox publisher later emits the event after commit

IMPORTANT FAILURE CASE (must be handled):
- If step 9 succeeds (wallet credited) but step 11 DB transaction fails, the system MUST recover safely.
  Requirements:
  - Wallet settlement gRPC must be idempotent using deposit_id/tx_hash reference.
  - Payments must retry step 11 later (poller/reconciliation), and re-calling wallet settlement must be safe (no double credit).
  - Settlement completion in Payments must be transactionally consistent: update statuses + insert outbox event in one DB transaction.

Late deposit policy (explicit):
- If deposit arrives after deposit_session expired, still create deposit_request and settle it (recommended), but add metadata late=true and keep session EXPIRED (or mark COMPLETED with late flag). Never lose funds due to session expiry.

---

### 4.2 Withdrawal Flow (Casino → Player)

1) Frontend calls Payments API to request withdrawal (Idempotency-Key header required)
2) Payments creates withdrawal_request status=REQUESTED
3) Payments calls Wallet via gRPC ReserveWithdrawal (synchronous)
4) On success: Payments updates withdrawal_request status=PROCESSING
5) Payments sends withdrawal to provider (SendWithdrawal)
6) Provider responds with provider_reference and optionally tx_hash:
   - Payments sets status=BROADCASTED, stores provider_reference/tx_hash
7) Payments writes outbox event payments.withdrawal.broadcasted (INFORMATIONAL; Wallet does not consume)
8) Provider confirms settlement via webhook OR poller confirms confirmations reached:
   - Payments sets status=SETTLED, stores final tx_hash if available
   - Payments writes outbox event payments.withdrawal.settled
9) If provider rejects/fails:
   - Payments sets status=FAILED, fail_reason
   - Payments writes outbox event payments.withdrawal.failed

Wallet consumption (explicit):
- Wallet consumes ONLY:
  - payments.withdrawal.settled
  - payments.withdrawal.failed
- Wallet consumes via RabbitMQ inbox pattern:
  - message stored in wallet.inbox_events
  - wallet InboxProcessorWorker applies ledger postings and marks processed
- Wallet does NOT consume payments.withdrawal.broadcasted.

Optional informational event:
- wallet.withdrawal.reserved may exist for observability/reporting, but is NOT required for orchestration because ReserveWithdrawal is synchronous.

---

## 5. State Machines (Must be explicit per table)

### 5.1 deposit_sessions.status
- OPEN
- EXPIRED
- COMPLETED

Rules:
- OPEN → COMPLETED when a deposit_request becomes SETTLED
- OPEN → EXPIRED when expires_at passed
- EXPIRED is an intent timeout; late deposits can still be settled via deposit_requests

### 5.2 deposit_requests.status
- PENDING (optional if inserted before verification)
- CONFIRMED (provider verified / enough confirmations)
- SETTLED (Wallet credited successfully)
- FAILED

### 5.3 withdrawal_requests.status
- REQUESTED
- PROCESSING
- BROADCASTED
- SETTLED
- FAILED

---

## 6. Database Schema (Payments) — SQL Migrations Required

Implement SQL migrations for:

### payment_providers
- provider_id (PK)
- name
- type (CRYPTO/FIAT)
- api_base_url
- is_active
- created_at

### deposit_sessions
- session_id (UUID PK)
- player_id (BIGINT)
- provider_id (FK)
- currency_network_id (INT)  -- references Wallet currency_networks
- address
- memo_or_tag
- provider_reference (optional)
- expected_amount_minor (optional)
- confirmations_required
- status (OPEN/EXPIRED/COMPLETED)
- expires_at
- metadata JSONB
- created_at
- updated_at

### deposit_requests
- deposit_id (UUID PK)
- session_id (UUID FK nullable)
- player_id (BIGINT)
- provider_id (FK)
- currency_network_id (INT)
- tx_hash TEXT UNIQUE NOT NULL
- amount_minor BIGINT NOT NULL
- confirmations_received INT DEFAULT 0
- confirmations_required INT DEFAULT 1
- status (PENDING/CONFIRMED/SETTLED/FAILED)
- metadata JSONB
- created_at
- updated_at

Dedup note:
- tx_hash UNIQUE prevents duplicate deposit entries for the same chain tx.
- Additionally, provider webhooks may be duplicated; handle duplicates by ignoring conflicts and returning 200 OK.

### withdrawal_requests
- withdrawal_id (UUID PK)
- player_id (BIGINT)
- provider_id (FK)
- currency_network_id (INT)
- amount_minor BIGINT NOT NULL
- fee_minor BIGINT DEFAULT 0
- target_address TEXT NOT NULL
- provider_reference TEXT
- tx_hash TEXT
- status (REQUESTED/PROCESSING/BROADCASTED/SETTLED/FAILED)
- fail_reason TEXT
- metadata JSONB
- created_at
- updated_at

### idempotency_keys
- source TEXT NOT NULL
- idempotency_key TEXT NOT NULL
- tx_id UUID NULL
- created_at TIMESTAMPTZ DEFAULT now()
PK: (source, idempotency_key)

Header standard:
- Use exactly: Idempotency-Key

### outbox_events
Follow Wallet pattern:
- id BIGSERIAL PK
- event_type TEXT
- routing_key TEXT
- payload JSONB
- created_at TIMESTAMPTZ
- published_at TIMESTAMPTZ NULL
- publish_attempts INT DEFAULT 0
- last_error TEXT NULL

### inbox_events
This is REQUIRED for provider webhooks (not optional):
- id BIGSERIAL PK
- source TEXT NOT NULL
- idempotency_key TEXT NOT NULL
- payload JSONB NOT NULL
- received_at TIMESTAMPTZ DEFAULT now()
- processed_at TIMESTAMPTZ NULL
- last_error TEXT NULL
UNIQUE (source, idempotency_key)

Provider webhook controllers MUST:
- Validate signature
- Insert into inbox_events (idempotent)
- Return 200 quickly
Processing is done asynchronously by workers.

---

## 7. Idempotency Rules

### Frontend → Payments (HTTP)
- Frontend MUST send Idempotency-Key header on:
  - POST /api/deposits/sessions
  - POST /api/withdrawals
- Frontend MUST reuse the same Idempotency-Key when retrying the same user intent.
- Payments must enforce uniqueness using idempotency_keys.

Frontend guidance:
- Generate one UUID when the user starts the action.
- Keep it stable across retries/double-clicks until final success/failure.

### Provider → Payments (Webhooks)
- Webhooks MUST be stored in inbox_events (source=providerName, idempotency_key=providerEventId or txHash+eventType).
- Use tx_hash uniqueness in deposit_requests as additional deduplication.

---

## 8. External Provider Abstraction

Define IPaymentProvider operations:

- CreateDepositSessionAsync (returns address/QR/memo/tag, provider_reference, expiresAt)
- VerifyDepositAsync(txHash) (returns amount_minor, currency_network_id, confirmations)
- SendWithdrawalAsync(request) (returns provider_reference and tx_hash if available)
- GetTransactionStatusAsync(reference) (returns status + confirmations)

Adapters:
- Map provider API to internal models
- Handle provider-specific errors
- Verify signatures if needed
- Never touch wallet logic

---

## 9. Background Jobs via Hangfire (Magenta.Payment.Workers)

You will implement a separate executable project Magenta.Payment.Workers that:

- Hosts Hangfire Server (no HTTP endpoints required)
- Uses Hangfire PostgreSQL storage for distributed locks and job persistence
- Runs recurring jobs only
- References Magenta.Payment.Infrastructure and Magenta.Payment.Application directly (this is intended)
- Does NOT reference Magenta.Payment.API

Storage clarification:
- Hangfire can use the same PostgreSQL instance as Payments DB.
- Use a dedicated schema (e.g., schema "hangfire") or separate database; either is acceptable but must be explicit in config.
- Payments tables remain in their normal schema; Hangfire tables are isolated by schema/database.

Workers dependency rule (your preference):
- Workers references Infrastructure so it can reuse repositories, providers, and DB access code.
- Do NOT extract a separate shared project unless required.

Idempotency requirement inside jobs:
- Every job must be safe to run multiple times without duplicating outcomes.
- Always guard updates with current status checks and unique constraints.

### 9.1 ProviderPollerJob (scope expanded)
Purpose:
- Poll provider APIs for deposits/withdrawals that are not final

Behavior:
- Runs on configurable interval
- Poll deposit_requests in:
  - PENDING (if verification hasn’t happened due to delayed webhook processing)
  - CONFIRMED where confirmations_received < confirmations_required
- Poll withdrawal_requests in:
  - PROCESSING
  - BROADCASTED
- When state changes:
  - Update DB transactionally
  - Insert outbox events in the same transaction
- Never call Wallet from poller unless you are advancing a deposit to SETTLED (and that call must be idempotent)

### 9.2 WebhookInboxProcessorJob (REQUIRED)
Purpose:
- Process provider webhook inbox events

Behavior:
- Runs frequently
- Reads inbox_events where processed_at is NULL (batch with locking)
- For each event:
  - Verify/normalize payload
  - Upsert deposit_requests or update withdrawal_requests
  - Write outbox events as needed
  - Mark inbox_event processed_at
- Must be idempotent and safe under retries

### 9.3 ReconciliationJob (scope clarified)
Purpose:
- Detect inconsistencies between Payments DB and provider state

Behavior:
- Runs less frequently
- Finds “stuck” records (PROCESSING too long, BROADCASTED too long, CONFIRMED too long)
- Queries provider status
- Drives Payments records forward (update status + emit events) or flags manual review
- Must NOT mutate wallet balances directly
- Must NOT bypass settlement rules; it only fixes Payments state and emits events

### 9.4 OutboxPublisherJob (continuous behavior clarified)
Purpose:
- Publish outbox_events to RabbitMQ reliably

Behavior requirement:
- Functionally continuous dispatcher, like Wallet’s OutboxDispatcherWorker.
- If implemented as Hangfire recurring job, implement it as a loop with a time budget:
  - Run: fetch batch, publish, update published_at, repeat with small delay
  - Exit after (e.g.) 10–30 seconds, then let Hangfire schedule it again immediately/minutely as configured
- The goal is low latency publishing without CPU spinning.
- Must be safe with multiple Workers pods (either:
  - one dispatcher queue/lock, or
  - rely on DB row locking: SELECT ... FOR UPDATE SKIP LOCKED)

---

## 10. Wallet Integration (Explicit)

Payments interacts with Wallet ONLY via gRPC:

- ReserveWithdrawal
- ApplyDepositSettlement

Wallet consumes RabbitMQ events using inbox pattern:
- payments.withdrawal.settled
- payments.withdrawal.failed

Wallet applies ledger postings only when its inbox worker processes events (transactional, idempotent).

Payments MUST treat Wallet as authoritative for balances.

---

## 11. Security

- Frontend APIs use secure cookie authentication
- Provider webhooks use signature verification + secret
- Admin-only endpoints must be protected

---

## 12. Database Initialization on Startup

### 12.1 Magenta.Payment.API (Program.cs)
After `app.Build()` and before `app.Run()`, add database initialization:

```csharp
// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Payment database tables created successfully.");
        }
        else
        {
            context.Database.Migrate();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Payment database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize payment database: {ErrorMessage}", ex.Message);
        throw;
    }
}
```

### 12.2 Magenta.Payment.Grpc (Program.cs)
If gRPC service needs direct DB access, add the same initialization pattern as API.
If gRPC only calls Application layer (which uses Infrastructure), DB initialization may not be needed here.

### 12.3 Magenta.Payment.Workers (Program.cs)
Workers host MUST initialize both:
1. PaymentDbContext (for job data access)
2. Hangfire storage (PostgreSQL)

Add after service registration and before starting Hangfire server:

```csharp
// Initialize Payment database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
        }
        else
        {
            context.Database.Migrate();
        }
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Payment database initialized for Workers host.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize payment database: {ErrorMessage}", ex.Message);
        throw;
    }
}

// Hangfire storage is initialized automatically when Hangfire Server starts,
// but ensure connection string is configured correctly in appsettings.
```

Note: Hangfire will create its own tables in the configured schema/database on first run.

---

## 13. Swagger

- Enable Swagger in Magenta.Payment.API
- Document:
  - Deposit endpoints
  - Withdrawal endpoints
  - Webhook endpoints
  - Idempotency-Key header requirement
  - Error responses
- Swagger MUST NOT expose internal endpoints

---

## 14. Multi-Currency Rules

- currency_network_id is mandatory everywhere
- Decimals come from Wallet currency_networks table (currency_networks.decimals)
- Payments MUST NOT store decimals independently
- All amounts are integer minor units:
  amount_minor = amount_major * (10 ^ decimals)

---

## 15. Non-Functional Requirements

- Safe under retries and duplicates
- Safe under multiple pods
- Inbox/outbox patterns enforced
- Structured logging
- Clear exception handling
- Consistent UTC timestamps

---

## 16. Deliverables

Generate:

1) Full Payments system code (API + Infrastructure + Domain + Application)
2) Separate Workers host running Hangfire jobs (Magenta.Payment.Workers) that references Infrastructure and Application
3) SQL migration scripts
4) Provider abstraction + example adapter
5) RabbitMQ outbox publisher implementation
6) Swagger documentation
7) Unit tests covering:
   - State transitions
   - Idempotency (HTTP + webhook inbox)
   - Provider retry handling
   - Wallet integration (mock gRPC client)
   - Outbox publish correctness and dedupe

Do NOT modify Wallet business logic.  
Do NOT bypass ledger rules.  
Follow this prompt exactly.
