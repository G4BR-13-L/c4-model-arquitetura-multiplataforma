# rental-service

Servico de locacao em Node.js alinhado ao contrato revisado em `fontes/CONTRATOS_revisados.md`.

## Endpoints

- `POST /rentals` cria uma locacao autenticada.
- `GET /rentals` lista as locacoes do usuario autenticado.
- `GET /rentals/{id}` retorna uma locacao do usuario autenticado.
- `GET /health` verifica o estado da aplicacao.

Compatibilidade legada:

- `POST /v1/rental`
- `GET /v1/rental`
- `GET /v1/rental/{id}`

## Variaveis de ambiente

- `PORT`: porta HTTP. Padrao `8080`.
- `VEHICLE_SERVICE_BASE_URL`: URL do `vehicle-service`. Padrao `http://localhost:7002`.
- `KEYCLOAK_AUTHORITY`: issuer base do Keycloak. Ex.: `http://keycloak:8080/realms/master`.
- `KEYCLOAK_CLIENT_ID`: client usado para validar `aud`/`azp` e para client credentials.
- `KEYCLOAK_CLIENT_SECRET`: segredo para obter token de servico quando o rental precisar chamar outro servico sem token de usuario.
- `AWS_REGION`: regiao do SQS. Padrao `us-east-1`.
- `SQS_ENDPOINT`: endpoint do LocalStack/AWS. Ex.: `http://localstack:4566`.
- `RENTAL_CREATED_QUEUE_NAME`: fila para o evento `rental.created`. Padrao `rental-created`.
- `PAYMENT_CONFIRMED_QUEUE_NAME`: fila consumida para `payment.confirmed`. Padrao `payment-confirmed`.
- `PAYMENT_EVENTS_ENABLED`: ativa o consumidor de pagamentos. Padrao `true`.

## Executar localmente

```bash
npm install
npm start
```
