# 🧱 Wallet System — Technical Documentation  
### (.NET 9 • PostgreSQL • RabbitMQ • gRPC • REST • Internal Ledger Architecture)

---

# 🧭 1. Purpose & Scope

The **Wallet System** is the financial core of the platform.  
It is responsible for:

- Managing **all player balances**
- Executing **all monetary transactions** internally
- Guaranteeing **ledger accuracy**
- Enforcing **race-condition safety**
- Processing **game transactions** (bet, win, rollback)
- Integrating with **Payments System** for deposits & withdrawals
- Providing **read-only APIs** for FE
- Exposing **gRPC** for internal services (Payments, Game Gateway)

The Wallet system is the **single source of truth** for:

- Balances  
- Account states  
- Ledger transactions  
- All monetary flows  

Wallet **never talks to external payment providers** — that is handled by Payments System.

---

# 🧩 2. High-Level Architecture

```mermaid
flowchart LR
  FE[Frontend] -.->|HTTP (read-only)| WAL_API[Wallet.API]

  subgraph Internal Systems
    PAY[Payments System]
    GAME[Game Gateway]
    WAL[Wallet.Grpc Service]
  end

  FE -.-> WAL_API
  PAY -->|gRPC| WAL
  GAME -->|gRPC| WAL

  WAL --> DB[(PostgreSQL)]
  WAL --> MQ[RabbitMQ]
  MQ --> WAL
```

### Key principles

- **Wallet.API** = **READ-ONLY** (balances, history, configuration).
- **Wallet.Grpc** = **MUTATIONS** (bets, wins, deposits, withdrawals).
- **Payments System** handles deposit/withdrawal orchestration.
- **Wallet** handles only **balance changes**.
- **Ledger-based**: every balance change = immutable posting.
- **SERIALIZABLE** isolation + row-level locking prevents race conditions in multi-pod environments.

---

# 🧱 3. Core Concepts

## 🧾 3.1 Ledger Architecture

Wallet uses **double-entry accounting**:

- Every movement has **two postings**:
  - **DR** (debit → negative)
  - **CR** (credit → positive)
- The sum of all accounts is always **zero** (closed system).
- No balance fields exist — balance is derived by summing postings.

---

## 🧩 3.2 Accounts

Each player has **multiple internal accounts**.  
These accounts are purely ledger constructs.

### Player accounts:

| Account Type | Purpose |
|--------------|---------|
| **Player:MAIN** | Main spendable balance. |
| **Player:WITHDRAW_HOLD** | Funds locked during withdrawal processing. |
| **Player:BONUS** | Future bonus/promo funds. |

### House accounts:

| Account | Purpose |
|---------|---------|
| **HOUSE** | Tracks casino funds. |
| **HOUSE:WAGER** | Intermediate account for bets and wins. |
| **HOUSE:FEES** | Accumulates withdrawal fees. |

---

## 🧮 3.3 Minor Units (Mandatory)

All amounts are stored as **integers**, in the smallest divisible unit of a currency.

### Examples

| Currency | Decimals | Major | Minor |
|----------|----------|--------|--------|
| USDT | 6 | 1.25 | 1_250_000 |
| BTC | 8 | 0.001 | 100_000 |
| ETH | 18 | 0.01 | 10_000_000_000_000_000 |

### Why?

- No rounding errors  
- Exact blockchain matching  
- Consistent accounting  
- Works across all currencies and networks  

Decimals come from the `currency_networks` table.

---

# 🌐 4. External Interfaces

## 🌐 4.1 Wallet.API (HTTP — Frontend)

**Wallet.API is READ-ONLY.**  
Frontend **never modifies balances directly**.

### Authentication
Player is identified **via secure cookie**.  
NO endpoints accept `playerId` as input.

### Endpoints

#### GET /api/wallet/balances
Returns balances for the authenticated player.

#### GET /api/wallet/transactions
Returns ledger history.

#### GET /api/wallet/currencies
Returns currency + network + decimals.

All responses use **minor units**.

---

## 🔌 4.2 Wallet.Grpc (Internal Mutations)

Called by:

- Payments System  
- Game Gateway  

### Methods

```
rpc ReserveWithdrawal (ReserveWithdrawalRequest) returns (OperationResult);
rpc ApplyDepositSettlement (DepositSettlementRequest) returns (OperationResult);

rpc Bet (BetRequest) returns (OperationResult);
rpc Win (WinRequest) returns (OperationResult);
rpc Rollback (RollbackRequest) returns (OperationResult);
```

Each runs in a single SERIALIZABLE DB transaction.

---

# 🧱 5. Database Schema

## 🧱 5.1 accounts

```sql
CREATE TABLE accounts (
  account_id BIGSERIAL PRIMARY KEY,
  player_id BIGINT,                  -- null for house accounts
  account_type TEXT NOT NULL,        -- MAIN, WITHDRAW_HOLD, HOUSE, HOUSE:WAGER, HOUSE:FEES
  currency_network_id INT NOT NULL REFERENCES currency_networks(currency_network_id),
  UNIQUE(player_id, currency_network_id, account_type)
);
```

---

## 🧱 5.2 ledger_transactions

```sql
CREATE TABLE ledger_transactions (
  tx_id UUID PRIMARY KEY,
  source TEXT NOT NULL,                   -- e.g., "payments", "game"
  reference TEXT NOT NULL,                -- idempotency key
  metadata JSONB DEFAULT '{}',
  created_at TIMESTAMPTZ DEFAULT now()
);
```

