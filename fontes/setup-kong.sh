#!/bin/bash
# ============================================================
# setup-kong.sh — Configure Kong API Gateway
# Execute no Git Bash a partir da pasta fontes/
# ============================================================

set -e

KONG_ADMIN="http://localhost:8001"
KEYCLOAK="http://localhost:7000"
REALM="master"

echo ""
echo "============================================"
echo " [1/5] Aguardando Kong..."
echo "============================================"
until curl -sf "$KONG_ADMIN/status" > /dev/null 2>&1; do
  echo "  Aguardando Kong..."
  sleep 3
done
echo "  Kong OK!"

echo ""
echo "============================================"
echo " [2/5] Importando kong.yaml..."
echo "============================================"
docker cp kong/kong.yaml kong:/tmp/kong.yaml
docker exec kong kong config db_import //tmp/kong.yaml
echo "  Importacao concluida!"

echo ""
echo "============================================"
echo " [3/5] Aguardando Keycloak..."
echo "============================================"
until curl -sf "$KEYCLOAK/realms/$REALM" > /dev/null 2>&1; do
  echo "  Aguardando Keycloak... (pode levar 2-3 min)"
  sleep 10
done
echo "  Keycloak OK!"

echo ""
echo "============================================"
echo " [4/5] Extraindo chave publica do Keycloak..."
echo "============================================"

CERTS=$(curl -sf "$KEYCLOAK/realms/$REALM/protocol/openid-connect/certs")

X5C=$(echo "$CERTS" \
  | grep -o '"x5c":\["[^"]*"' \
  | head -1 \
  | sed 's/"x5c":\["//;s/"//')

if [ -z "$X5C" ]; then
  echo "  ERRO: nao foi possivel extrair o x5c"
  exit 1
fi

echo "  x5c extraido!"

CERT_FILE=$(mktemp --suffix=.crt)
PUB_FILE=$(mktemp --suffix=.pub)

{
  echo "-----BEGIN CERTIFICATE-----"
  echo "$X5C" | fold -w 64
  echo "-----END CERTIFICATE-----"
} > "$CERT_FILE"

openssl x509 -pubkey -noout -in "$CERT_FILE" -out "$PUB_FILE"

echo "  Chave publica extraida!"
cat "$PUB_FILE"

# Ler conteudo como variavel — evita passar caminho para o curl
PUBKEY_CONTENT=$(cat "$PUB_FILE")
rm -f "$CERT_FILE" "$PUB_FILE"

echo ""
echo "============================================"
echo " [5/5] Registrando JWT consumer no Kong..."
echo "============================================"

curl -sf -X POST "$KONG_ADMIN/consumers" \
  --data username=keycloak-users > /dev/null 2>&1 \
  && echo "  Consumer criado!" \
  || echo "  Consumer ja existe, continuando..."

# Passar a chave como variavel string — sem @arquivo, sem caminho
RESULT=$(curl -s -X POST "$KONG_ADMIN/consumers/keycloak-users/jwt" \
  --data "algorithm=RS256" \
  --data "key=http://keycloak:8080/realms/master" \
  --data-urlencode "rsa_public_key=$PUBKEY_CONTENT")

if echo "$RESULT" | grep -q '"id"'; then
  echo "  JWT registrado com sucesso!"
elif echo "$RESULT" | grep -q 'unique constraint'; then
  echo "  JWT ja registrado, continuando..."
else
  echo "  Resposta Kong: $RESULT"
fi

echo ""
echo "============================================"
echo " Verificacao final"
echo "============================================"

echo ""
echo "Services:"
curl -sf "$KONG_ADMIN/services" \
  | grep -o '"name":"[^"]*"' \
  | sed 's/"name":"//;s/"//' \
  | while read -r name; do echo "  OK  $name"; done

echo ""
echo "Routes:"
curl -sf "$KONG_ADMIN/routes" \
  | grep -o '"name":"[^"]*"' \
  | sed 's/"name":"//;s/"//' \
  | while read -r name; do echo "  OK  $name"; done

echo ""
echo "Plugins:"
curl -sf "$KONG_ADMIN/plugins" \
  | grep -o '"name":"[^"]*"' \
  | sed 's/"name":"//;s/"//' \
  | while read -r name; do echo "  OK  $name"; done

echo ""
echo "============================================"
echo " Setup finalizado com sucesso!"
echo "============================================"
echo ""
echo "  Endpoints via Kong (porta 8000):"
echo "  POST  http://localhost:8000/api/auth/token   <- gerar token"
echo "  POST  http://localhost:8000/api/users        <- criar usuario"
echo "  GET   http://localhost:8000/api/vehicles     <- listar veiculos"
echo ""
echo "  Kong Admin:  http://localhost:8001"
echo "  Konga UI:    http://localhost:1337"
echo "  Keycloak:    http://localhost:7000"
echo ""