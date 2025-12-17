# WALLET SYSTEM — GENERATION PROMPT

Use namespaces:

- Magenta.Wallet.Domain
- Magenta.Wallet.Application
- Magenta.Wallet.Infrastructure
- Magenta.Wallet.Api
- Magenta.Wallet.Grpc

Build the Wallet System described in wallet-documentation.md following these strict requirements. Apply S.O.L.I.D principles and clean architecture separation at all times.

---

# 1. Core Architecture Requirements

The Wallet system must:

- Act as the single source of truth for all balances and money movements.
- Implement a strict double-entry ledger.
- Store all monetary values as integers (minor units).
- Use SERIALIZABLE isolation for all financial operations.
- Lock accounts using SELECT ... FOR UPDATE.
- Be fully safe in multi-pod Kubernetes environments.
- Implement Outbox (DB + polling worker), Inbox (MQ listener + polling worker), Idempotency, and Derived balances.
- Publish and consume RabbitMQ events.
- Expose:
  - gRPC API for all write operations (internal calls).
  - REST API for FE read-only operations.
  - Swagger/OpenAPI for REST API.

Wallet is the only service allowed to change money.

---

# 2. Double-Entry Ledger Model

Implement:

## 2.1 ledger_transactions table
Fields:
- ledger_transaction_id
- created_at
- reference_type (Deposit, Withdrawal, Bet, Win, Rollback)
- reference_id

## 2.2 ledger_postings table
Fields:
- ledger_posting_id
- ledger_transaction_id (FK)
- account_id (FK)
- direction (DR or CR)
- amount_minor (BIGINT)

Rules:
- Every transaction has at least 2 postings.
- SUM(DR) == SUM(CR).
- Ledger logic implemented in domain layer.
- Apply SOLID: Ledger domain service should have single responsibility; repositories abstract persistence.

---

# 3. Accounts and Currency Model

Accounts required:

Player:
- MAIN
- WITHDRAW_HOLD

House/System:
- HOUSE
- HOUSE:WAGER
- HOUSE:FEES

Tables:
- accounts (UNIQUE per player + currency_network_id + account_type)
- currency_catalog (contains decimals, symbol)
- currency_networks (currency + network mapping)
- account_balances_derived (for FE fast reads)

Minor units:
amount_minor = amount_major * (10^decimals)

---

# 4. Event System (Outbox and Inbox)

## 4.1 Outbox (DB + POLLING worker)
- Insert outbox event inside same transaction as ledger change.
- OutboxPublisherWorker:
  - Polls outbox_events WHERE status='PENDING'.
  - Publishes to RabbitMQ.
  - Marks event as SENT or retries on failure.
- Follow SOLID: separate publisher, serializer, worker responsibilities.

## 4.2 Inbox (MQ listener + POLLING worker)
- MQ listener writes all incoming messages to inbox_events and ACKs only after DB write.
- InboxProcessorWorker polls inbox_events where processed_at IS NULL.
- Dispatch to appropriate handler based on event type.
- Mark processed.
- UNIQUE(source, idempotency_key/message_id).

Use interfaces for handlers to respect Dependency Inversion.

---

# 5. Background Workers

All workers must be HostedServices with single responsibility:

1. OutboxDispatcherWorker (polling)
2. InboxProcessorWorker (polling)
3. WithdrawalReconciliationWorker (polling)
4. DepositReconciliationWorker (polling)
5. GameTransactionReconciliationWorker (polling)

All must be idempotent and retry-safe.

---

# 6. gRPC Endpoints (Magenta.Wallet.Grpc)

Implement the following internal commands:

## ReserveWithdrawal
- Validate player has sufficient balance in MAIN.
- Lock MAIN and WITHDRAW_HOLD accounts.
- DR MAIN / CR WITHDRAW_HOLD.
- Insert idempotency key.
- Emit outbox event wallet.withdrawal.reserved.

## FinalizeWithdrawalSettled
- DR WITHDRAW_HOLD / CR HOUSE (+ HOUSE:FEES if applicable).
- Idempotent.

## FinalizeWithdrawalFailed
- DR WITHDRAW_HOLD / CR MAIN.
- Idempotent.

## ApplyDepositSettlement
- DR HOUSE / CR MAIN.
- Idempotent per txHash.

## PostBet
- DR MAIN / CR HOUSE:WAGER.
- Idempotent per betId.

## PostWin
- DR HOUSE:WAGER / CR MAIN.
- Idempotent per winId.

## RollbackTransaction
- Generate compensating postings.
- Idempotent per rollbackId.

All gRPC handlers must call Application layer services, no business logic in handlers.

---

# 7. REST API (Magenta.Wallet.Api) with Swagger

Expose FE read-only endpoints:

## GET /api/balances
- Auth using secure cookie.
- Returns derived balances per currency_network.

## GET /api/currencies
- Returns list of currencies and networks.

Enable Swagger/OpenAPI for all endpoints. Controllers must be thin, calling Application layer.

---

# 8. Concurrency and Idempotency Rules

- All write operations use SERIALIZABLE.
- Lock rows with SELECT ... FOR UPDATE.
- Use idempotency_keys table for dedupe.
- Bundle ledger operations, account updates, and outbox inserts into the same DB transaction.
- Wallet must work correctly under multiple pods, retries, duplicate MQ messages, provider resends, and crashes.

---

# 9. Minor Units

All amounts must be integers. Use decimals from currency_catalog for conversion.

---

# 10. Folder Structure

Use:

Magenta.Wallet.Domain
Magenta.Wallet.Application
Magenta.Wallet.Infrastructure
Magenta.Wallet.Api
Magenta.Wallet.Grpc

Domain should have no dependencies. Application depends only on Domain. Infrastructure implements interfaces. API/Grpc call Application.

---

# 11. Testing Requirements

Test:

- Ledger correctness (DR/CR equality).
- Reserve/finalize withdrawal correctness.
- Deposit settlement correctness.
- Bet/win/rollback correctness.
- Idempotency for all operations.
- Derived balances correctness.
- Outbox/Inbox behavior under repeated events.
- Multi-pod concurrency handling.

---

# 12. Final Rule

Use wallet-documentation.md as the absolute truth. Apply SOLID and clean architecture. Never modify business rules.
