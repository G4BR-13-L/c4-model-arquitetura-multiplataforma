FROM lukemathwalker/cargo-chef:latest-rust-1.88 AS chef
WORKDIR /app

FROM chef AS planner
# Copia o que é necessário para calcular a "receita" de dependências
COPY Cargo.toml Cargo.lock ./
COPY src ./src/ 
RUN cargo chef prepare --recipe-path recipe.json

FROM chef AS builder
# 1. Copia a receita gerada no estágio anterior
COPY --from=planner /app/recipe.json recipe.json

# 2. Constrói apenas as dependências (isso será cacheado em uma camada do Docker)
# Removido os --mounts que exigiam BuildKit
RUN cargo chef cook --release --recipe-path recipe.json

# 3. Agora copiamos o código fonte real
COPY . .
ENV SQLX_OFFLINE=true

# 4. Build da aplicação 
# O binário será gerado na pasta target/release padrão
RUN cargo build --release

FROM debian:bookworm-slim AS runtime
WORKDIR /app

# Instala bibliotecas dinâmicas necessárias
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libssl3 \
    && rm -rf /var/lib/apt/lists/*

# Copia o binário final e as configs
# Ajuste o caminho de origem se o nome do seu binário no Cargo.toml for diferente
COPY --from=builder /app/target/release/notification-service /app/notification-service
COPY --from=builder /app/config /app/config

ENV APP_ENVIRONMENT=dev
EXPOSE 8080
ENTRYPOINT ["./notification-service"]