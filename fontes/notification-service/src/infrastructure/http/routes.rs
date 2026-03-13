use actix_web::web;

use crate::infrastructure::http::handlers::email_handler;

pub fn config(cfg: &mut web::ServiceConfig) {
    cfg.service(
        web::scope("/email")
            .route("/{id}", web::get().to(email_handler::find_by_id))
            .route(
                "",
                web::get().to(email_handler::list_emails_5_recent_emails),
            ),
    );
}
