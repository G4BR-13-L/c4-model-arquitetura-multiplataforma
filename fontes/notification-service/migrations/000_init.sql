-- Garante a extensão para UUIDs
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =========================
-- EMAIL
-- =========================
CREATE TABLE IF NOT EXISTS t_email (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sender_name VARCHAR(255) NOT NULL,
    sender_email VARCHAR(255) NOT NULL,
    recipient_email VARCHAR(255) NOT NULL,
    recipient_name VARCHAR(255) NOT NULL,
    subject VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    original_json TEXT NOT NULL,
    status VARCHAR(255) NOT NULL,
    inserted_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS t_erro_mensagem_email (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    raw_content TEXT NOT NULL,
    error_message TEXT NOT NULL,
    inserted_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
