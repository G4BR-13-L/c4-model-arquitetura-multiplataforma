#![deny(clippy::unwrap_used)]
#![deny(clippy::expect_used)]

use actix_web::{
    App, HttpResponse, HttpServer,
    http::StatusCode,
    middleware::{ErrorHandlerResponse, ErrorHandlers},
    web,
};
use opentelemetry::KeyValue;
use opentelemetry_sdk::{Resource, trace};
use tracing::info_span;
use crate::infrastructure::sqs_client::create_sqs_client;
use dotenv::dotenv;
use log::{error, info};
use std::sync::Arc;
use tracing_actix_web::TracingLogger;
use tracing_subscriber::{
    EnvFilter, Layer,
    fmt::{self, time::ChronoLocal},
    layer::SubscriberExt,
    util::SubscriberInitExt,
};
use notification_service::{
    application::{
        service::{email_error_service::EmailErrorService, email_service::EmailService},
        state::AppState,
    },
    infrastructure::{self, db::create_pool, http::routes},
    shared::settings::Settings,
};

use opentelemetry_otlp::WithExportConfig;

#[actix_web::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenv().ok();

    let settings = Settings::new()?;

    // --- 1. CONFIGURAÇÃO JAEGER/OTLP ---
    let tracer = opentelemetry_otlp::new_pipeline()
        .tracing()
        .with_exporter(
            opentelemetry_otlp::new_exporter()
                .tonic()
                .with_endpoint(settings.jaeger_url.clone()),
        )
        .with_trace_config(
            trace::config().with_resource(Resource::new(vec![KeyValue::new(
                "service.name",
                "notification-service",
            )])),
        )
        .install_batch(opentelemetry_sdk::runtime::Tokio)?;

    let telemetry = tracing_opentelemetry::layer().with_tracer(tracer);

    // 1. Configure a camada de console (FMT)
    let fmt_layer = fmt::layer()
        .with_writer(std::io::stdout)
        .with_ansi(settings.time_tracing_logs_with_ansi);

    // O segredo é importar 'use tracing_subscriber::Layer;' no topo do arquivo
    let fmt_layer = if settings.show_time_tracing_logs {
        fmt_layer.with_timer(ChronoLocal::rfc_3339()).boxed()
    } else {
        fmt_layer.without_time().boxed()
    };

    // --- 3. INICIALIZAÇÃO ÚNICA DO REGISTRY ---
    let filter = EnvFilter::try_from_default_env().unwrap_or_else(|_| EnvFilter::new("info"));

    tracing_subscriber::registry()
        .with(filter)
        .with(telemetry) // Envia para o Jaeger
        .with(fmt_layer) // Envia para o Console
        .init(); // <--- ÚNICO .init() do programa inteiro

    let root = info_span!("startup");
    let _enter = root.enter();
    info!("Testando conexão com Jaeger...");

    info!("Starting notification service with Jaeger Tracing and Console Logs...");

    // --- RESTO DA APLICAÇÃO ---
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
