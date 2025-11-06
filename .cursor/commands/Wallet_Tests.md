# 🧪 Magenta Wallet System — Full Testing Prompt (.NET 9)

## 🧭 Context

You already have these projects in your solution:

- `Magenta.Domain`
- `Magenta.Application`
- `Magenta.Infrastructure`
- `Magenta.API`
- `Magenta.Grp`

You also have the **Wallet System documentation (Markdown)** that defines:
- Multi-network currency model
- Double-entry ledger (DR/CR rules)
- Derived balances (`account_balances`)
- Minor unit integer math
- Idempotency & Outbox/Inbox patterns
- RabbitMQ events
- Cookie-authenticated API

This prompt defines **everything the AI IDE must generate** to build a **complete, production-grade test suite** for the Wallet System.

## 🧱 Goal

Create automated tests verifying:

- DR/CR posting correctness
- Minor-unit math (no decimals)
- Derived balances updated atomically inside transactions
- Idempotency (exactly-once)
- Outbox publishing / Inbox dedupe
- Multi-network isolation
- SERIALIZABLE isolation safety
- API cookie security (no playerId in routes)
- Metadata JSON validation
- House vs Player balance equilibrium
- Reporting metrics (GGR, Fees)

Use **xUnit** for testing.  
Use **FluentAssertions** (optional, for readability).  
Use **Testcontainers** for Postgres and RabbitMQ in integration tests.  
No extra dependencies (no MediatR, no FluentValidation).

## 🧩 0. Test Projects

Create two test projects:

### 1️⃣ `Magenta.Tests.Unit`
- Target: `net9.0`
- References: `Magenta.Domain`, optional `Magenta.Application`
- For pure logic testing (PostingRules, Money conversions, DerivedBalanceUpdater, etc.)
- Packages: `xunit`, `FluentAssertions` *(optional)*

### 2️⃣ `Magenta.Tests.Integration`
- Target: `net9.0`
- References: **all** Magenta projects
- Uses real Postgres + RabbitMQ containers via Testcontainers
- Starts the app using HostBuilder, applies migrations, and runs live gRPC/API tests
- Packages:
  - `xunit`
  - `FluentAssertions`
  - `DotNet.Testcontainers`
  - `Grpc.Net.Client`

Provide a `README.md` inside each test project explaining how to run locally and in CI.

## 🧰 1. Shared Test Utilities

Create reusable helpers across both test projects:

### ⏰ `TestClock`
A clock abstraction that can be frozen, advanced, and injected.

### 🧩 `DeterministicGuidProvider`
Generates predictable GUIDs for deterministic tests (useful for tx_id, eventId).

### 💵 `Money` helpers
```csharp
public static long ToMinor(decimal major, int decimals);
public static decimal FromMinor(long minor, int decimals);
```
Used to assert conversions (no floating-point).

### ⚖️ `PostingAssert`
Given a `tx_id`, load `ledger_transactions` + `ledger_postings` and assert expected DR/CR pairs match exactly.

### 📊 `DbAsserts`
- Validate that `account_balances` updates atomically
- Ensure derived balances = Σ(CR−DR)
- Validate House+Player sums balance to zero per currency_network

### 📨 `EventProbe`
Connect to RabbitMQ and await specific routing keys (`wallet.balance.changed`, `wallet.withdrawal.reserved`) with timeouts.

## 🧪 2. Unit Tests — `Magenta.Tests.Unit`

Focus on **domain logic** (`Magenta.Domain`) and stateless services.

### 2.1 Minor Units Conversion
- ✅ `ToMinor_USDT6_BasicValues_AreExact`
- ✅ `ToMinor_BTC8_RoundsDown_Correctly`
- ✅ `FromMinor_ConvertsBack_ToDisplay`

### 2.2 Posting Rules Mapping

