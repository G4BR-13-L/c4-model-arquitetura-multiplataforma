use actix_web::{HttpResponse, Responder, web};
use uuid::Uuid;

use crate::{application::state::AppState, domain::email::Email};

pub async fn find_by_id(state: web::Data<AppState>, path: web::Path<String>) -> impl Responder {
    let Ok(uuid) = Uuid::parse_str(&path) else {
        return HttpResponse::BadRequest().json(serde_json::json!({
            "error": "invalid_uuid_format",
            "message": "The provided identifier is not a valid UUID"
        }));
    };

    match state.email_service.find_by_uuid(uuid).await {
        Ok(Some(email)) => HttpResponse::Ok().json(email),

        Ok(None) => HttpResponse::NotFound().json(serde_json::json!({
            "error": "email_not_found",
            "message": "No email record found with the given identifier"
        })),

        Err(e) => {
            log::error!("Database error in find_by_id: {:?}", e);
            HttpResponse::InternalServerError().json(serde_json::json!({
                "error": "internal_server_error",
                "message": "An unexpected error occurred while retrieving the email"
            }))
        }
    }
}

pub async fn list_emails_5_recent_emails(state: web::Data<AppState>) -> impl Responder {
    match state.email_service.get_latest_5_emails().await {
        Ok(Some(emails)) => HttpResponse::Ok().json(emails),

        Ok(None) => HttpResponse::Ok().json(Vec::<Email>::new()),

        Err(e) => {
            log::error!("Database error in list_emails_5_recent_emails: {:?}", e);
            HttpResponse::InternalServerError().json(serde_json::json!({
                "error": "internal_server_error",
                "message": "An unexpected error occurred while fetching the recent emails list"
            }))
        }
    }
}
