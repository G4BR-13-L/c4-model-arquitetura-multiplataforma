#!/bin/bash

QUEUE_URL="http://localhost:4566/000000000000/minha-fila"

echo "--- 1. Enviando mensagem CORRETA ---"
EMAIL_CORRETO='{
  "sender_name": "Sistema de Alerta",
  "sender_email": "alerta@empresa.com",
  "recipient_email": "usuario@cliente.com",
  "recipient_name": "João Silva",
  "subject": "Seu Relatório Chegou",
  "content": "Conteúdo único do email '$(date +%s)'",
  "original_json": "",
  "status": "PENDING"
}'

docker exec -t localstack_main awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$EMAIL_CORRETO"

echo -e "\n--- 2. Enviando mensagem com CAMPOS FALTANDO (Erro de Validação) ---"
EMAIL_INCOMPLETO='{
  "subject": "Email sem remetente",
  "content": "Isso vai falhar no parse do Serde"
}'

docker exec -t localstack_main awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$EMAIL_INCOMPLETO"

echo -e "\n--- 3. Enviando JSON MALFORMADO (Erro de Sintaxe) ---"
# Note a falta de aspas ou chaves quebradas
JSON_QUEBRADO='{"subject": "Erro de syntax", "content": "faltando fechar chaves"'

docker exec -t localstack_main awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$JSON_QUEBRADO"

echo -e "\n✅ Testes enviados! Verifique os logs do container Rust."
