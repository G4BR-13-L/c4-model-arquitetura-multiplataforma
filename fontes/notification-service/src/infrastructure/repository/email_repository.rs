use sqlx::{Pool, Postgres};
use uuid::Uuid;

use crate::domain::value_objects::email_field::EmailField;
use crate::domain::value_objects::name_field::NameField;
use crate::domain::email::Email;

#[derive(Clone)]
pub struct EmailRepository {
    pool: Pool<Postgres>,
}

impl EmailRepository {
    #[must_use]
    pub const fn new(pool: Pool<Postgres>) -> Self {
        Self { pool }
    }

    pub async fn insert(&self, email: &Email) -> Result<Email, sqlx::Error> {
        let row = sqlx::query_as!(
            Email,
            r#"
            INSERT INTO t_email (
                sender_name, sender_email, recipient_email, recipient_name,
                subject, content, original_json, status
            )
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
            RETURNING
                id,
                sender_name as "sender_name: NameField",
                sender_email as "sender_email: EmailField",
                recipient_email as "recipient_email: EmailField",
                recipient_name as "recipient_name: NameField",
                subject, content, original_json, status, inserted_at
            "#,
            email.sender_name.to_string(),
            email.sender_email.to_string(),
            email.recipient_email.to_string(),
            email.recipient_name.to_string(),
            email.subject,
            email.content,
            email.original_json,
            email.status
        )
        .fetch_one(&self.pool)
        .await?;

        Ok(row)
    }

    pub async fn find_by_uuid(&self, id: Uuid) -> Result<Option<Email>, sqlx::Error> {
        sqlx::query_as!(
            Email,
            r#"
            SELECT
                id,
                sender_name as "sender_name: NameField",
                sender_email as "sender_email: EmailField",
                recipient_email as "recipient_email: EmailField",
                recipient_name as "recipient_name: NameField",
                subject, content, original_json, status, inserted_at
            FROM t_email
            WHERE id = $1
            "#,
            id
        )
        .fetch_optional(&self.pool)
        .await
    }

    pub async fn delete(&self, id: Uuid) -> Result<(), sqlx::Error> {
        sqlx::query!("DELETE FROM t_email WHERE id = $1", id)
            .execute(&self.pool)
            .await?;
        Ok(())
    }

    pub async fn get_latest_5_emails(&self) -> Result<Vec<Email>, sqlx::Error> {
        let emails = sqlx::query_as!(
            Email,
            r#"
            SELECT
                id,
                sender_name as "sender_name: NameField",
                sender_email as "sender_email: EmailField",
                recipient_email as "recipient_email: EmailField",
                recipient_name as "recipient_name: NameField",
                subject, content, original_json, status, inserted_at
            FROM t_email
            ORDER BY inserted_at DESC
            LIMIT 5
            "#
        )
        .fetch_all(&self.pool)
        .await?;

        Ok(emails)
    }
}
