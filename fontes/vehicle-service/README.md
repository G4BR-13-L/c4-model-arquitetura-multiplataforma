# VehicleService.API

API RESTful para gerenciamento de veiculos e categorias, desenvolvida em **.NET 9** com suporte a autenticacao via Keycloak, observabilidade com OpenTelemetry e Serilog, persistencia com PostgreSQL via Entity Framework Core e documentacao interativa com Swagger.

---

## Sumario

- [Tecnologias e Pacotes](#tecnologias-e-pacotes)
- [Dominio](#dominio)
- [Endpoints](#endpoints)
  - [Veiculos](#veiculos)
  - [Categorias](#categorias)
- [Autenticacao e Autorizacao](#autenticacao-e-autorizacao)
- [Banco de Dados](#banco-de-dados)
- [Observabilidade](#observabilidade)
- [Logging](#logging)
- [Documentacao da API](#documentacao-da-api)
- [Execucao Local](#execucao-local)

---

## Tecnologias e Pacotes

### Runtime

| Tecnologia | Versao |
|---|---|
| .NET | 9.0 |
| ASP.NET Core | 9.0 |

### Pacotes NuGet

| Pacote | Versao | Finalidade |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.x | Validacao de tokens JWT emitidos pelo Keycloak |
| `Microsoft.EntityFrameworkCore` | 9.0.4 | ORM para acesso ao banco de dados |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.4 | Suporte a ferramentas de design (migrations via CLI) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | Provider do EF Core para PostgreSQL |
| `OpenTelemetry.Extensions.Hosting` | 1.15.0 | Integracao do OpenTelemetry com o host ASP.NET Core |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.15.1 | Rastreamento automatico de requisicoes HTTP recebidas |
| `OpenTelemetry.Instrumentation.Http` | 1.15.0 | Rastreamento automatico de chamadas `HttpClient` |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.15.0-beta.1 | Rastreamento automatico de queries do EF Core |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.15.0 | Exportacao de traces via OTLP (HTTP/protobuf) para o Jaeger |
| `Serilog.AspNetCore` | 10.0.0 | Integracao do Serilog com o host ASP.NET Core |
| `Serilog.Sinks.Console` | 6.1.1 | Saida de logs no console |
| `Swashbuckle.AspNetCore` | 6.x | Geracao e interface interativa do Swagger UI |

---

## Dominio

### `Vehicle`

Representa um veiculo disponivel para locacao.

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Id` | `Guid` | Identificador unico |
| `Model` | `string` | Modelo do veiculo |
| `LicensePlate` | `string` | Placa (unica) |
| `CategoryId` | `Guid` | Referencia a categoria |
| `Available` | `bool` | Indica se esta disponivel para reserva |
| `DailyPrice` | `decimal` | Preco por diaria |
| `CreatedAt` | `DateTime` | Data de criacao |
| `UpdatedAt` | `DateTime` | Data da ultima atualizacao |

**Metodos de dominio:**

- `Reserve()` - marca o veiculo como indisponivel (`Available = false`) e atualiza `UpdatedAt`. Lanca `InvalidOperationException` se ja estiver indisponivel.
- `Return()` - marca o veiculo como disponivel (`Available = true`) e atualiza `UpdatedAt`.

### `Category`

Representa uma categoria de veiculos (ex: Hatch, Sedan, SUV).

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Id` | `Guid` | Identificador unico |
| `Name` | `string` | Nome da categoria |
| `Description` | `string` | Descricao da categoria |
| `Optionals` | `List<string>` | Lista de opcionais disponiveis |
| `Vehicles` | `List<Vehicle>` | Veiculos pertencentes a categoria |

---

## Endpoints

### Veiculos

Base URL: `/vehicles`

#### `GET /vehicles`

Retorna a lista de todos os veiculos cadastrados.

- **Autenticacao:** nao requerida
- **Resposta:** `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "model": "Volkswagen Gol",
    "license_plate": "ABC-1234",
    "category_id": "557dc64b-e7c4-40b0-918f-4469be2c5d06",
    "available": true,
    "daily_price": 89.90
  }
]
```

---

#### `GET /vehicles/{id}`

Retorna um veiculo pelo seu `id`.

- **Autenticacao:** nao requerida
- **Respostas:**
  - `200 OK` - veiculo encontrado
  - `404 Not Found` - veiculo nao encontrado

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "model": "Volkswagen Gol",
  "license_plate": "ABC-1234",
  "category_id": "557dc64b-e7c4-40b0-918f-4469be2c5d06",
  "available": true,
  "daily_price": 89.90
}
```

---

#### `POST /vehicles/{id}/reservation`

Realiza a reserva de um veiculo. Chama o metodo de dominio `Reserve()` e persiste a alteracao.

- **Autenticacao:** **requerida** (Bearer JWT)
- **Respostas:**
  - `204 No Content` - reserva realizada com sucesso
  - `400 Bad Request` - veiculo nao esta disponivel para reserva
  - `404 Not Found` - veiculo nao encontrado

---

#### `PUT /vehicles/{id}/return`

Registra a devolucao de um veiculo. Chama o metodo de dominio `Return()` e persiste a alteracao.

- **Autenticacao:** **requerida** (Bearer JWT)
- **Respostas:**
  - `204 No Content` - devolucao registrada com sucesso
  - `404 Not Found` - veiculo nao encontrado

---

### Categorias

Base URL: `/categories`

#### `GET /categories`

Retorna a lista de todas as categorias cadastradas.

- **Autenticacao:** nao requerida
- **Resposta:** `200 OK`

```json
[
  {
    "id": "557dc64b-e7c4-40b0-918f-4469be2c5d06",
    "name": "Hatch",
    "description": "Veiculos compactos com porta traseira integrada ao vidro, ideais para uso urbano.",
    "optionals": ["Ar-condicionado", "Direcao eletrica", "Central multimidia"]
  }
]
```

---

#### `GET /categories/{id}/vehicles`

Retorna todos os veiculos pertencentes a uma categoria especifica.

- **Autenticacao:** nao requerida
- **Respostas:**
  - `200 OK` - lista de veiculos da categoria
  - `404 Not Found` - categoria nao encontrada

---

## Autenticacao e Autorizacao

A API utiliza **Keycloak** como provedor de identidade. A validacao dos tokens e feita via **JWT Bearer**.

- O middleware baixa automaticamente o JWKS do endpoint `{Authority}/.well-known/openid-configuration` e valida assinatura, expiracao e audience de cada token recebido.
- Os endpoints de escrita (`POST /vehicles/{id}/reservation` e `PUT /vehicles/{id}/return`) exigem um token valido no header `Authorization: Bearer {token}`.
- Os endpoints de leitura (`GET`) sao publicos.

### Obtendo um token

```http
POST http://localhost:8080/realms/vehicle-service/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=vehicle-service-client&client_secret={secret}
```

---

## Banco de Dados

- **SGBD:** PostgreSQL
- **ORM:** Entity Framework Core 9 com provider Npgsql
- **Migrations:** geradas via `dotnet ef migrations` e aplicadas automaticamente na inicializacao da aplicacao

### Tabelas

| Tabela | Descricao |
|---|---|
| `categories` | Categorias de veiculos |
| `vehicles` | Veiculos com FK para `categories` |

### Seed de dados

Ao iniciar, a aplicacao verifica se existem categorias cadastradas. Caso nao existam, o `DatabaseSeeder` popula automaticamente o banco com 3 categorias (Hatch, Sedan e SUV) e veiculos de exemplo para cada uma.

### Aplicar migrations manualmente

```bash
dotnet ef database update
```

---

## Observabilidade

A aplicacao utiliza **OpenTelemetry** para rastreamento distribuido (tracing), exportando traces para um coletor compativel com OTLP (ex: **Jaeger**).

### Instrumentacoes ativas

| Instrumentacao | O que rastreia |
|---|---|
| `AspNetCore` | Todas as requisicoes HTTP recebidas |
| `HttpClient` | Todas as chamadas HTTP de saida |
| `EntityFrameworkCore` | Queries SQL executadas pelo EF Core |

### Exportador

Os traces sao exportados via **OTLP HTTP/protobuf** para o endpoint configurado em `OpenTelemetry:OtlpEndpoint` (padrao: `http://localhost:4318`).

Para visualizar os traces localmente, suba o Jaeger com suporte OTLP:

```bash
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4318:4318 \
  jaegertracing/all-in-one:latest
```

Acesse o Jaeger UI em: `http://localhost:16686`

---

## Logging

A aplicacao utiliza **Serilog** como provedor de logging, integrado ao sistema `ILogger` do ASP.NET Core.

### Configuracao

- **Bootstrap logger** - captura erros de inicializacao antes do host ser construido
- **Console sink** - todos os logs sao exibidos no console em formato estruturado
- **Request logging** - cada requisicao HTTP e registrada como uma unica linha de log via `UseSerilogRequestLogging()`
- **Flush no encerramento** - `Log.CloseAndFlushAsync()` e chamado no `finally` para garantir que nenhum log seja perdido

### Adicionando sinks via configuracao

Novos destinos de log (arquivo, Seq, etc.) podem ser adicionados diretamente no `appsettings.json` sem alterar codigo:

```json
"Serilog": {
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
  ]
}
```

### Mensagens de log por endpoint

| Endpoint | Nivel | Mensagem |
|---|---|---|
| `GET /vehicles` | Info | `Buscando todos os veiculos` / `{Count} veiculo(s) retornado(s)` |
| `GET /vehicles/{id}` | Info / Warning | `Buscando veiculo com id {VehicleId}` / `nao encontrado` |
| `POST /vehicles/{id}/reservation` | Info / Warning | `Iniciando reserva` / `nao disponivel` / `reservado com sucesso` |
| `PUT /vehicles/{id}/return` | Info / Warning | `Iniciando devolucao` / `nao encontrado` / `devolvido com sucesso` |
| `GET /categories` | Info | `Buscando todas as categorias` / `{Count} categoria(s) retornada(s)` |
| `GET /categories/{id}/vehicles` | Info / Warning | `Buscando veiculos da categoria` / `nao encontrada` |

---

## Documentacao da API

O Swagger UI esta disponivel em todos os ambientes em:

```
http://localhost:5005/swagger
```

O JSON da especificacao OpenAPI esta disponivel em:

```
http://localhost:5005/swagger/v1/swagger.json
```

O Swagger esta configurado para suportar autenticacao Bearer JWT - utilize o botao **Authorize** para informar o token antes de testar os endpoints protegidos.

---

## Execucao Local

### Pre-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) (para PostgreSQL, Keycloak e Jaeger)

### Subindo as dependencias

```bash
# PostgreSQL
docker run -d --name postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=vehicle_service_db \
  -p 5432:5432 \
  postgres:16

# Keycloak
docker run -d --name keycloak \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  -p 8080:8080 \
  quay.io/keycloak/keycloak:latest start-dev

# Jaeger
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4318:4318 \
  jaegertracing/all-in-one:latest
```

### Executando a API

```bash
cd fontes/vehicle-service
dotnet run
```

A API estara disponivel em:
- HTTP: `http://localhost:5005`
- HTTPS: `https://localhost:7185`
