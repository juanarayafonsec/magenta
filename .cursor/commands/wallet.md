Context
You have the following .NET 9 projects already created in one solution:

Magenta.Wallet.Domain
Magenta.Wallet.Application

Magenta.Wallet.Infrastructure

Magenta.Wallet.API (HTTP for FE, cookie-auth)

Magenta.Wallet.Grp (internal gRPC for commands; keep the project name exactly)


You are also given a Wallet System documentation WalletDocumentation.md (attached) that specifies:

Multi-network currency model (currencies, networks, currency_networks)

Double-entry ledger with DR/CR rules for Deposit, Withdrawal (reserve/settle/fail), Bet, Win, Rollback, Fee

Minor units (integers)

Derived balances table (account_balances) and update rules

Idempotency + Outbox/Inbox patterns

RabbitMQ event contracts

Cookie-auth read API (no playerId in URL)

Diagrams (Mermaid, ERD) and SQL DDL

Goal
Implement the complete Wallet system inside the existing projects, strictly following the WalletDocumentation.md. Do not rename projects. Do not add unnecessary libraries (no MediatR, no FluentValidation). Use only: .NET 9, EF Core, Npgsql, RabbitMQ.Client, Grpc.AspNetCore, Serilog.

1) Cross-cutting constraints (apply everywhere)

Money: use long for all minor-unit amounts; no decimals/floats in persistence or domain.

DR/CR is authoritative: every balance-changing operation must create a ledger_transaction with balanced ledger_postings (sum DR == sum CR).

Derived balances: update account_balances inside the same DB transaction that inserts postings; recompute reserved_minor and cashable_minor as specified in the md.

Isolation: use SERIALIZABLE; lock affected account_balances rows with FOR UPDATE.

Idempotency: enforce with idempotency_keys (source, idempotency_key) inside the same transaction.

Outbox/Inbox: publish only from outbox after commit; consume with inbox dedupe.

Multi-network: every operation resolves currency_network_id from (currency, network); accounts are unique on (player_id, currency_network_id, account_type).

API auth: cookie identifies the caller; do not accept playerId in API routes.

Events: publish wallet.balance.changed after every balance change; publish wallet.withdrawal.reserved on reservation; consume all payments.* events.

2) Project work breakdown
2.1 Magenta.Domain (pure domain)

Create domain models and value objects:

Money/Amount (minor units long, helper for add/subtract; no floats).

Identifiers & enums: TxType (DEPOSIT, WITHDRAW_RESERVE, WITHDRAW_FINALIZE, WITHDRAW_RELEASE, BET, WIN, ROLLBACK, FEE), Direction (DEBIT,CREDIT), AccountType (MAIN, WITHDRAW_HOLD, BONUS, HOUSE, HOUSE:WAGER, HOUSE:FEES).

Entities/Aggregates (plain classes): Account, LedgerTransaction, LedgerPosting.

Services (domain logic):

PostingRules → maps a business command/event to the exact DR/CR pairs per md.

DerivedBalanceUpdater → applies postings to account_balances and recomputes reserved_minor, cashable_minor.

IdempotencyService → check/record idempotency keys.

CurrencyNetworkResolver.

No infrastructure dependencies here.

2.2 Magenta.Infrastructure (data, migrations, bus, workers)

Persistence (With EF Core):

Implement the exact SQL schema from the WalletDocumentation.md:

networks, currencies, currency_networks

accounts (UNIQUE (player_id, currency_network_id, account_type))

ledger_transactions, ledger_postings

account_balances

idempotency_keys, outbox_events, inbox_events

View v_player_currency_balances

Provide migrations to create all tables, indexes, constraints, and the view.

Provide seed for: networks (TRON, ETHEREUM), currencies (USDT with 6, BTC with 8), at least one currency_networks row (e.g., USDT-TRON), and House accounts (HOUSE, HOUSE:WAGER, HOUSE:FEES) per currency_network, with account_balances rows.

Repositories / Data services (interfaces in Application, impl here):

ILedgerWriter → begin/commit tx, insert ledger_transactions + ledger_postings, update account_balances, write idempotency_keys, write outbox_events.

IAccountReadModel → read balances from account_balances / v_player_currency_balances.

IInboxStore, IOutboxStore.

RabbitMQ

Connection factory config; declare exchanges: wallet.events, payments.events (topic).

OutboxPublisher BackgroundService: reads unpublished outbox rows, publishes, sets published_at.

PaymentsEventsConsumer BackgroundService: consumes:

payments.deposit.settled

