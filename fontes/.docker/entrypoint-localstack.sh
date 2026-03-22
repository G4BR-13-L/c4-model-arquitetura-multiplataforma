#!/bin/sh

ENDPOINT=http://localstack:4566
REGION=us-east-1

aws configure set aws_access_key_id default_access_key --profile=localstack
aws configure set aws_secret_access_key default_secret_key --profile=localstack
aws configure set region $REGION --profile=localstack

echo "---------- CONFIGURANDO SQS LOCALSTACK ----------"

# Cria a fila FIFO
awslocal sqs create-queue \
  --queue-name notification_send_email.fifo \
  --attributes FifoQueue=true,ContentBasedDeduplication=true


# Opcional: Lista para confirmar nos logs do container
awslocal sqs list-queues

echo "---------- FILAS CRIADAS COM SUCESSO ----------"
