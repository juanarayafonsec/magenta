# PAYMENTS SYSTEM — GENERATION PROMPT

Use namespaces:

- Magenta.Payment.Domain
- Magenta.Payment.Application
- Magenta.Payment.Infrastructure
- Magenta.Payment.Api
- Magenta.Payment.Grpc

Build the Payments System described in payments-documentation.md following these rules. Apply S.O.L.I.D principles and clean architecture separation at all times.

---

# 1. System Role

Payments acts as the orchestrator between:

- Frontend (players)
- Wallet System (actual money movements)
- External payment providers (crypto gateways)

Payments:
- Creates deposit sessions.
- Handles provider webhooks.
- Verifies deposits.
- Initiates withdrawals.
- Coordinates with Wallet (reserve, settle, fail).
- Implements deposit and withdrawal state machines.
- Uses Outbox + Inbox + RabbitMQ.
- Enforces idempotency on all external input.

Payments does not change ledger directly.

---

# 2. Components

Implement:

- Magenta.Payment.Api (REST endpoints + Swagger)
- Magenta.Payment.Grpc (optional internal API)
- Magenta.Payment.Application (use cases)
- Magenta.Payment.Domain (entities, state machines)
- Magenta.Payment.Infrastructure (DB, MQ, providers, workers)

Controllers must be thin (SRP). Application uses interfaces. Infrastructure implements interfaces.

---

# 3. Database Schema

Implement exactly:

- payment_providers
- deposit_sessions
- deposit_requests
- withdrawal_requests
- idempotency_keys
- outbox_events
- inbox_events

Follow the documentation for enums, statuses, FK constraints.

---

# 4. Outbox and Inbox Patterns

## 4.1 Outbox (DB + POLLING)
- Insert row into outbox_events inside the same transaction as state change.
- OutboxPublisherWorker:
  - polls outbox_events WHERE status='PENDING'
  - publishes messages (RabbitMQ)
  - marks as SENT
  - retries failures

## 4.2 Inbox (MQ LISTENER + POLLING worker)

MQ listener:
- receives provider webhook messages or Wallet events
- stores into inbox_events
- ack message after DB write

InboxProcessorWorker:
- polls inbox_events WHERE processed_at IS NULL
- processes events exactly once
- dispatches to application layer
- marks processed

Follow SOLID: separate listener, inbox writer, event handlers, and worker orchestration.

---

# 5. Background Workers

Implement as HostedServices:

1. OutboxPublisherWorker (polling)
2. InboxProcessorWorker (polling)
3. ProviderPollerWorker (polling provider statuses)
4. ReconciliationWorker (polling for stale or inconsistent records)

Each worker has single responsibility. Must be idempotent and retry-safe.

---

# 6. Provider Integration

Define:

public interface IPaymentProvider
{
    Task<CreateDepositSessionResult> CreateDepositSessionAsync(CreateDepositSessionCommand cmd);
    Task<ProviderDepositResult> VerifyDepositAsync(string txHash);
    Task<ProviderWithdrawalResult> SendWithdrawalAsync(WithdrawalRequest req);
    Task<ProviderTransactionStatus> GetTransactionStatusAsync(string reference);
}

Implement providers in Infrastructure. Application only depends on interface.

Providers must perform:
- Address generation
- Deposit verification
- Withdrawal broadcasting
- Status polling

---

# 7. Deposit Flow

Implement exactly as documented:

1. FE calls POST /api/deposits/sessions with idempotencyKey.
2. Payments calls provider to create deposit address/QR.
3. Payments stores deposit_sessions row (OPEN).
4. Provider sends webhook → stored in inbox_events.
5. InboxProcessorWorker:
   - Verifies deposit via VerifyDepositAsync
   - Creates deposit_requests
   - Marks as CONFIRMED when verified
   - Emits payments.deposit.settled via outbox
6. OutboxPublisherWorker publishes the event.
7. Wallet applies DR HOUSE / CR MAIN.
8. Payments marks deposit as SETTLED and session as COMPLETED.

---

# 8. Withdrawal Flow

1. FE calls POST /api/withdrawals with idempotencyKey and secure cookie auth.
2. Payments creates withdrawal_requests with status REQUESTED.
3. Payments → Wallet via gRPC ReserveWithdrawal.
   - On success: status PROCESSING.
   - On failure: status FAILED.
4. Payments → Provider SendWithdrawalAsync.
   - Stores provider_reference and tx_hash.
   - Marks withdrawal as BROADCASTED.
   - Emits payments.withdrawal.broadcasted via outbox.
5. Provider webhook or ProviderPoller:
   - If CONFIRMED:
     - status SETTLED
     - emit payments.withdrawal.settled
   - If FAILED:
     - status FAILED
     - emit payments.withdrawal.failed
6. Wallet consumes event:
   - settled → DR WITHDRAW_HOLD / CR HOUSE (+ FEES)
   - failed → DR WITHDRAW_HOLD / CR MAIN

All steps are idempotent.

---

# 9. State Machines

Implement domain-level state machines:

Deposit Session:
- OPEN → COMPLETED
- OPEN → EXPIRED

Deposit Request:
- PENDING → CONFIRMED → SETTLED
- PENDING → FAILED
- CONFIRMED → FAILED

Withdrawal Request:
- REQUESTED → PROCESSING → BROADCASTED → SETTLED
- REQUESTED → FAILED
- PROCESSING → FAILED
- BROADCASTED → FAILED

Enforce valid transitions in domain layer.

---

# 10. API + Swagger

Define REST endpoints:

POST /api/deposits/sessions  
POST /api/withdrawals  
POST /api/providers/{providerId}/webhook  

Swagger/OpenAPI required.  
Controllers must call Application layer use case classes.  
Validation logic in Application layer (or fluent guards).  
Document request and response DTOs.  

Webhook endpoint should be lightweight (store events then return 200).

---

# 11. Idempotency

Payments must:

- Require idempotencyKey for deposit session creation and withdrawal requests.
- Store idempotency keys in idempotency_keys.
- Ensure same key + same source results in same outcome.
- Deduplicate provider webhooks using txHash or (source, idempotency_key).
- Use inbox_events uniqueness to avoid reprocessing MQ messages.

---

# 12. Minor Units

All monetary values stored as amount_minor (BIGINT).

Convert major to minor using decimals shared with wallet.  
Store amounts exactly as Payment → Wallet expects.

---

# 13. Folder Structure

Use:

Magenta.Payment.Domain  
Magenta.Payment.Application  
Magenta.Payment.Infrastructure  
Magenta.Payment.Api  
Magenta.Payment.Grpc

Domain: pure.  
Application: use cases.  
Infrastructure: actual DB, MQ, provider code.  
Api/Grpc: controllers calling Application.

---

# 14. Testing Requirements

Test:

- Deposit creation flow
- Deposit webhook processing
- Withdrawal REQUESTED → SETTLED/FAILED flow
- Outbox/Inbox correctness
- Provider polling behavior
- State machine transitions validity
- Idempotency under retries and duplicates

---

# 15. Final Rule

Use payments-documentation.md as the absolute truth.  
Apply SOLID and clean architecture.  
Do not alter business rules.
