# Wallet Tests Generation Prompt

You are an AI coding assistant working inside a .NET 9 solution that already contains the Wallet system described in `wallet-documentation.md` and implemented under the namespaces:

- Magenta.Wallet.Domain
- Magenta.Wallet.Application
- Magenta.Wallet.Infrastructure
- Magenta.Wallet.Api
- Magenta.Wallet.Grpc

Your task is to create a comprehensive test suite for the Wallet system, following these rules:

- Use xUnit for tests (no extra third-party libraries unless already present in the solution).
- Follow S.O.L.I.D principles and clean architecture in the test design:
  - Tests of Domain do not depend on Infrastructure.
  - Application tests mock Infrastructure dependencies via interfaces.
- Prefer unit tests; where appropriate, add a small number of integration tests around database and messaging.
- Do not change production code behavior; tests must reflect the business rules specified in `wallet-documentation.md`.

Create at least the following test projects:

- Magenta.Wallet.Domain.Tests
- Magenta.Wallet.Application.Tests
- Magenta.Wallet.Infrastructure.Tests
- Magenta.Wallet.Api.Tests (for controllers)
- Magenta.Wallet.Grpc.Tests (for gRPC handlers)

If the solution uses a different naming convention for test projects, align with it but keep separation between layers.

## 1. General Requirements

1. All tests must be deterministic and fast.
2. Use in-memory or fake implementations for:
   - Repositories
   - Message bus abstractions (RabbitMQ publisher/consumer)
   - Time providers
3. For any DB-related integration tests, use:
   - Either an in-memory provider or a test PostgreSQL instance configured for tests.
   - Explicit teardown between tests to avoid cross-test contamination.
4. The tests must cover:
   - Happy paths
   - Edge cases
   - Error conditions and validation failures
   - Idempotency behavior
   - Concurrency-critical conditions (as far as possible in unit tests)

Reference `wallet-documentation.md` as the source of truth for all flows and states.

---

## 2. Domain Tests (Magenta.Wallet.Domain.Tests)

Goal: Validate domain rules without infrastructure.

Create tests for:

### 2.1 Ledger Transactions and Postings

- Ensure that creating a ledger transaction requires:
  - At least two postings.
  - DR sum equals CR sum.
- Verify that:
  - Adding a posting with negative amount is rejected.
  - Missing account references are rejected (if domain enforces it).

Test cases:

- Create a valid transaction with:
  - DR Player:MAIN 100
  - CR HOUSE 100
- Attempt transaction with only one posting (should fail).
- Attempt transaction where DR total != CR total (should fail).

### 2.2 Account Entity and Account Types

- Validate that account types (MAIN, WITHDRAW_HOLD, HOUSE, HOUSE:WAGER, HOUSE:FEES) are correctly modeled.
- Ensure that domain prevents:
  - Invalid transitions (for example, using a non-player account where a player account is required).
- If there is a domain aggregate that represents a player’s wallet, test:
  - That initial state is correct.
  - That applying deposit, withdrawal reservation, bet, win, rollback changes balances in the expected way (only at domain level, not persistence).

### 2.3 Domain Services for Operations

For each operation described in `wallet-documentation.md`, implement tests that validate pure domain logic:

- ReserveWithdrawal:
  - Succeeds when MAIN balance >= amount.
  - Fails when MAIN balance < amount.
- ApplyDepositSettlement:
  - Credits MAIN and debits HOUSE.
- PostBet:
  - Debits MAIN, credits HOUSE:WAGER.
  - Fails when insufficient MAIN balance.
- PostWin:
  - Debits HOUSE:WAGER, credits MAIN.
  - Fails if there is no prior matching bet (if this rule is enforced in domain).
- RollbackTransaction:
  - Builds correct compensating postings for previous transactions.

These tests should use pure in-memory domain objects.

---

## 3. Application Tests (Magenta.Wallet.Application.Tests)

Goal: Validate application services and use cases, mocking persistence and messaging.

Mock (or fake) the following:

- Account and ledger repositories
- Idempotency repository
- Outbox event repository
- Any time or logging abstractions

Focus on:

### 3.1 Idempotency Behavior

For each command handler (ReserveWithdrawal, FinalizeWithdrawalSettled, FinalizeWithdrawalFailed, ApplyDepositSettlement, PostBet, PostWin, RollbackTransaction):

- Test that the first call with a given idempotency key:
  - Executes domain logic.
  - Persists transaction and postings.
  - Writes outbox events.
- Test that subsequent calls with the same idempotency key:
  - Do not execute domain logic again.
  - Return the same result (or a safe idempotent result).
  - Do not write duplicate ledger transactions.

