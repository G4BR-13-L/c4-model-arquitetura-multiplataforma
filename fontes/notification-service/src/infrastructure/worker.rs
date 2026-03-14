use aws_sdk_sqs::Client;
use crate::{application::service::email_error_service::EmailErrorService, domain::email::Email};
use crate::application::service::email_service::EmailService;
use std::error::Error;
use tracing::{info, error};

pub async fn start_consumer(
    client: Client,
    queue_url: String,
    email_service: EmailService,
    error_service: EmailErrorService,
) -> Result<(), Box<dyn Error>> {
    info!("Iniciando consumidor SQS em: {}", queue_url);

    loop {
        let rcv_output = client
            .receive_message()
            .queue_url(&queue_url)
            .max_number_of_messages(10)
            .wait_time_seconds(20)
            .send()
            .await?;

        if let Some(messages) = rcv_output.messages {
            for message in messages {
                let body = message.body().unwrap_or("");

                // 1. Tenta deserializar e validar
                match serde_json::from_str::<Email>(body) {
                    Ok(mut email_data) => {
                        // Atribui o JSON original para auditoria conforme sua struct
                        email_data.original_json = body.to_string();

                        // 2. Persiste via Service (que já faz o find_or_create)
                        match email_service.insert(&email_data).await {
                            Ok(saved) => {
                                info!(
                                    "Email processado e persistido! ID: {}. Enviando...",
                                    saved.id
                                );
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