| Operation | Expected DR/CR Pair |
|------------|--------------------|
| Deposit settled | DR HOUSE → CR Player:MAIN |
| Withdrawal reserved | DR Player:MAIN → CR Player:WITHDRAW_HOLD |
| Withdrawal settled | DR Player:WITHDRAW_HOLD → CR HOUSE (net), CR HOUSE:FEES (fee) |
| Withdrawal failed | DR Player:WITHDRAW_HOLD → CR Player:MAIN |
| Bet | DR Player:MAIN → CR HOUSE:WAGER |
| Win | DR HOUSE:WAGER → CR Player:MAIN |
| Rollback bet | DR HOUSE:WAGER → CR Player:MAIN |
| Rollback win | DR Player:MAIN → CR HOUSE:WAGER |
| Standalone fee | DR Player:MAIN → CR HOUSE:FEES |

### 2.3 Derived Balance Updater
- ✅ `ApplyPostings_UpdatesBalanceMinor_Correctly`
- ✅ `RecomputeReservedAndCashable_PerPlayerCurrencyNetwork`
- ✅ `NoPostings_NoChange`

### 2.4 Idempotency
- ✅ `FirstCall_WritesKey_SecondCall_NoEffect`

### 2.5 Currency-Network Resolver
- ✅ `ResolveCurrencyNetwork_ValidPair_Succeeds`
- ✅ `ResolveCurrencyNetwork_Invalid_Fails`

### 2.6 Metadata Builder
- ✅ `BetMetadata_ContainsProviderBetRoundGame`
- ✅ `WithdrawalSettledMetadata_ContainsTxHashFeeNetwork`
- ✅ Metadata has **no secrets**.

## 🌐 3. Integration Tests — `Magenta.Tests.Integration`

### 3.0 Fixtures

| Fixture | Purpose |
|----------|----------|
| `PostgresContainerFixture` | Spins up Postgres, applies migrations |
| `RabbitMqContainerFixture` | Spins up RabbitMQ, declares exchanges |
| `WalletHostFixture` | Boots Magenta.Grp and background services (OutboxPublisher, PaymentsEventsConsumer) |

Seed data:
- Networks: TRON, ETHEREUM  
- Currencies: USDT (6 decimals), BTC (8 decimals)  
- CurrencyNetworks: USDT-TRON  
- House accounts: HOUSE, HOUSE:WAGER, HOUSE:FEES  
- account_balances initialized

### 3.1 Deposits
**Test:** `DepositSettled_CreditsPlayerMain_EmitsBalanceChanged_IsIdempotent`

Steps:
1. Publish `payments.deposit.settled`
2. Assert DR HOUSE / CR Player:MAIN
3. Assert `account_balances` MAIN += 1,000,000
4. Assert outbox `wallet.balance.changed`
5. Replay → idempotent

### 3.2 Withdrawals
- Reserve → Fail / Settled tests per spec

### 3.3 Bets / Wins / Rollbacks
- Bet → DR MAIN / CR HOUSE:WAGER  
- Win → DR HOUSE:WAGER / CR MAIN  
- Rollbacks reverse the original DR/CR

### 3.4 Multi-Network Isolation
Balances for different currency_networks must stay isolated.

### 3.5 Derived Balances Validation
Rebuild balances from ledger_postings and match `account_balances`.

### 3.6 Outbox/Inbox Reliability
Outbox published after commit; Inbox dedupes on reconsumption.

### 3.7 Concurrency (SERIALIZABLE)
Concurrent bets exceeding balance → one success, one failure.

### 3.8 API Security
`GET /api/balances` without cookie → 401; with cookie → only caller’s balances.

### 3.9 Metadata
All transactions include metadata (txHash, requestId, provider, etc.).

### 3.10 House vs Player Equilibrium
Σ(House) + Σ(Player) = 0 per currency_network.

### 3.11 Reporting
Validate GGR and fee reporting SQL views.

## 🧱 4. Folder Structure

```
Magenta.Tests.Unit/
Magenta.Tests.Integration/
```

## 🧾 5. Acceptance Criteria

✅ All tests pass via `dotnet test`.  
✅ Integration tests run automatically via Testcontainers.  
✅ ≥85% coverage.  
✅ Clear failure messages with operation ID & currency_network.

## 🚫 6. Guardrails

- ❌ No new frameworks  
- ✅ Only integers for money  
- ✅ Derived balances updated atomically  
- ✅ Idempotency enforced  
- ✅ Ledger remains balanced
