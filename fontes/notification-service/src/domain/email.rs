use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use sqlx::FromRow;
use uuid::Uuid;

use crate::domain::value_objects::{email_field::EmailField, name_field::NameField};

#[derive(Debug, Clone, FromRow, Default, Serialize, Deserialize)]
pub struct Email {
    #[serde(default = "Uuid::new_v4")]
    #[sqlx(default)]
    pub id: Uuid,

    pub sender_email: EmailField,
    pub recipient_email: EmailField,
    pub sender_name: NameField,
    pub recipient_name: NameField,
    pub subject: String,
    pub content: String,

    #[serde(default)] // Garante que o Serde não quebre se não vier no JSON
    #[sqlx(default)]
    pub original_json: String,

    #[serde(default = "default_status")] // Define um status padrão
    #[sqlx(default)]
    pub status: String,

    #[serde(skip_deserializing)]
    #[sqlx(default)]
    pub inserted_at: DateTime<Utc>,
}

fn default_status() -> String {
    "PENDING".to_string()
}

impl std::fmt::Display for Email {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        let status_icon = match self.status.to_lowercase().as_str() {
            "sent" => "✅",
            "failed" => "❌",
            "pending" => "⏳",
            _ => "✉️",
        };

        write!(
            f,
            "\n┌─ {icon}  EMAIL REPORT [{id}] ──────────────────────────┐\n\
             │  From:    {s_name} <{s_email}>\n\
             │  To:      {r_name} <{r_email}>\n\
             │  Subject: {subject}\n\
             ├───────────────────────────────────────────────────────┤\n\
             │  Status:  {status}\n\
             │  At:      {date}\n\
             └───────────────────────────────────────────────────────┘",
            icon = status_icon,
            id = &self.id.to_string()[..8],
            s_name = self.sender_name,
            s_email = self.sender_email,
            r_name = self.recipient_name,
            r_email = self.recipient_email,
            subject = self.subject,
            status = self.status.to_uppercase(),
            date = self.inserted_at.format("%Y-%m-%d %H:%M:%S")
        )
    }
}
