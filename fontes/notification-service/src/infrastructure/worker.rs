use aws_sdk_sqs::Client;
use crate::domain::sqs_email::SQSEmailMessage;
use crate::{application::service::email_error_service::EmailErrorService, domain::email::Email};
use crate::application::service::email_service::EmailService;
use std::error::Error;
use std::fmt::Display;
use tracing::{info, error};

pub async fn start_consumer(
    client: Client,
    queue_url: String,
    email_service: EmailService,
    error_service: EmailErrorService,
) -> Result<(), Box<dyn Error>> {
    info!("Iniciando consumidor SQS em: {}", queue_url);

    loop {
        let rcv_result = client
            .receive_message()
            .queue_url(&queue_url)
            .max_number_of_messages(10)
            .wait_time_seconds(20)
            .send()
            .await;

        let rcv_output = match rcv_result {
            Ok(output) => output,
            Err(e) => {
                error!(
                    "Erro ao receber mensagens do SQS (LocalStack offline?): {}. Tentando novamente em 5s...",
                    e
                );
                tokio::time::sleep(std::time::Duration::from_secs(5)).await;
                continue;
            }
        };

        if let Some(messages) = rcv_output.messages {
            for message in messages {
                let body = message.body().unwrap_or("");
                info!("DEBUG: Payload recebido do SQS: {}", body); // Adicione esta linha

                match serde_json::from_str::<SQSEmailMessage>(body) {
                    Ok(mut sqs_email_message_data) => {
                        // Atribui o JSON original para auditoria conforme sua struct
                        sqs_email_message_data.data.original_json = body.to_string();

                        info!("Consumindo mensagem");
                        info!("{sqs_email_message_data}");

                        // 2. Persiste via Service (que já faz o find_or_create)
                        match email_service.insert(&sqs_email_message_data.data).await {
                            Ok(saved) => {
                                info!(
                                    "Email processado e persistido! ID: {}. Enviando...",
                                    saved.id
                                );
                                // papercut envio de email
                                // Simulação de envio: log de "Enviou e-mail"
                                info!(
                                    "LOG: E-mail enviado com sucesso para {}",
                                    saved.recipient_email
                                );
                            }
                            Err(e) => {
                                error!("Erro de banco ao persistir e-mail válido: {}", e);
                                let _ = error_service
                                    .log_error(body, &format!("DB Error: {}", e))
                                    .await;
                            }
                        }
                    }
                    Err(e) => {
                        error!("Falha no parse do JSON ou Validação: {}", e);
                        // 3. Se o JSON for inválido, persiste na tabela de erro
                        if let Err(db_err) = error_service.log_error(body, &e.to_string()).await {
                            error!(
                                "ERRO CRÍTICO: Não foi possível salvar o log de erro no banco: {}",
                                db_err
                            );
                        }
                    }
                }

                // 4. Deleta da fila (sempre, para não entrar em loop se o erro for persistente)
                if let Some(handle) = message.receipt_handle() {
                    let _ = client
                        .delete_message()
                        .queue_url(&queue_url)
                        .receipt_handle(handle)
                        .send()
                        .await;
                }
            }
        }
    }
}