---

## 🧱 5.3 ledger_entries

```sql
CREATE TABLE ledger_entries (
  entry_id BIGSERIAL PRIMARY KEY,
  tx_id UUID NOT NULL REFERENCES ledger_transactions(tx_id),
  account_id BIGINT NOT NULL REFERENCES accounts(account_id),
  amount_minor BIGINT NOT NULL,           -- positive = CR, negative = DR
  metadata JSONB DEFAULT '{}'
);
```

---

## 🧱 5.4 currency_networks

```sql
CREATE TABLE currency_networks (
  currency_network_id SERIAL PRIMARY KEY,
  currency TEXT NOT NULL,
  network TEXT NOT NULL,
  decimals INT NOT NULL,
  UNIQUE(currency, network)
);
```

---

## 🧱 5.5 idempotency_keys

```sql
CREATE TABLE idempotency_keys (
  source TEXT,
  idempotency_key TEXT,
  tx_id UUID,
  created_at TIMESTAMPTZ DEFAULT now(),
  PRIMARY KEY (source, idempotency_key)
);
```

---

## 🧱 5.6 outbox_events / inbox_events

```sql
CREATE TABLE outbox_events ( ... );
CREATE TABLE inbox_events ( ... );
```

Used for reliable event delivery.

---

# 🔄 6. Event Handling & Ledger Posting Rules

## 💰 6.1 Deposit Settlement  
**Triggered by Payments → Wallet**  
Event: `payments.deposit.settled`

| Debit | Credit |
|--------|--------|
| **HOUSE** | **Player:MAIN** |

---

## 💸 6.2 Withdrawal Reservation  
**Triggered by Payments.gRPC**

| Debit | Credit |
|--------|--------|
| **Player:MAIN** | **Player:WITHDRAW_HOLD** |

---

## 💸 6.3 Withdrawal Settlement  
**Triggered by Payments → Wallet**

| Debit | Credit |
|--------|--------|
| **Player:WITHDRAW_HOLD** | **HOUSE** |
| — | **HOUSE:FEES** (optional) |

---

## ⚠️ 6.4 Withdrawal Failed  
**Triggered by Payments → Wallet**

| Debit | Credit |
|--------|--------|
| **Player:WITHDRAW_HOLD** | **Player:MAIN** |

---

## 🎮 6.5 Game Bet

| Debit | Credit |
|--------|--------|
| **Player:MAIN** | **HOUSE:WAGER** |

---

## 🎰 6.6 Game Win

| Debit | Credit |
|--------|--------|
| **HOUSE:WAGER** | **Player:MAIN** |

---

## 🔄 6.7 Game Rollback

- Must reverse previous postings for the referenced transaction.
- Must ensure idempotency.

---

# 🏗 7. Safety & Concurrency

Wallet ensures correctness with:

### ✔ SERIALIZABLE transactions  
### ✔ Row locking (`FOR UPDATE`)  
### ✔ Idempotency keys  
### ✔ One mutation = one DB transaction  
### ✔ Multi-pod safe design  
### ✔ Atomic posting creation  

Wallet is compatible with Kubernetes autoscaling.

---

# 💾 8. Derived Balances

Balances = SUM of ledger_entries.

For performance:

- A materialized view **or**
- A table updated by triggers / workers

is used for Wallet.API.

**Derived balances are not source of truth**, ledger is.

---

# 🌐 9. Wallet.API Endpoints

### GET /api/wallet/balances  
Returns balances for authenticated user (via cookie).

### GET /api/wallet/transactions  
Returns ledger history.

### GET /api/wallet/currencies  
Returns supported currency & network pairs.

---

# 🔌 10. Wallet.Grpc Methods

### ReserveWithdrawal  
Moves MAIN → WITHDRAW_HOLD.

### ApplyDepositSettlement  
Moves HOUSE → MAIN.

### Bet  
Moves MAIN → HOUSE:WAGER.

### Win  
Moves HOUSE:WAGER → MAIN.

### Rollback  
Reverses a prior bet/win.

---

# 📨 11. Events Consumed by Wallet

- `payments.deposit.settled`
- `payments.withdrawal.broadcasted`
- `payments.withdrawal.settled`
- `payments.withdrawal.failed`

---

# 🌱 12. Metadata

Metadata is JSONB stored in:

- ledger_transactions  
- ledger_entries  

Used for:

- txHash  
- provider ID  
- game round ID  
- rollback references  

Not used for logic decisions.

---

# 📖 13. Glossary

| Term | Meaning |
|------|---------|
| **DR** | Debit (negative) |
| **CR** | Credit (positive) |
| **Minor Units** | Integer representation of currency |
| **Posting** | DR or CR entry |
| **Transaction** | Group of postings |
| **Derived Balance** | Cached performance table |
| **Ledger** | Immutable accounting system |

---

# 🟢 14. Summary

- Wallet is **balance authority**
- Ledger-based, double-entry
- gRPC for mutations, HTTP for reads
- Minor units everywhere
- Payments orchestrates real-world money
- Wallet processes only internal balance changes
- Safe under concurrency and scaling
- Full auditability & correctness

---

# 🧱 END OF WALLET SYSTEM DOCUMENTATION
