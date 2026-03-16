FROM lukemathwalker/cargo-chef:latest-rust-1.88 AS chef
WORKDIR /app

FROM chef AS planner
COPY . .
RUN cargo chef prepare --recipe-path recipe.json

FROM chef AS builder
COPY --from=planner /app/recipe.json recipe.json
RUN cargo chef cook --release --recipe-path recipe.json
COPY . .
ENV SQLX_OFFLINE=true
RUN cargo build --release

FROM debian:bookworm-slim AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libssl-dev \
    && rm -rf /var/lib/apt/lists/*
COPY --from=builder /app/target/release/notification-service /app/notification-service
COPY --from=builder /app/config /app/config
ENV APP_ENVIRONMENT=dev
EXPOSE 8080
ENTRYPOINT ["./notification-service"]
