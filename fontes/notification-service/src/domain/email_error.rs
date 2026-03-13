use serde::{Deserialize, Serialize};
use uuid::Uuid;
use chrono::{DateTime, Utc};
use sqlx::FromRow;

#[derive(Debug, Clone, FromRow, Serialize, Deserialize)]
pub struct EmailError {
    #[serde(default)]
    pub id: Uuid,
    #[sqlx(default)]
    pub raw_content: String,
    #[sqlx(default)]
    pub error_message: String,
    #[serde(skip_deserializing)]
    #[sqlx(default)]
    pub inserted_at: DateTime<Utc>,
}
