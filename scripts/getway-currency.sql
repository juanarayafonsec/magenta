-- Accounts: one player can have multiple (e.g., MAIN, BONUS, NON-CASHABLE)
CREATE TABLE accounts (
  account_id        BIGSERIAL PRIMARY KEY,
  player_id         BIGINT NOT NULL,
  currency          TEXT NOT NULL,            -- "USDT-TRX", "BTC", "EUR"
  account_type      TEXT NOT NULL,            -- MAIN, BONUS, HOUSE, FEES
  status            TEXT NOT NULL DEFAULT 'ACTIVE',
  UNIQUE (player_id, currency, account_type)
);

-- Transactions: logical grouping (e.g., a bet or deposit)
CREATE TABLE ledger_transactions (
  tx_id             UUID PRIMARY KEY,
  tx_type           TEXT NOT NULL,            -- BET, WIN, DEPOSIT, WITHDRAWAL, ROLLBACK, FEE, ADJUSTMENT
  external_ref      TEXT,                     -- provider round/txn id or blockchain tx hash
  created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
  metadata          JSONB NOT NULL DEFAULT '{}'
);

-- Postings: actual debits/credits. Amounts as integer minor units.
CREATE TABLE ledger_postings (
  posting_id        BIGSERIAL PRIMARY KEY,
  tx_id             UUID NOT NULL REFERENCES ledger_transactions(tx_id),
  account_id        BIGINT NOT NULL REFERENCES accounts(account_id),
  direction         TEXT NOT NULL CHECK (direction IN ('DEBIT','CREDIT')),
  amount_minor      BIGINT NOT NULL CHECK (amount_minor >= 0),
  created_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Idempotency & dedup (per external system)
CREATE TABLE idempotency_keys (
  source            TEXT NOT NULL,            -- e.g. 'GameX', 'PaymentsGatewayY'
  idempotency_key   TEXT NOT NULL,
  tx_id             UUID NOT NULL,
  created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (source, idempotency_key)
);

-- Derived, transactional balances, updated via trigger or stored proc
CREATE TABLE account_balances (
  account_id        BIGINT PRIMARY KEY REFERENCES accounts(account_id),
  balance_minor     BIGINT NOT NULL DEFAULT 0,        -- total
  reserved_minor    BIGINT NOT NULL DEFAULT 0,        -- pending bets (optional)
  cashable_minor    BIGINT NOT NULL DEFAULT 0,        -- usable for withdrawal
  updated_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);
