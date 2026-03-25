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
- `RENTAL_CREATED_QUEUE_NAME`: fila para o evento `rental.created`. Padrao `rental_created_fifo`.
- `PAYMENT_CONFIRMED_QUEUE_NAME`: fila consumida para `payment.confirmed`. Padrao `payment_confirmed_fifo`.
- `PAYMENT_EVENTS_ENABLED`: ativa o consumidor de pagamentos. Padrao `true`.

## Executar localmente

```bash
npm install
npm start
```

## Testes E2E

A suíte end-to-end valida o `rental-service` junto ao `user-service`, `vehicle-service`,
`payment-service` e ao LocalStack/SQS. Ela só funciona com o `docker compose` em execução,
pois precisa dos outros microsserviços e das filas.

O comando é `npm run test:e2e` e tolera a definição das URLs via variáveis de ambiente para
acomodar ambientes diferentes:

- `RENTAL_SERVICE_URL` (padrao `http://localhost:7003`)
- `USER_SERVICE_URL` (padrao `http://localhost:7001`)
- `VEHICLE_SERVICE_URL` (padrao `http://localhost:7002`)
- `PAYMENT_SERVICE_URL` (padrao `http://localhost:3005`)
- `SQS_ENDPOINT` (padrao `http://localhost:4566`)
- `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` seguem os valores do LocalStack (`us-east-1` / `test`)

### Executando a partir da máquina host

```bash
RENTAL_SERVICE_URL=http://localhost:7003 \
USER_SERVICE_URL=http://localhost:7001 \
VEHICLE_SERVICE_URL=http://localhost:7002 \
PAYMENT_SERVICE_URL=http://localhost:3005 \
SQS_ENDPOINT=http://localhost:4566 \
npm run test:e2e
```

### Executando dentro do contêiner (mesma rede Docker)

```bash
docker compose exec rental-service-api sh -c '
  RENTAL_SERVICE_URL=http://localhost:8080 \
  USER_SERVICE_URL=http://user-service-api:8080 \
  VEHICLE_SERVICE_URL=http://vehicle-service-api:8080 \
  PAYMENT_SERVICE_URL=http://payment-service-api:3005 \
  SQS_ENDPOINT=http://localstack:4566 \
  npm run test:e2e
'
```

A suíte cria usuários temporários, inicia locações, consome e publica eventos no SQS e
confirma que o estado final está alinhado com o contrato.
