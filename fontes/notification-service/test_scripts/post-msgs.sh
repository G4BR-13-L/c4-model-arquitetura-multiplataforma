#!/bin/bash

# Se a fila for FIFO, lembre-se do --message-group-id
QUEUE_URL="http://localhost:4566/000000000000/notification_send_email.fifo"

echo "--- 1. Enviando mensagem CORRETA (Novo Formato) ---"
# Removi a vírgula extra após o campo content e ajustei o JSON
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

# Alterado: localstack_main -> localstack
# Adicionado: message-deduplication-id para permitir múltiplos testes seguidos
docker exec -t localstack awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$EMAIL_CORRETO" \
    --message-group-id "emails-notificacao" \
    --message-deduplication-id "msg-$(date +%s)"

echo -e "\n--- 2. Enviando mensagem com CAMPOS FALTANDO (Erro de Validação) ---"
EMAIL_INCOMPLETO='{
  "subject": "Email sem remetente",
  "content": "Isso vai falhar no parse do Serde"
}'

# Alterado: localstack_main -> localstack
docker exec -t localstack awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$EMAIL_INCOMPLETO" \
    --message-group-id "erros-validacao" \
    --message-deduplication-id "err-val-$(date +%s)"

echo -e "\n--- 3. Enviando JSON MALFORMADO (Erro de Sintaxe) ---"
JSON_QUEBRADO='{"subject": "Erro de syntax", "content": "faltando fechar chaves"'

# Alterado: localstack_main -> localstack
docker exec -t localstack awslocal sqs send-message \
    --queue-url "$QUEUE_URL" \
    --message-body "$JSON_QUEBRADO" \
    --message-group-id "erros-sintaxe" \
    --message-deduplication-id "err-sin-$(date +%s)"

echo -e "\n✅ Testes enviados! Verifique os logs do container Rust."
