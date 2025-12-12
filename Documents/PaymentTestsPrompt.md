# Payments Tests Generation Prompt

You are an AI coding assistant working inside a .NET 9 solution that already contains the Payments system described in `payments-documentation.md` and implemented under the namespaces:

- Magenta.Payment.Domain
- Magenta.Payment.Application
- Magenta.Payment.Infrastructure
- Magenta.Payment.Api
- Magenta.Payment.Grpc

Your task is to create a comprehensive test suite for the Payments system, following these rules:

- Use xUnit for tests (no extra third-party libraries unless already present in the solution).
- Apply S.O.L.I.D principles and clean architecture:
  - Domain tests do not depend on Infrastructure.
  - Application tests use interfaces and mocks/fakes for infrastructure.
- Focus on correctness of:
  - State machines (deposit and withdrawal flows).
  - Idempotency.
  - Provider interaction.
  - Outbox and Inbox behavior.

Create at least these test projects:

- Magenta.Payment.Domain.Tests
- Magenta.Payment.Application.Tests
- Magenta.Payment.Infrastructure.Tests
- Magenta.Payment.Api.Tests

If the solution uses a different convention, adapt names but keep the separation by layer.

---

## 1. General Requirements

1. All tests must be deterministic and fast.
2. Use fakes/mocks for:
   - Wallet gRPC client
   - Payment provider (`IPaymentProvider`)
   - Outbox and Inbox repositories
   - Time provider
3. For any DB integration tests, use a test-safe database setup with proper isolation.
4. All business rules and flows must match `payments-documentation.md`.

---

## 2. Domain Tests (Magenta.Payment.Domain.Tests)

Goal: Validate state machines and core domain logic.

### 2.1 Deposit Session State Machine

- Test transitions:
  - OPEN → COMPLETED
  - OPEN → EXPIRED
- Verify invalid transitions are rejected, for example:
  - COMPLETED → EXPIRED
  - EXPIRED → COMPLETED

### 2.2 Deposit Request State Machine

- Valid transitions:
  - PENDING → CONFIRMED → SETTLED
  - PENDING → FAILED
  - CONFIRMED → FAILED
- Ensure:
  - Cannot go from FAILED back to CONFIRMED or SETTLED.
  - Cannot skip steps where documentation forbids it.

### 2.3 Withdrawal Request State Machine

- Valid transitions:
  - REQUESTED → PROCESSING → BROADCASTED → SETTLED
  - REQUESTED → FAILED
  - PROCESSING → FAILED
  - BROADCASTED → FAILED
- Test invalid transitions:
  - SETTLED → PROCESSING
  - FAILED → PROCESSING
  - REQUESTED → SETTLED (skipping PROCESSING/BROADCASTED if not allowed).

### 2.4 Domain Validation Rules

If domain entities enforce validations (amount > 0, valid address, etc.), test those as pure domain rules.

---

## 3. Application Tests (Magenta.Payment.Application.Tests)

Goal: Validate use cases orchestrating Wallet, Provider, Outbox, and DB.

Mock or fake:

- IPaymentProvider (one or multiple concrete providers).
- Wallet gRPC client/service.
- Repositories for deposit_sessions, deposit_requests, withdrawal_requests.
- Outbox and Inbox repositories.
- Idempotency repository.

Focus on:

### 3.1 Idempotency for FE Requests

For:

- Create Deposit Session (POST /api/deposits/sessions).
- Request Withdrawal (POST /api/withdrawals).

Tests:

- First request with a given idempotency key:
  - Creates the session or withdrawal.
  - Persists the correct state.
- Second request with the same idempotency key:
  - Does not create duplicates.
  - Returns a consistent response.

### 3.2 Create Deposit Session Use Case

Tests:

- Happy path:
  - Calls provider.CreateDepositSessionAsync.
  - Creates deposit_sessions row with status OPEN.
  - Returns correct data (address, QR, expiresAt).
- Validation failures:
  - Invalid currency/network handled properly.
- Idempotent behavior:
  - Same idempotencyKey does not create multiple sessions.

### 3.3 Handling Deposit Webhooks and Verification

Model tests as if the InboxProcessorWorker calls an application service that handles provider webhook events:

- When a deposit_detected event arrives:
  - Calls provider.VerifyDepositAsync(txHash).
  - Creates deposit_requests row with status PENDING or CONFIRMED.
- When verification confirms:
  - Status transitions to CONFIRMED.
  - Outbox event payments.deposit.settled is created with correct payload.
- Idempotency:
  - Same txHash or same message_id from provider does not create multiple deposit_requests or multiple events.

### 3.4 Withdrawal Request Use Case

Tests for POST /api/withdrawals orchestration logic:

- Happy path:
  - Creates withdrawal_requests with status REQUESTED.
  - Calls Wallet to ReserveWithdrawal.
  - On success, status → PROCESSING.
- ReserveWithdrawal failure:
  - Status → FAILED.
  - No provider call.

