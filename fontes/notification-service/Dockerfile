FROM lukemathwalker/cargo-chef:latest-rust-1.88 AS chef
WORKDIR /app

FROM chef AS planner
# Copia apenas o que é estritamente necessário para a receita
COPY Cargo.toml Cargo.lock ./
COPY src ./src/ 
# Se tiver outros folders de crates (workspace), copie-os aqui
RUN cargo chef prepare --recipe-path recipe.json

FROM chef AS builder
COPY --from=planner /app/recipe.json recipe.json
# Cache das dependências
RUN --mount=type=cache,target=/usr/local/cargo/registry \
    --mount=type=cache,target=/app/target \
    cargo chef cook --release --recipe-path recipe.json

COPY . .
ENV SQLX_OFFLINE=true
# Build da aplicação usando cache do target
RUN --mount=type=cache,target=/usr/local/cargo/registry \
    --mount=type=cache,target=/app/target \
    cargo build --release && \
    cp target/release/notification-service /app/notification-service

FROM debian:bookworm-slim AS runtime
WORKDIR /app
# Instala apenas o necessário para rodar
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libssl3 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=builder /app/notification-service /app/notification-service
COPY --from=builder /app/config /app/config

ENV APP_ENVIRONMENT=dev
EXPOSE 8080
ENTRYPOINT ["./notification-service"]
