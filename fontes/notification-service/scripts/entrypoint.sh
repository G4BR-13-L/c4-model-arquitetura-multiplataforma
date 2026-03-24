#!/bin/bash
set -e

# Aguarda o Postgres estar pronto
until pg_isready -h "${DB_HOST:-localhost}" -p "${DB_PORT:-5432}" -U "${DB_USER:-postgres}"; do
  echo "Aguardando Postgres..."
  sleep 2
done

echo "Postgres pronto! Rodando migrations..."
# Executa as migrations usando o binário que você vai incluir no Docker
./notification-service --migrate-only || echo "Migrations já executadas ou erro controlado"

echo "Iniciando a aplicação..."
exec ./notification-service
