#!/bin/bash
set -e

# O pg_isready aceita uma connection string (URL) com o parâmetro -d
echo "Aguardando Postgres em ${DATABASE_URL}..."

until pg_isready -d "${DATABASE_URL}"; do
  echo "Postgres ainda não está pronto... dormindo 2s"
  sleep 2
done

echo "Postgres pronto! Verificando migrations..."
# Roda as migrations usando a flag que você criou no main.rs
./notification-service --migrate-only

echo "Iniciando a API..."
# Usa 'exec' para o processo Rust virar o PID 1 do container (bom para sinais de encerramento)
exec ./notification-service
