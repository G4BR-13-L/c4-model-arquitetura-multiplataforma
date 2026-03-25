use config::{Config, Environment, File};
use serde::Deserialize;

#[derive(Debug, Deserialize, Clone)]
pub struct DatabaseConfig {
    pub url: String,
}

#[derive(Debug, Deserialize, Clone)]
pub struct Settings {
    pub database_url: String,
    pub jwt_secret: String,
    pub sqs_endpoint_url: String,
    pub queue_url: String,
    pub show_time_tracing_logs: bool,
    pub time_tracing_logs_with_ansi: bool,
}

impl Settings {
    pub fn new() -> Result<Self, config::ConfigError> {
        let run_mode = std::env::var("APP_ENV").unwrap_or_else(|_| "dev".into());

        let s = Config::builder()
            .add_source(File::with_name("config/default"))
            .add_source(File::with_name(&format!("config/{run_mode}")).required(false))
            .add_source(Environment::default().convert_case(config::Case::Snake))
            .build()?;

        s.try_deserialize()
    }
}