### 3.2 ReserveWithdrawal Use Case

Tests:

- Success path:
  - MAIN >= amount.
  - Ledger postings: DR MAIN / CR WITHDRAW_HOLD.
  - Outbox event for wallet.withdrawal.reserved is created.
- Failure path:
  - MAIN < amount, operation fails with an appropriate error.
  - No ledger transaction created.
  - No outbox event written.

### 3.3 Deposit Settlement Use Case

Tests:

- Happy path:
  - ApplyDepositSettlement creates DR HOUSE / CR Player:MAIN.
  - Outbox event is created if specified (for example, wallet.balance.changed).
- Idempotent behavior:
  - Same txHash or reference ID is not applied twice.

### 3.4 Bet / Win / Rollback Use Cases

Bet:

- MAIN sufficient: DR MAIN / CR HOUSE:WAGER, outbox event created.
- MAIN insufficient: operation fails, no ledger created.

Win:

- Bet exists: DR HOUSE:WAGER / CR MAIN, idempotent on winId.
- Bet does not exist or invalid: error or no-op according to documentation.

Rollback:

- Builds correct compensating postings.
- Idempotent on rollbackId.
- If original transaction already rolled back, no double rollback.

### 3.5 Derived Balances Updates

If the application layer is responsible for updating `account_balances_derived`:

- Test that after each operation (deposit, withdrawal, bet, win, rollback):
  - The derived balance is updated exactly once.
  - Derived balances match the sum of postings.

---

## 4. Infrastructure Tests (Magenta.Wallet.Infrastructure.Tests)

Goal: Test integration with DB and messaging at a light level.

### 4.1 Repository Integration Tests

- Using a test DB (in-memory or PostgreSQL), ensure that:
  - LedgerTransactions and LedgerPostings are saved and loaded correctly.
  - Idempotency keys are persisted and uniqueness is enforced.
  - Account locks (FOR UPDATE) behave as expected (this can be simulated with concurrent tests where possible).

### 4.2 Outbox and Inbox Repository Tests

- Write and read outbox_events:
  - Insert event inside transaction.
  - Read pending events.
  - Mark events as sent.
- Inbox_events:
  - Insert events with (source, message_id).
  - Enforce uniqueness to avoid duplicates.

### 4.3 Background Worker Logic (Unit Level)

For OutboxDispatcherWorker and InboxProcessorWorker:

- Use fakes for repositories and RabbitMQ clients.
- Verify that:
  - Outbox worker reads pending events, publishes them, and marks them as sent.
  - Inbox worker reads unprocessed inbox events, calls handlers, and marks as processed.

You can test workers at logic level without actual RabbitMQ.

---

## 5. API Tests (Magenta.Wallet.Api.Tests)

Goal: Validate controller behavior as thin wrappers.

- Use the ASP.NET Core testing infrastructure (WebApplicationFactory or minimal test host).
- Mock Application layer services.
- Tests for:
  - GET /api/balances:
    - Authenticated user sees own balances.
    - Unauthorized user is rejected.
  - GET /api/currencies:
    - Returns the configured currency + network matrix.

Focus on:

- Correct HTTP status codes.
- Model binding.
- Correct mapping to application queries.
- Swagger generation can be verified by ensuring the OpenAPI document builds without errors (if desired).

---

## 6. gRPC Tests (Magenta.Wallet.Grpc.Tests)

Goal: Ensure gRPC handlers call Application layer correctly.

- Mock Application services for each of the operations:
  - ReserveWithdrawal
  - FinalizeWithdrawalSettled
  - FinalizeWithdrawalFailed
  - ApplyDepositSettlement
  - PostBet
  - PostWin
  - RollbackTransaction

Tests:

- gRPC request is validated and mapped to the correct application command.
- Application result is correctly translated into gRPC response.
- Error conditions (for example validation errors) are properly mapped to gRPC status codes or response messages.

You can use the gRPC test server utilities or directly instantiate handlers with mocked dependencies.

---

## 7. Concurrency and Edge Cases

Where feasible in unit/integration tests:

- Simulate two concurrent ReserveWithdrawal calls for the same player and ensure:
  - Only one succeeds.
  - The other fails due to insufficient funds.
- Simulate reprocessing of the same inbox event and verify it does not double-apply state.

---

## 8. Final Instruction

Do not change the Wallet production code behavior.  
Read and respect `wallet-documentation.md`.  
Generate all test classes, fixtures, fakes, and helpers needed to cover the scenarios above comprehensively.