payments.withdrawal.broadcasted

payments.withdrawal.settled

payments.withdrawal.failed
For each message: insert into inbox_events (dedupe), call Application command handler, ack.

Configuration (appsettings / env):

ConnectionStrings:WalletDb

Rabbit:Uri, Rabbit:WalletExchange, Rabbit:PaymentsExchange, Rabbit:Queue names

Serilog minimal JSON console.


2.3 Magenta.Application (use cases, orchestration)

Define request/response DTOs and handlers for each command / event:

Commands (gRPC-facing):

ApplyDepositSettlementCommand

ReserveWithdrawalCommand

FinalizeWithdrawalCommand

ReleaseWithdrawalCommand

PlaceBetCommand

SettleWinCommand

RollbackCommand

GetBalanceQuery (reads only)

Event handlers (MQ-facing):

OnPaymentsDepositSettled → maps payload to ApplyDepositSettlementCommand

OnPaymentsWithdrawalBroadcasted → (info only, no postings)

OnPaymentsWithdrawalSettled → FinalizeWithdrawalCommand

OnPaymentsWithdrawalFailed → ReleaseWithdrawalCommand

Handler behavior (for every balance-changing handler):

Resolve currency_network_id from (currency, network).

Ensure required accounts exist for (player, currency_network) and House accounts for that currency_network; ensure account_balances rows exist (create lazily).

Idempotency check (source, idempotency_key).

Begin SERIALIZABLE transaction.

Insert ledger_transactions with tx_type, external_ref, and metadata JSON from the WalletDocumentation.md.

Insert all required DR/CR postings (use PostingRules).

Update derived balances for affected accounts (DerivedBalanceUpdater).

Insert outbox_events (always wallet.balance.changed; plus wallet.withdrawal.reserved when applicable).

Insert idempotency_keys.

Commit.

PostingRules mapping (must match WalletDocumentation.md exactly):

Deposit settled → DR HOUSE; CR Player:MAIN

Withdrawal reserved → DR Player:MAIN; CR Player:WITHDRAW_HOLD

Withdrawal broadcasted → (no postings)

Withdrawal settled → DR Player:WITHDRAW_HOLD; CR HOUSE (net), CR HOUSE:FEES (fee)

Withdrawal failed → DR Player:WITHDRAW_HOLD; CR Player:MAIN

Bet → DR Player:MAIN; CR HOUSE:WAGER

Win → DR HOUSE:WAGER; CR Player:MAIN

Rollback bet → DR HOUSE:WAGER; CR Player:MAIN

Rollback win → DR Player:MAIN; CR HOUSE:WAGER

Standalone fee → DR Player:MAIN; CR HOUSE:FEES


2.4 Magenta.Grp (internal gRPC)

Create wallet.proto (keep project name Magenta.Grp):

syntax = "proto3";
option csharp_namespace = "Magenta.Grp";

service WalletService {
  rpc ApplyDepositSettlement (ApplyDepositSettlementRequest) returns (OperationResult);
  rpc ReserveWithdrawal     (ReserveWithdrawalRequest)     returns (OperationResult);
  rpc FinalizeWithdrawal    (FinalizeWithdrawalRequest)    returns (OperationResult);
  rpc ReleaseWithdrawal     (ReleaseWithdrawalRequest)     returns (OperationResult);
  rpc PlaceBet              (PlaceBetRequest)              returns (OperationResult);
  rpc SettleWin             (SettleWinRequest)             returns (OperationResult);
  rpc Rollback              (RollbackRequest)              returns (OperationResult);
  rpc GetBalance            (GetBalanceRequest)            returns (GetBalanceResponse);
}

message OperationResult { bool ok = 1; string message = 2; }

message ApplyDepositSettlementRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  int64 amountMinor = 4;
  string txHash = 5;
  string idempotencyKey = 6;
  string correlationId = 7;
}

message ReserveWithdrawalRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  int64 amountMinor = 4;
  string requestId = 5;   // idempotency key
  string correlationId = 6;
}

message FinalizeWithdrawalRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  int64 amountMinor = 4;
  int64 feeMinor = 5;
  string requestId = 6;   // idempotency key
  string txHash = 7;
  string correlationId = 8;
}

message ReleaseWithdrawalRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  int64 amountMinor = 4;
  string requestId = 5;   // idempotency key
  string correlationId = 6;
}

message PlaceBetRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  int64 amountMinor = 4;
  string betId = 5;       // idempotency key
  string provider = 6;
  string roundId = 7;
  string gameCode = 8;
  string correlationId = 9;
}

message SettleWinRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  int64 amountMinor = 4;
  string winId = 5;       // idempotency key
  string betId = 6;
  string roundId = 7;
  string provider = 8;
  string correlationId = 9;
}

message RollbackRequest {
  int64 playerId = 1;
  string currency = 2;
  string network = 3;
  string referenceType = 4; // "BET" | "WIN"
  string referenceId = 5;   // betId or winId
  string rollbackId = 6;    // idempotency key
  string reason = 7;
  string correlationId = 8;
}

message GetBalanceRequest {
  int64 playerId = 1;
}

message BalanceItem {
  string currency = 1;
  string network = 2;
  int64 balanceMinor = 3;
  int64 reservedMinor = 4;
  int64 cashableMinor = 5;
}

message GetBalanceResponse {
  repeated BalanceItem items = 1;
}

Implement the service by delegating to Magenta.Application handlers.

Propagate correlationId into logs and outbox events’ payloads.


2.5 Magenta.API (HTTP, cookie-auth, read-only)

Endpoints (no playerId in URL; identity from cookie):

* GET /api/currencies → list active currency-network pairs with decimals.

* GET /api/balances → balances for authenticated player from v_player_currency_balances.
Configure auth to use the existing secure cookie from your Identity service.

3) Events (RabbitMQ contracts)

Consume (payments.events):

payments.deposit.settled { eventId, occurredAt, playerId, currency, network, amountMinor, txHash, idempotencyKey }

payments.withdrawal.broadcasted { eventId, occurredAt, requestId, playerId, currency, network, amountMinor, txHash } (no postings)

payments.withdrawal.settled { eventId, occurredAt, requestId, playerId, currency, network, amountMinor, feeMinor, txHash, idempotencyKey }

payments.withdrawal.failed { eventId, occurredAt, requestId, playerId, currency, network, amountMinor, reason, idempotencyKey }

Publish (wallet.events):

wallet.withdrawal.reserved { eventId, occurredAt, requestId, playerId, currency, network, amountMinor }

wallet.balance.changed { eventId, occurredAt, playerId, changes:[{ currency, network, balanceMinor, cashableMinor, reservedMinor }] }

All events carry correlationId if provided.

4) Metadata (store in ledger_transactions.metadata)

Populate per operation as in the WalletDocumentation.md (non-financial, audit context only), e.g.:

Deposit settled: { "source":"payments.deposit.settled","txHash":"...","network":"TRON","confirmations":12,"depositAddress":"..." }

Withdrawal settled: { "source":"payments.withdrawal.settled","requestId":"...","txHash":"...","feeMinor":12345,"network":"TRON" }

Bet: { "provider":"...","betId":"...","roundId":"...","gameCode":"..." }

Win: { "provider":"...","winId":"...","roundId":"...","betId":"..." }

Rollback: { "referenceType":"BET|WIN","referenceId":"...","reason":"..." }

Never store secrets or private keys.

5) Configuration & DI

Register: Npgsql connection, repositories, Application handlers, OutboxPublisher, PaymentsEventsConsumer.

Health checks for DB & RabbitMQ.

Serilog JSON console with CorrelationId enricher.

appsettings.json keys:

{
  "ConnectionStrings": { "WalletDb": "Host=...;Database=...;Username=...;Password=..." },
  "Rabbit": {
    "Uri": "amqp://user:pass@host:5672/vhost",
    "WalletExchange": "wallet.events",
    "PaymentsExchange": "payments.events",
    "WalletQueue": "wallet.consumer"
  }
}

6) Acceptance criteria (done = true only if all pass)

All tables, indexes, and view match the WalletDocumentation.md.

Every command/event leads to correct DR/CR postings and derived balances updated in the same TX.

Idempotency: re-sending the same idempotency key causes no duplicate postings.

Outbox publishes wallet.balance.changed after each balance mutation; wallet.withdrawal.reserved when reserving.

API requires cookie and returns only the caller’s balances; /api/currencies is public.

gRPC service compiles and works with real DB + RabbitMQ.

House vs Player sums remain balanced per currency_network in tests.

Logs contain correlationId for all operations.

7) Non-goals / Do not do

Do not introduce MediatR, FluentValidation, or extra third-party libs.

Do not change project names (keep Magenta.Grp).

Do not accept playerId in API routes.

Do not use floating-point arithmetic for money.

Now implement all of the above in the respective projects, using the attached Wallet documentation WalletDocumentation.md as the authoritative reference for schema, DR/CR tables, and flows.