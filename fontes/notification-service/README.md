# Notification Service

Serviço em Rust para consumo e processamento de notificações via e-mail utilizando **AWS SQS** e **PostgreSQL**.

## 🚀 Como Executar

### **1. Requisitos Prévios**

* Docker e Docker Compose
* Rust (Toolchain 1.84+) - *Caso execute fora do Docker*
* AWS CLI (opcional, para testes manuais)

---

### **2. Execução via Docker (Recomendado)**

A forma mais rápida de subir todo o ambiente (App, Banco de Dados e LocalStack).

1. **Subir os containers:**
```bash
docker-compose up -d

```


2. **Verificar se a fila foi criada automaticamente:**
O script `init-sqs.sh` criará a fila no LocalStack assim que o container estiver pronto.
3. **Logs da aplicação:**
```bash
docker logs -f notification_service_app

```



---

### **3. Execução Local (Cargo/Rust)**

Para desenvolvimento ativo e debugging mais rápido.

1. **Subir apenas a infraestrutura:**
```bash
docker-compose up -d notification_service_postgres localstack

```


2. **Configurar variáveis de ambiente:**
Certifique-se de que o arquivo `config/dev.toml` ou seu `.env` aponte para `localhost`:
```env
DATABASE_URL=postgres://postgres:postgres@localhost:5453/notification_service
SQS_ENDPOINT=http://localhost:4566

```


3. **Rodar a aplicação:**
```bash
cargo run

```
---

### **🧪 Testando o Fluxo**

Para validar se o processamento e o tratamento de erros estão funcionando, utilize o script de teste incluso:

1. **Dar permissão de execução:**
```bash
chmod +x test_scripts/post-msgs.sh

```

2. **Executar o envio de mensagens:**
```bash
./test_scripts/post-msgs.sh

```

Este script enviará:

* ✅ **Mensagem Correta:** Será persistida em `t_email`.
* ❌ **Mensagem Malformada:** O conteúdo bruto será salvo em `t_erro_mensagem_email` para auditoria.

---

### **🛠 Estrutura do Projeto**

* `src/application`: Serviços de regra de negócio.
* `src/domain`: Entidades, Value Objects e lógica central.
* `src/infrastructure`: Implementações de SQS, Repositórios e Banco de Dados.
* `migrations`: Scripts SQL de inicialização do banco.