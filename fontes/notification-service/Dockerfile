# 1. Base com cargo-chef
FROM lukemathwalker/cargo-chef:latest-rust-1.89 AS chef
WORKDIR /app

# 2. Planejamento (Planner)
FROM chef AS planner
COPY Cargo.toml Cargo.lock ./
# CRIAMOS UM ESQUELETO MÍNIMO:
# Isso permite que o 'cargo metadata' funcione sem precisar do seu código real.
RUN mkdir src && echo "fn main() {}" > src/main.rs
RUN cargo chef prepare --recipe-path recipe.json

# 3. Construção das Dependências (Cacher)
FROM chef AS builder
COPY --from=planner /app/recipe.json recipe.json
# Compila apenas as bibliotecas externas.
# Se o Cargo.toml não mudar, o Docker usará o CACHE aqui (etapa mais demorada).
RUN cargo chef cook --release --recipe-path recipe.json

# 4. Compilação do Código Real
# Agora sim copiamos o seu código fonte real.
COPY . .
ENV SQLX_OFFLINE=true

# O build agora será instantâneo (apenas o seu código)
RUN cargo build --release --bin notification-service

# 5. Imagem Final (Runtime)
FROM debian:bookworm-slim AS runtime
WORKDIR /app

# Instala libssl (pro Rust) e postgresql-client (pro entrypoint.sh)
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libssl3 \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/*

# Copia o binário
COPY --from=builder /app/target/release/notification-service /app/notification-service
# Copia a pasta de configurações (importante para o Settings::new())
COPY --from=builder /app/config /app/config
# Copia o script
COPY scripts/entrypoint.sh ./scripts/entrypoint.sh

RUN chmod +x ./scripts/entrypoint.sh

EXPOSE 8080
# O ENTRYPOINT deve ser o script
ENTRYPOINT ["./scripts/entrypoint.sh"]