### 3.5 Withdrawal Provider Interaction

Simulate the application service that handles the step:

- SendWithdrawal:
  - Calls provider.SendWithdrawalAsync.
  - Saves provider_reference and tx_hash.
  - Status → BROADCASTED.
  - Outbox event payments.withdrawal.broadcasted is emitted.

- Confirmation handling (via webhook or poller):
  - If provider reports CONFIRMED:
    - Status → SETTLED.
    - Outbox event payments.withdrawal.settled is emitted.
  - If provider reports FAILED:
    - Status → FAILED.
    - Outbox event payments.withdrawal.failed is emitted.

Ensure idempotent behavior:
- Same webhook or poll event for the same provider_reference does not double-settle or double-fail.

---

## 4. Infrastructure Tests (Magenta.Payment.Infrastructure.Tests)

Goal: Validate repository behavior, Outbox/Inbox mechanics, and worker logic.

### 4.1 Repositories for Sessions and Requests

- Insert and retrieve deposit_sessions, deposit_requests, withdrawal_requests.
- Ensure status updates are correctly persisted.
- Ensure tx_hash uniqueness is enforced (if documented).

### 4.2 Outbox and Inbox Repositories

- Insert outbox events inside a simulated transaction.
- Retrieve pending outbox events.
- Mark events as SENT.
- Insert inbox events with (source, message_id).
- Verify uniqueness constraint prevents duplicates.

### 4.3 Background Workers Logic

Using fake repositories and fake RabbitMQ client:

- OutboxPublisherWorker:
  - When pending events exist, publishes them and marks as SENT.
  - Handles empty queue gracefully.
- InboxProcessorWorker:
  - Picks unprocessed inbox events.
  - Calls application handlers.
  - Marks as processed.

You do not need a real RabbitMQ instance; test the logic via interfaces.

---

## 5. API Tests (Magenta.Payment.Api.Tests)

Goal: Test that the HTTP endpoints behave correctly and delegate to Application.

Use ASP.NET Core test host or WebApplicationFactory. Mock application services behind controllers.

### 5.1 POST /api/deposits/sessions

Tests:

- Valid request:
  - Returns 200/201 with session info.
  - Application service is invoked with correct DTO and idempotencyKey.
- Invalid request:
  - Missing currency/network or invalid values lead to 400.
- Idempotency behavior:
  - Same idempotencyKey does not create multiple sessions (verify via mocked service).

### 5.2 POST /api/withdrawals

Tests:

- Authenticated player:
  - Valid amount and address accepted.
  - Application service is invoked.
- Unauthenticated player:
  - Request is rejected (401/403).
- Bad input:
  - Negative amounts or invalid address rejected with 400.
- Idempotency:
  - Same idempotencyKey leads to one logical withdrawal in the application service.

### 5.3 POST /api/providers/{providerId}/webhook

Tests:

- Valid webhook payload:
  - An application service is invoked to record the event into inbox_events.
- Idempotent behavior:
  - Duplicate webhook with same provider event ID does not cause duplicate events (via application/idempotency logic).
- Webhook returns appropriate HTTP status (e.g., 200) after recording.

---

## 6. Provider Integration Tests (Optional)

If there is a mock or sandbox provider implementation:

- Verify that the mock provider:
  - Maps create session calls correctly.
  - Validates txHash and returns expected test responses.
  - Handles withdrawal send and status polling for test environments.

These tests can remain purely unit-level, using in-memory data.

---

## 7. Idempotency Tests

Add focused tests for:

- CreateDepositSession with duplicate idempotencyKey.
- RequestWithdrawal with duplicate idempotencyKey.
- Provider webhook re-sends (same txHash or message_id).
- Inbox event re-processing attempts.

All these must result in:
- No duplicate rows.
- Consistent state.

---

## 8. Reconciliation and Poller Tests

For the ProviderPollerWorker and ReconciliationWorker:

- Use fakes for repositories and providers.
- Simulate deposits in PENDING state; provider reports CONFIRMED:
  - Worker updates status and triggers events via outbox.
- Simulate withdrawals in BROADCASTED state where provider reports FAILED or CONFIRMED:
  - Worker updates status and triggers appropriate events.
- Simulate stuck records (older than threshold):
  - Reconciliation marks them as failed or flagged according to documentation.

Tests should assert:
- Correct status transitions.
- Correct outbox events created.

---

## 9. Concurrency and Edge Cases

As far as unit/integration tests allow:

- Simulate rapid double-submission of withdrawal from FE with same idempotencyKey.
- Simulate rapid duplicate webhooks from provider for same txHash.
- Ensure the system behaves idempotently and the state remains correct.

---

## 10. Final Instruction

Do not modify Payments production code behavior based on tests.  
Use `payments-documentation.md` as the source of truth for all flows and rules.  
Generate a full test suite that validates deposit, withdrawal, provider interaction, outbox/inbox, idempotency, and state machines exactly as described.
