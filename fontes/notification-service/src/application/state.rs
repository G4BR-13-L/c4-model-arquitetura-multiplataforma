use sqlx::PgPool;
use std::sync::Arc;

use crate::{
    application::service::{email_error_service::EmailErrorService, email_service::EmailService},
    shared::settings::Settings,
};

#[derive(Clone)]
pub struct AppState {
    pub pool: PgPool,
    pub settings: Settings,
    pub email_service: Arc<EmailService>,
    pub email_error_service: Arc<EmailErrorService>,
}
