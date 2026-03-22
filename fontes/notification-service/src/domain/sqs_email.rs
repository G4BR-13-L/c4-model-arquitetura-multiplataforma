use chrono::{DateTime, NaiveDate, Utc};
use serde::{Deserialize, Serialize};
use sqlx::FromRow;
use uuid::Uuid;

use crate::domain::{
    email::Email,
    value_objects::{email_field::EmailField, name_field::NameField},
};

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct SQSEmailMessage {
    #[serde(default = "Uuid::new_v4")] // Gera um ID interno se quiser trackear a mensagem
    pub id: Uuid,

    pub event_type: String,

    pub occurred_at: DateTime<Utc>,

    pub data: Email,
}

impl std::fmt::Display for SQSEmailMessage {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        let id_short = &self.id.to_string()[..8];
        let date_str = self.occurred_at.format("%Y-%m-%d %H:%M:%S").to_string();

        let data_json = serde_json::to_string_pretty(&self)
            .unwrap_or_else(|_| "Erro ao serializar payload".to_string());

        write!(
            f,
            "\n┌─ SQS EMAIL MESSAGE CONSUMED [{id}] ──────────────────────────┐\n\
             │  Event type:    {event_type}\n\
             │  At:            {occurred_at}\n\
             ├───────────────────────────────────────────────────────┤\n\
             │  message_container: \n{data_json}\n\
             └───────────────────────────────────────────────────────┘",
            id = id_short,
            event_type = self.event_type,
            occurred_at = date_str,
            data_json = data_json
        )
    }
}
