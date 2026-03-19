#!/bin/bash
echo "Iniciando criação de filas no LocalStack..."

awslocal sqs create-queue \
  --queue-name notification_send_email.fifo \
  --attributes FifoQueue=true,ContentBasedDeduplication=true


echo "Filas criadas com sucesso!"