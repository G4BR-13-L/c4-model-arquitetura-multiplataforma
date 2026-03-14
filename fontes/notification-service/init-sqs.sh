#!/bin/bash
echo "Iniciando criação de filas no LocalStack..."

# Cria a fila que sua aplicação espera
awslocal sqs create-queue --queue-name minha-fila

echo "Filas criadas com sucesso!"
