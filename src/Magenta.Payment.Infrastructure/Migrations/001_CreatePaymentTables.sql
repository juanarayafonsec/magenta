-- Migration: Create Payment System Tables
-- Created: 2024

-- Payment Providers
CREATE TABLE IF NOT EXISTS payment_providers (
    provider_id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL,
    api_base_url TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Deposit Sessions
CREATE TABLE IF NOT EXISTS deposit_sessions (
    session_id UUID PRIMARY KEY,
    player_id BIGINT NOT NULL,
    provider_id INTEGER NOT NULL REFERENCES payment_providers(provider_id),
    currency_network_id INTEGER NOT NULL,
    address TEXT NOT NULL,
    memo_or_tag TEXT,
    provider_reference TEXT,
    expected_amount_minor BIGINT,
    confirmations_required INTEGER NOT NULL DEFAULT 1,
    status VARCHAR(20) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_deposit_sessions_player_id ON deposit_sessions(player_id);
CREATE INDEX IF NOT EXISTS IX_deposit_sessions_status ON deposit_sessions(status);

-- Deposit Requests
CREATE TABLE IF NOT EXISTS deposit_requests (
    deposit_id UUID PRIMARY KEY,
    session_id UUID REFERENCES deposit_sessions(session_id),
    player_id BIGINT NOT NULL,
    provider_id INTEGER NOT NULL REFERENCES payment_providers(provider_id),
    currency_network_id INTEGER NOT NULL,
    tx_hash TEXT NOT NULL UNIQUE,
    amount_minor BIGINT NOT NULL,
    confirmations_received INTEGER NOT NULL DEFAULT 0,
    confirmations_required INTEGER NOT NULL DEFAULT 1,
    status VARCHAR(20) NOT NULL,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_deposit_requests_tx_hash ON deposit_requests(tx_hash);
CREATE INDEX IF NOT EXISTS IX_deposit_requests_status ON deposit_requests(status);
CREATE INDEX IF NOT EXISTS IX_deposit_requests_player_id ON deposit_requests(player_id);

-- Withdrawal Requests
CREATE TABLE IF NOT EXISTS withdrawal_requests (
    withdrawal_id UUID PRIMARY KEY,
    player_id BIGINT NOT NULL,
    provider_id INTEGER NOT NULL REFERENCES payment_providers(provider_id),
    currency_network_id INTEGER NOT NULL,
    amount_minor BIGINT NOT NULL,
    fee_minor BIGINT NOT NULL DEFAULT 0,
    target_address TEXT NOT NULL,
    provider_reference TEXT,
    tx_hash TEXT,
    status VARCHAR(20) NOT NULL,
    fail_reason TEXT,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_withdrawal_requests_status ON withdrawal_requests(status);
CREATE INDEX IF NOT EXISTS IX_withdrawal_requests_player_id ON withdrawal_requests(player_id);

-- Idempotency Keys
CREATE TABLE IF NOT EXISTS idempotency_keys (
    source TEXT NOT NULL,
    idempotency_key TEXT NOT NULL,
    tx_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (source, idempotency_key)
);

-- Outbox Events
CREATE TABLE IF NOT EXISTS outbox_events (
    id BIGSERIAL PRIMARY KEY,
    event_type TEXT NOT NULL,
    routing_key TEXT NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMPTZ,
    publish_attempts INTEGER NOT NULL DEFAULT 0,
    last_error TEXT
);

CREATE INDEX IF NOT EXISTS IX_outbox_events_published_created ON outbox_events(published_at, created_at);

-- Inbox Events
CREATE TABLE IF NOT EXISTS inbox_events (
    id BIGSERIAL PRIMARY KEY,
    source TEXT NOT NULL,
    idempotency_key TEXT NOT NULL,
    payload JSONB NOT NULL,
    received_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMPTZ,
    last_error TEXT,
    UNIQUE (source, idempotency_key)
);

CREATE INDEX IF NOT EXISTS IX_inbox_events_source_idempotency ON inbox_events(source, idempotency_key);
CREATE INDEX IF NOT EXISTS IX_inbox_events_processed_received ON inbox_events(processed_at, received_at);

-- Hangfire schema (will be created automatically by Hangfire, but documented here)
-- Hangfire uses its own schema: hangfire
