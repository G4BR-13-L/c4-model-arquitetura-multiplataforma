# --- Stage 1: Planejamento (Otimização de Cache) ---
FROM lukemathwalker/cargo-chef:latest-rust-1.84 AS chef
WORKDIR /app

# --- Stage 2: Receita (Prepara as dependências) ---
FROM chef AS planner
COPY . .
RUN cargo chef prepare --recipe-path recipe.json

# --- Stage 3: Build (Compilação real) ---
FROM chef AS builder
COPY --from=planner /app/recipe.json recipe.json
# Constrói apenas as dependências para cachear essa camada
RUN cargo chef cook --release --recipe-path recipe.json

# Agora copia o código fonte e compila o projeto
COPY . .
RUN cargo build --release

# --- Stage 4: Runtime (Imagem final leve) ---
FROM debian:bookworm-slim AS runtime
WORKDIR /app

# Instala certificados CA e bibliotecas necessárias para requisições HTTPS (AWS/Lettre)
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libssl-dev \
    && rm -rf /var/lib/apt/lists/*

# Copia o binário compilado do stage builder
COPY --from=builder /app/target/release/notification-service /app/notification-service

# Copia as pastas de configuração necessárias
COPY --from=builder /app/config /app/config

# Define as variáveis de ambiente (podem ser sobrescritas no docker-compose)
ENV APP_ENVIRONMENT=prod

# Porta que o Actix-Web escuta
EXPOSE 8080

# Comando para rodar a aplicação
ENTRYPOINT ["./notification-service"]
