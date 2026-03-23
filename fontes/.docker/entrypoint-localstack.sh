#!/bin/sh
set -eu

REGION="${REGION:-us-east-1}"

create_queue() {
  local queue_name="$1"

  if awslocal sqs get-queue-url --queue-name "$queue_name" >/dev/null 2>&1; then
    echo "Queue '$queue_name' already exists."
    return
  fi

  echo "Creating queue '$queue_name'..."
  awslocal sqs create-queue --queue-name "$queue_name" >/dev/null
}

create_fifo_queue() {
  local queue_name="$1"

  if awslocal sqs get-queue-url --queue-name "$queue_name" >/dev/null 2>&1; then
    echo "FIFO queue '$queue_name' already exists."
    return
  fi

  echo "Creating FIFO queue '$queue_name'..."
  awslocal sqs create-queue \
    --queue-name "$queue_name" \
    --attributes FifoQueue=true,ContentBasedDeduplication=true >/dev/null
}

aws configure set aws_access_key_id default_access_key --profile localstack
aws configure set aws_secret_access_key default_secret_key --profile localstack
aws configure set region "$REGION" --profile localstack

echo "---------- CONFIGURANDO SQS LOCALSTACK ----------"

create_fifo_queue "notification_send_email.fifo"
create_queue "rental_created_fifo"
create_queue "payment_confirmed_fifo"

# Opcional: Lista para confirmar nos logs do container
awslocal sqs list-queues

echo "---------- FILAS CRIADAS COM SUCESSO ----------"
