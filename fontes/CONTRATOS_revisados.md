Aqui está o fluxo revisado e padronizado, seguindo as convenções REST, com UUIDs, JSON consistente, headers corretos e nomenclatura em inglês (padrão de mercado para APIs).

---

## User Service

### `POST /users` — criar usuário
**Request body:**
```json
{
  "first_name": "string",
  "last_name": "string",
  "email": "string",
  "username": "string",
  "password": "string",
  "document_number": "string"
}
```
**Response `201 Created`:**
```json
{
  "id": "uuid",
  "first_name": "string",
  "last_name": "string",
  "email": "string",
  "username": "string",
  "document_number": "string",
  "created_at": "ISO 8601"
}
```

### `GET /users` — listar usuários
**Response `200 OK`:**
```json
[
  {
    "id": "uuid",
    "first_name": "string",
    "last_name": "string",
    "email": "string",
    "username": "string",
    "document_number": "string"
  }
]
```

### `GET /users/{id}` — buscar usuário por ID
**Response `200 OK`:** mesmo shape do item acima (objeto único).

### Evento SQS — `user.created`
```json
{
  "event_type": "user.created",
  "occurred_at": "ISO 8601",
  "data": {
    "id": "uuid",
    "first_name": "string",
    "last_name": "string",
    "email": "string",
    "username": "string",
    "document_number": "string"
  }
}
```

---

## Vehicle Service

### `GET /categories` — listar categorias
**Response `200 OK`:**
```json
[
  {
    "id": "uuid",
    "name": "string",
    "description": "string"
  }
]
```

### `GET /categories/{id}/vehicles` — veículos de uma categoria
**Response `200 OK`:** array de veículos (mesmo shape abaixo).

### `GET /vehicles` — listar veículos
### `GET /vehicles/{id}` — buscar veículo por ID
**Response `200 OK`:**
```json
{
  "id": "uuid",
  "model": "string",
  "license_plate": "string",
  "category_id": "uuid",
  "available": "boolean",
  "daily_price": "number"
}
```

### `POST - /vehicles/{id}/reservation` — reservar veículo
### `PUT - /vehicles/{id}/return` — tornar o veículo disponivel
> utilizar token de autenticação entre APIs
**Response `200 OK`:**


---

## Rental Service

### `POST /v1/rentals` — criar locação *(autenticado)*
**Header:** `Authorization: Bearer <jwt>`

**Request body:**
```json
{
  "vehicle_id": "uuid",
  "start_date": "ISO 8601",
  "end_date": "ISO 8601"
}
```
**Response `201 Created`:**
```json
{
  "id": "uuid",
  "vehicle_id": "uuid",
  "user_id": "uuid",
  "start_date": "ISO 8601",
  "end_date": "ISO 8601",
  "total_amount": "number",
  "payment_status": "PENDING | CONFIRMED | FAILED",
  "status": "PENDING | COMPLETED | CANCELLED",
  "created_at": "ISO 8601"
}
```

### `GET /v1/rentals` — listar locações *(autenticado)*
**Header:** `Authorization: Bearer <jwt>`
**Response `200 OK`:** array com o mesmo shape acima.

### `GET /v1/rentals/{id}` — buscar locação por ID *(autenticado)*
**Header:** `Authorization: Bearer <jwt>`
**Response `200 OK`:** objeto único com o mesmo shape.

### Evento SQS — `rental.created` *(rental publica → payment consome)*
```json
{
  "event_type": "rental.created",
  "occurred_at": "ISO 8601",
  "data": {
    "id": "uuid",
    "vehicle_id": "uuid",
    "user_id": "uuid",
    "start_date": "ISO 8601",
    "end_date": "ISO 8601",
    "total_amount": "number",
    "payment_status": "PENDING",
    "status": "PENDING"
  }
}
```

---

## Payment Service

### `POST /payments` — iniciar pagamento
**Request body:**
```json
{
  "rental_id": "uuid",
  "amount": "number",
  "payment_method": "CREDIT_CARD | DEBIT_CARD | PIX | BOLETO"
}
```
**Response `201 Created`:**
```json
{
  "id": "uuid",
  "rental_id": "uuid",
  "amount": "number",
  "payment_method": "CREDIT_CARD | DEBIT_CARD | PIX | BOLETO",
  "status": "PENDING | CONFIRMED | FAILED ",
  "checkout_url": "string",
  "created_at": "ISO 8601"
}
```

### `GET /payments` — listar pagamentos *(autenticado)*
**Header:** `Authorization: Bearer <jwt>`
**Response `200 OK`:**
```json
[
  {
    "id": "uuid",
    "rental_id": "uuid",
    "amount": "number",
    "payment_method": "string",
    "status": "string",
    "created_at": "ISO 8601"
  }
]
```

### Evento SQS — `payment.confirmed` *(payment publica → rental consome)*
```json
{
  "event_type": "payment.confirmed",
  "occurred_at": "ISO 8601",
  "data": {
    "payment_id": "uuid",
    "rental_id": "uuid",
    "status": "CONFIRMED | FAILED "
  }
}
```

---

## Notification Service

### Evento SQS — `notification.email`
```json
{
  "event_type": "notification.email",
  "occurred_at": "ISO 8601",
  "data": {
    "sender_email": "string",
    "sender_name": "string",
    "recipient_email": "string",
    "recipient_name": "string",
    "subject": "string",
    "content": "string"
  }
}
```

---

Veja o fluxo visual dos eventos assíncronos entre os serviços:---

## Principais padronizações aplicadas

**Nomenclatura:** campos em `snake_case` inglês (`first_name` em vez de `firstname`, `daily_price` em vez de `valor_diaria`, `license_plate` em vez de `placa`).

**IDs:** todos como `uuid` (v4), incluindo `rental_id` que estava como `String`.

**Rotas REST:** recursos no plural e sem verbos na URL (`/v1/rentals` em vez de `/rental`, `POST /v1/rentals` em vez de `POST /vehicles/{id}/reservation`).

**Valores monetários:** `total_amount` como `number` (decimal, ex: `150.00`) em vez de `bigint` — evita problemas de serialização JSON e é adequado para valores em reais com centavos.

**Datas:** todas as datas em ISO 8601 (`2025-03-18T14:30:00Z`) em vez de nomes genéricos como `data_inicio`.

**Enums explícitos:** `payment_status` e `status` com valores documentados (`PENDING`, `CONFIRMED`, etc.) e `payment_method` com opções válidas (`CREDIT_CARD`, `PIX`, etc.).

**Eventos SQS:** padronizados com envelope `{ event_type, occurred_at, data }` — facilita roteamento e versionamento no broker.

**`checkout_url`** adicionado no response do `POST /payments` para o fluxo do link de pagamento por e-mail que já existia na lógica, mas não estava refletido no contrato.
