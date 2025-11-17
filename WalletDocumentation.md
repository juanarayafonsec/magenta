# Wallet System Documentation (Converted from PDF)

> Note: This Markdown was auto-converted from PDF; minor formatting fixes may be helpful.


<!-- Page 1 -->
🏦 WALLET SYSTEM
🧭 Overview
The Wallet System is the central accounting and balance-management service for your crypto
casino.
It ensures that every movement of funds (deposit, withdrawal, bet, win, rollback, or fee) is 
double-entry recorded, auditable, and event-driven.
The Wallet integrates with the Payments Service and Game Gateway using RabbitMQ and 
gRPC respectively.
⚙ Components
Wallet.Grpc gRPC Internal command
service — performs all
balance-changing
operations (the single
writer).
Wallet.API HTTP (Cookie Auth)Read-only interface for
Frontend to query
balances and
currencies.
PostgreSQL — Ledger & derived
balances database.
RabbitMQ — Message broker for
async
Wallet↔Payments
communication.Component Interface Purpose

<!-- Page 2 -->
🧩 Architecture Diagram
flowchart LR
FE[Frontend (Cookie Auth)] -->|HTTP| WALAPI[Wallet.API
(Read-Only)]
GP[Game Provider] -->|REST HMAC| GW[Game Gateway]
GW -->|gRPC| WALGRPC[Wallet.Grpc (Commands)]
WALAPI -->|SQL Read| DB[(PostgreSQL)]
WALGRPC -->|SQL R/W| DB
PAY[Payments Service] <-->|RabbitMQ| MQ[(RabbitMQ
Broker)]
WALGRPC <-->|RabbitMQ| MQ
PAY -->|HTTP/Webhooks| CUST[Crypto Gateway]
💾 Database Design
Networks
CREATE TABLE networks (
network_id SERIAL PRIMARY KEY,
name TEXT NOT NULL UNIQUE,
native_symbol TEXT NOT NULL,
confirmations_required INT NOT NULL DEFAULT 1,
explorer_url TEXT,
is_active BOOLEAN NOT NULL DEFAULT TRUE
);


