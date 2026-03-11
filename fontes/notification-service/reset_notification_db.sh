#!/bin/bash

# Configurações
CONTAINER_NAME="notifications_service"
VOLUME_NAME="notifications_pg_data" # Nome do volume conforme seu docker-compose

echo "🚀 Iniciando destruição e recriação do ambiente de banco de dados..."

# 1. Derruba o container e remove o volume associado
# O comando 'down -v' remove containers e volumes declarados no docker-compose
docker compose down -v

if [ $? -eq 0 ]; then
    echo "|--- ✅ Container e Volume ($VOLUME_NAME) removidos."
else
    echo "|--- ⚠️  Erro ao tentar derrubar via compose. Tentando remoção manual..."
    docker rm -f $CONTAINER_NAME
    docker volume rm $(docker volume ls -q | grep $VOLUME_NAME)
fi

# 2. Sobe o container novamente
echo "|--- 🛠️  Recriando container e inicializando novo volume..."
docker compose up -d

# 3. Aguarda o Postgres ficar pronto para conexões
echo -n "|--- ⏳ Aguardando Postgres inicializar"
until docker exec $CONTAINER_NAME pg_isready -U postgres > /dev/null 2>&1; do
    echo -n "."
    sleep 1
done

echo -e "\n✅ Banco de Dados '$CONTAINER_NAME' está pronto e limpo!"
