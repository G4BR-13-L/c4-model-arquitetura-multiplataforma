#![deny(clippy::unwrap_used)]
#![deny(clippy::expect_used)]

use actix_web::{
    App, HttpResponse, HttpServer,
    http::StatusCode,
    middleware::{ErrorHandlerResponse, ErrorHandlers},
    web,
};
use crate::infrastructure::sqs_client::create_sqs_client;
use dotenv::dotenv;
use log::{error, info};
use std::sync::Arc;
use tracing_actix_web::TracingLogger;
use tracing_subscriber::{
    EnvFilter,
    fmt::{self, time::ChronoLocal},
    layer::SubscriberExt,
    util::SubscriberInitExt,
}; // Importamos o Target
use notification_service::{
    application::{
        service::{email_error_service::EmailErrorService, email_service::EmailService},
        state::AppState,
    },
    infrastructure::{self, db::create_pool, http::routes},
    shared::settings::Settings,
};

#[actix_web::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenv().ok();

    let settings = Settings::new()?;

    let filter = EnvFilter::try_from_default_env().unwrap_or_else(|_| EnvFilter::new("info"));

    if !settings.show_time_tracing_logs {
        // Modo Produção / Sem tempo
        tracing_subscriber::registry()
            .with(filter)
            .with(
                fmt::layer()
                    .with_writer(std::io::stdout)
                    .with_ansi(settings.time_tracing_logs_with_ansi)
                    .without_time(),
            )
            .init();
    } else {
        // Modo Local / Com tempo
        tracing_subscriber::registry()
            .with(filter)
            .with(
                fmt::layer()
                    .with_writer(std::io::stdout)
                    .with_ansi(settings.time_tracing_logs_with_ansi)
                    .with_timer(ChronoLocal::rfc_3339()),
            )
            .init();
    }
    info!("Strating notification service Job Search...");

    let pool = create_pool(&settings.database_url).await?;

    let queue_url = settings.queue_url.clone();

    let args: Vec<String> = std::env::args().collect();
    if args.contains(&"--migrate-only".to_string()) {
        info!("Rodando migrations e encerrando...");
        sqlx::migrate!("./migrations").run(&pool).await?;
        return Ok(());
    }

    let sqs_client = create_sqs_client(&settings.sqs_endpoint_url).await;
    let email_service = EmailService::new(pool.clone());
    let email_error_service = EmailErrorService::new(pool.clone());

    tokio::spawn(async move {
        info!("Aguardando filas do LocalStack estarem prontas...");

        loop {
            {
                let result = infrastructure::worker::start_consumer(
                    sqs_client.clone(),
                    queue_url.clone(),
                    email_service.clone(),
                    email_error_service.clone(),
                )
                .await;

                if let Err(ref e) = result {
                    error!("Worker parou: {:?}. Tentando novamente em 5s...", e);
                }
            }
            tokio::time::sleep(std::time::Duration::from_secs(5)).await;
        }
    });

    let email_service = Arc::new(EmailService::new(pool.clone()));
    let email_error_service = Arc::new(EmailErrorService::new(pool.clone()));

    let state = AppState {
        pool: pool.clone(),
        settings,
        email_service,
        email_error_service,
    };

    HttpServer::new(move || {
        App::new()
            .app_data(web::Data::new(state.clone()))
            .wrap(TracingLogger::default())
            .wrap(
                ErrorHandlers::new().handler(StatusCode::INTERNAL_SERVER_ERROR, |res| {
                    tracing::error!("Erro 500 detectado no middleware");

                    let response = HttpResponse::InternalServerError()
                        .json(serde_json::json!({
                            "error": "internal_server_error",
                            "message": "unexpected error"
                        }))
                        .map_into_right_body();

                    Ok(ErrorHandlerResponse::Response(res.into_response(response)))
                }),
            )
            .configure(routes::config)
    })
    .bind(("0.0.0.0", 8080))?
    .run()
    .await?;

    Ok(())
}
