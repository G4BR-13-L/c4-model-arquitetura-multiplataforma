use actix_web::{HttpResponse, ResponseError};
use serde::Serialize;
use std::fmt;

#[derive(Debug)]
pub enum ApiError {
    BadRequest(String),
    Unauthorized(String),
    NotFound(String),
    Conflict(String),
    Internal(String),
}

#[derive(Serialize)]
struct ErrorResponse {
    error: String,
    message: String,
}

impl fmt::Display for ApiError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            ApiError::BadRequest(msg)
            | ApiError::Unauthorized(msg)
            | ApiError::NotFound(msg)
            | ApiError::Conflict(msg)
            | ApiError::Internal(msg) => write!(f, "{msg}"),
        }
    }
}

impl ResponseError for ApiError {
    fn error_response(&self) -> HttpResponse {
        match self {
            ApiError::BadRequest(msg) => HttpResponse::BadRequest().json(ErrorResponse {
                error: "bad_request".into(),
                message: msg.clone(),
            }),

            ApiError::Unauthorized(msg) => HttpResponse::Unauthorized().json(ErrorResponse {
                error: "unauthorized".into(),
                message: msg.clone(),
            }),

            ApiError::NotFound(msg) => HttpResponse::NotFound().json(ErrorResponse {
                error: "not_found".into(),
                message: msg.clone(),
            }),

            ApiError::Conflict(msg) => HttpResponse::Conflict().json(ErrorResponse {
                error: "conflict".into(),
                message: msg.clone(),
            }),

            ApiError::Internal(msg) => HttpResponse::InternalServerError().json(ErrorResponse {
                error: "internal_server_error kkkkk".into(),
                message: msg.clone(),
            }),
        }
    }
}

impl From<sqlx::Error> for ApiError {
    fn from(err: sqlx::Error) -> Self {
        log::error!("Database error: {:?}", err);
        ApiError::Internal("database_error".into())
    }
}

impl From<jsonwebtoken::errors::Error> for ApiError {
    fn from(_: jsonwebtoken::errors::Error) -> Self {
        ApiError::Unauthorized("invalid_token".into())
    }
}

impl From<reqwest::Error> for ApiError {
    fn from(err: reqwest::Error) -> Self {
        log::error!("HTTP Client error: {:?}", err);
        ApiError::Internal(format!("external_api_error: {}", err))
    }
}

impl From<std::env::VarError> for ApiError {
    fn from(err: std::env::VarError) -> Self {
        log::error!("Environment variable error: {:?}", err);
        ApiError::Internal("missing_configuration_env".into())
    }
}