<!-- Page 3 -->
Currencies
CREATE TABLE currencies (
currency_id SERIAL PRIMARY KEY,
code TEXT NOT NULL UNIQUE,
display_name TEXT NOT NULL,
decimals INT NOT NULL CHECK (decimals BETWEEN 0 AND 18),
icon_url TEXT,
sort_order INT NOT NULL DEFAULT 0,
is_active BOOLEAN NOT NULL DEFAULT TRUE
);
Currency Networks
CREATE TABLE currency_networks (
currency_network_id SERIAL PRIMARY KEY,
currency_id INT NOT NULL REFERENCES
currencies(currency_id),
network_id INT NOT NULL REFERENCES networks(network_id),
token_contract TEXT,
withdrawal_fee_minor BIGINT NOT NULL DEFAULT 0,
min_deposit_minor BIGINT NOT NULL DEFAULT 0,
min_withdraw_minor BIGINT NOT NULL DEFAULT 0,
is_active BOOLEAN NOT NULL DEFAULT TRUE,
UNIQUE(currency_id, network_id)
);
Accounts
CREATE TABLE accounts (
account_id BIGSERIAL PRIMARY KEY,
player_id BIGINT NOT NULL,
currency_network_id INT NOT NULL REFERENCES
currency_networks(currency_network_id),
account_type TEXT NOT NULL CHECK (account_type IN

<!-- Page 4 -->
('MAIN','WITHDRAW_HOLD','BONUS','HOUSE','HOUSE:WAGER','HOUSE
:FEES')),
status TEXT NOT NULL DEFAULT 'ACTIVE',
UNIQUE(player_id, currency_network_id, account_type)
);
Ledger Transactions
CREATE TABLE ledger_transactions (
tx_id UUID PRIMARY KEY,
tx_type TEXT NOT NULL,
external_ref TEXT,
metadata JSONB NOT NULL DEFAULT '{}',
created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
Ledger Postings
CREATE TABLE ledger_postings (
posting_id BIGSERIAL PRIMARY KEY,
tx_id UUID NOT NULL REFERENCES
ledger_transactions(tx_id),
account_id BIGINT NOT NULL REFERENCES
accounts(account_id),
direction TEXT NOT NULL CHECK (direction IN
('DEBIT','CREDIT')),
amount_minor BIGINT NOT NULL CHECK (amount_minor >= 0),
created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
Account Balances (Derived)
CREATE TABLE account_balances (
account_id BIGINT PRIMARY KEY REFERENCES
accounts(account_id),

<!-- Page 5 -->
balance_minor BIGINT NOT NULL DEFAULT 0,
reserved_minor BIGINT NOT NULL DEFAULT 0,
cashable_minor BIGINT NOT NULL DEFAULT 0,
updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
🔸 Derived Table: Updated automatically in the same transaction that inserts ledger postings.
It holds the current balance snapshot for each account.
Idempotency Keys
CREATE TABLE idempotency_keys (
source TEXT NOT NULL,
idempotency_key TEXT NOT NULL,
tx_id UUID NOT NULL REFERENCES
ledger_transactions(tx_id),
created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
PRIMARY KEY (source, idempotency_key)
);
Outbox Events / Inbox events
CREATE TABLE outbox_events (
id BIGSERIAL PRIMARY KEY,
event_type TEXT NOT NULL,
routing_key TEXT NOT NULL,
payload JSONB NOT NULL,
created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
published_at TIMESTAMPTZ
);
CREATE TABLE inbox_events (
id BIGSERIAL PRIMARY KEY,
source TEXT NOT NULL,
idempotency_key TEXT NOT NULL,

<!-- Page 6 -->
payload JSONB NOT NULL,
processed_at TIMESTAMPTZ,
UNIQUE(source, idempotency_key)
);
View Player Currency Balances
CREATE OR REPLACE VIEW v_player_currency_balances AS
SELECT
c.code AS currency_code,
n.name AS network,
a.player_id,
COALESCE(ab.balance_minor,0) AS balance_minor,
COALESCE(ab.cashable_minor,0) AS cashable_minor,
COALESCE(ab.reserved_minor,0) AS reserved_minor
FROM currency_networks cn
JOIN currencies c ON c.currency_id = cn.currency_id
JOIN networks n ON n.network_id = cn.network_id
LEFT JOIN accounts a ON a.currency_network_id =
cn.currency_network_id AND a.account_type='MAIN'
LEFT JOIN account_balances ab ON ab.account_id =
a.account_id
WHERE c.is_active AND n.is_active;
📈 ER Diagram
erDiagram
NETWORKS ||--o{ CURRENCY_NETWORKS : has
CURRENCIES ||--o{ CURRENCY_NETWORKS : supports
CURRENCY_NETWORKS ||--o{ ACCOUNTS : used_by
ACCOUNTS ||--o{ LEDGER_POSTINGS : affects
LEDGER_TRANSACTIONS ||--o{ LEDGER_POSTINGS : groups
ACCOUNTS ||--|| ACCOUNT_BALANCES : summarizes

<!-- Page 7 -->
💡 Account Type Glossary
🔢 Minor Units
All monetary values stored as integers in smallest sub-unit (minor unit).
MAIN Playerʼs spendable balance.
WITHDRAW_HOLD Funds locked for withdrawal
requests.
BONUS Optional bonus wallet.
HOUSE Casino treasury.
HOUSE:WAGER Wager pool for bets/wins.
HOUSE:FEES Fee collection account.Account Type Description

<!-- Page 8 -->
1 USDT = 1,000,000
1 BTC = 100,000,000
Prevents rounding issues and ensures consistent precision.
Display conversion:
display_value = amount_minor / (10^decimals)
🔁 Derived Balances Explained
account_balances is not source of truth; itʼs derived from the immutable ledger.
It stores real-time state for fast reads.
Itʼs updated in the same transaction as postings.
Rebuild example:
SELECT account_id,
SUM(CASE WHEN direction='CREDIT' THEN amount_minor ELSE -
amount_minor END) AS balance_minor
FROM ledger_postings GROUP BY account_id;
🧾 Ledger Posting Rules (DR/CR)balance_minor Current total ( Σ  CR − Σ  DR).
reserved_minor Sum of WITHDRAW_HOLD
balances.
cashable_minor balance_minor −
reserved_minor.
updated_at Last update timestamp.Column Description
Deposit settledHOUSE Player:MAINExternal →
Player
Withdrawal
reservedPlayer:MAINPlayer:WITHDRA
W_HOLDLock fundsEvent DR CR Flow

<!-- Page 9 -->
🧠 Metadata (JSONB)
Used to store contextual, non-financial info inWithdrawal
settledPlayer:WITHDRA
W_HOLDHOUSE (net),
HOUSE:FEES
(fee)Funds exit
Withdrawal failedPlayer:WITHDRA
W_HOLDPlayer:MAINUnlock
Bet Player:MAINHOUSE:WAGERPlayer → House
Win HOUSE:WAGERPlayer:MAINHouse →  Player
Rollback BetHOUSE:WAGERPlayer:MAINReverse
Rollback WinPlayer:MAINHOUSE:WAGERReverse
Fee Player:MAINHOUSE:FEESPlayer → House
Deposit {
"source":"payments.dep
osit.settled",
"txHash":"0x123",
"network":"TRON" }
Withdrawal Reserve {
"requestId":"wd_829",
"network":"TRON" }
Withdrawal Settled { "txHash":"0xabc",
"feeMinor":200000,
"network":"TRON" }
Bet {
"provider":"PragmaticPOperation Example Metadata

<!-- Page 10 -->
🔐 Wallet.API
💬 Wallet.Grpc
Handles all balance mutations, applies DR/CR pairs, and emits MQ events.
RPCs
ApplyDepositSettlement
ReserveWithdrawal
FinalizeWithdrawal
ReleaseWithdrawal
PlaceBet
SettleWin
Rollbacklay","betId":"B123","r
oundId":"R567" }
Win {
"provider":"PragmaticP
lay","winId":"W678","r
oundId":"R567" }
Rollback {
"referenceType":"BET",
"referenceId":"B123","
reason":"GameCrash" }
GET
/api/currencie
sPublic List active
currency/network pairs
GET
/api/balancesCookie Get balances for
authenticated playerRoute Auth Purpose

<!-- Page 11 -->
GetBalance
Each RPC:
1. Starts SERIALIZABLE TX.
2. Inserts ledger transaction + postings.
3. Updates derived balances.
4. Emits events via Outbox.
5. Commits atomically.
🐇 RabbitMQ Events
Exchanges:
wallet.events
payments.events
Wallet →  Payments
Payments →  Walletwallet.withdrawal.rese
rved{eventId, playerId,
currency, network,
amountMinor}
wallet.balance.changed{playerId, balances:
[...]}Routing Key Payload
payments.deposit.settl
ed{playerId, currency,
network, amountMinor,
txHash}
payments.withdrawal.br
oadcasted{requestId, txHash}Routing Key Payload

<!-- Page 12 -->
📊 Reporting
📘 Glossarypayments.withdrawal.se
ttled{requestId,
amountMinor, feeMinor,
txHash}
payments.withdrawal.fa
iled{requestId, reason}
GGR Bets − Wins Gross Gaming Revenue
Fees Σ credits to
HOUSE:FEESFee income
Player LiabilitiesΣ player balancesOwed funds
Casino Cash Σ HOUSE* balancesTotal treasury
Ledger IntegrityΣ DR = Σ  CR Must hold trueMetric Formula Meaning
DR (Debit) Value leaves account.
CR (Credit) Value enters account.
Ledger Transaction Logical event grouping postings.
Posting One DR or CR line.
Derived Table Cached result updated from ledger.
Minor Units Integer smallest subdivision.
Outbox/Inbox Reliable event publishing & dedup.
Idempotency Key Unique per external event.Term Definition

<!-- Page 13 -->
Saga Cross-service flow
(Wallet↔Payments).
GGR/NGR Gross/Net Gaming Revenue.

---
## AI Generation Prompt (Wallet System)

Create a .NET 9 solution named "WalletSystem" within the existing projects:
- Magenta.Domain
- Magenta.Application
- Magenta.Infrastructure
- Magenta.API  (HTTP read-only, cookie-auth)
- Magenta.Grp  (internal gRPC for commands)

Use the attached Wallet documentation (this Markdown) as the source of truth.

Key requirements:
- Double-entry ledger with DR/CR pairs for: Deposit, Withdrawal (reserve/broadcast/settle/fail), Bet, Win, Rollback, Fee.
- Minor units (integers) for all monetary fields.
- Multi-network currencies: networks, currencies, currency_networks, and unique accounts per (player, currency_network, account_type).
- Derived balances (account_balances): update inside the same DB transaction that inserts ledger_postings; recompute reserved and cashable.
- SERIALIZABLE isolation; SELECT ... FOR UPDATE on relevant balance rows.
- Idempotency: idempotency_keys(source, key) persisted inside the same transaction.
- Outbox/Inbox patterns: publish wallet.balance.changed after every mutation; publish wallet.withdrawal.reserved after reservation; consume payments.* events idempotently.
- RabbitMQ exchanges: wallet.events, payments.events.

Implement:
1) Magenta.Domain
   - Domain entities, enums, value objects (Money long minor units).
   - PostingRules mapping for each operation to DR/CR pairs.
   - DerivedBalanceUpdater logic.
   - CurrencyNetworkResolver & Idempotency service interfaces.

2) Magenta.Infrastructure
   - PostgreSQL schema (tables, indexes, view) per documentation.
   - Migrations + seeds (networks, currencies, currency_networks, House accounts & balances).
   - Repositories for ledger write/read, outbox/inbox stores.
   - Background services: OutboxPublisher, PaymentsEventsConsumer.
   - RabbitMQ setup (topic exchanges).

3) Magenta.Application
   - Command/query handlers for: ApplyDepositSettlement, ReserveWithdrawal, FinalizeWithdrawal, ReleaseWithdrawal, PlaceBet, SettleWin, Rollback, GetBalance.
   - Each handler: resolve currency_network_id; ensure accounts; idempotency check; begin SERIALIZABLE TX; write ledger_transaction + postings; update account_balances; insert outbox; insert idempotency; commit.
   - Build metadata JSONB for each operation (no secrets).

4) Magenta.Grp
   - wallet.proto with RPCs above (minor units as int64); implement service delegating to Application handlers.
   - Propagate correlationId in logs/events.

5) Magenta.API
   - GET /api/currencies (public)
   - GET /api/balances (cookie-auth; infer playerId from cookie; do not accept playerId in URL).

Events:
- Consume (payments.events): payments.deposit.settled, payments.withdrawal.broadcasted, payments.withdrawal.settled, payments.withdrawal.failed.
- Publish (wallet.events): wallet.withdrawal.reserved, wallet.balance.changed.

Acceptance:
- DR/CR postings correct and balanced.
- Derived balances updated atomically; reads use account_balances only.
- Idempotency enforced (no duplicate postings).
- Outbox published; inbox deduped.
- API security via cookie; no playerId route params.
- House vs Player sums stay balanced per currency_network.
