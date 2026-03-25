use aws_sdk_sqs::Client;
use crate::domain::sqs_email::SQSEmailMessage;
use crate::application::service::email_error_service::EmailErrorService;
use crate::application::service::email_service::EmailService;
use std::error::Error;
// Adicionamos 'instrument' e 'Span'
use tracing::{info, error, instrument, Span, info_span, Instrument};

// Instrumentamos a função principal para ver o ciclo de vida do Worker
#[instrument(skip(client, email_service, error_service), fields(queue = %queue_url))]
pub async fn start_consumer(
    client: Client,
    queue_url: String,
    email_service: EmailService,
    error_service: EmailErrorService,
) -> Result<(), Box<dyn Error>> {
    info!("Iniciando consumidor SQS");

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
                error!("Erro ao receber mensagens do SQS: {}. Tentando em 5s...", e);
                tokio::time::sleep(std::time::Duration::from_secs(5)).await;
                continue;
            }
        };

        if let Some(messages) = rcv_output.messages {
            for message in messages {
                // --- A MÁGICA DO JAEGER AQUI ---
                // Criamos um Span específico para esta mensagem.
                // Tudo o que acontecer dentro do 'async move' será agrupado no Jaeger.
                let process_span = info_span!("process_message", message_id = message.message_id());

                let body = message.body().unwrap_or("").to_string();
                let handle = message.receipt_handle().map(|h| h.to_string());

                // Clonamos os services para o bloco async
                let email_service = email_service.clone();
                let error_service = error_service.clone();
                let client = client.clone();
                let queue_url = queue_url.clone();

                // Processamos cada mensagem em seu próprio contexto de tracing
                async {
                    match serde_json::from_str::<SQSEmailMessage>(&body) {
                        Ok(mut sqs_data) => {
                            sqs_data.data.original_json = body.clone();
                            info!("Processando e-mail para: {}", sqs_data.data.recipient_email);

                            match email_service.insert(&sqs_data.data).await {
                                Ok(saved) => {
                                    info!("Sucesso: ID persistido {}", saved.id);
                                }
                                Err(e) => {
                                    error!("Erro DB: {}", e);
                                    let _ = error_service.log_error(&body, &e.to_string()).await;
                                }
                            }
                        }
                        Err(e) => {
                            error!("JSON Inválido: {}", e);
                            let _ = error_service.log_error(&body, &e.to_string()).await;
                        }
                    }

                    // Delete da fila
                    if let Some(h) = handle {
                        let _ = client
                            .delete_message()
                            .queue_url(&queue_url)
                            .receipt_handle(h)
                            .send()
                            .await;
                        info!("Mensagem removida da fila");
                    }
                }
                .instrument(process_span) // Acoplamos o Span ao bloco de execução
                .await;
            }
        }
    }
}
