#!/bin/bash

# Se a fila for FIFO, lembre-se do --message-group-id
QUEUE_URL="http://localhost:4566/000000000000/notification_send_email.fifo"

echo "--- 1. Enviando mensagem CORRETA (Novo Formato) ---"
EMAIL_CORRETO='{
  "event_type": "notification.email",
  "occurred_at": "2026-03-22T20:27:14.635Z",
  "data": {
    "sender_name": "Sistema de Alerta",
    "sender_email": "alerta@empresa.com",
    "recipient_email": "usuario@cliente.com",
    "recipient_name": "João Silva",
    "subject": "Seu Relatório Chegou",
    "content": "Conteúdo único do email '$(date +%s)'"
  }
}'

docker exec -t localstack_main awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$EMAIL_CORRETO" \
    --message-group-id "emails-notificacao"

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
