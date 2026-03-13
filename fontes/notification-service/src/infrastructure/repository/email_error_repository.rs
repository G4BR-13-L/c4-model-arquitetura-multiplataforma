use sqlx::{Pool, Postgres};
use crate::domain::email_error::EmailError;

#[derive(Clone)]
pub struct EmailErrorRepository {
    pool: Pool<Postgres>,
}

impl EmailErrorRepository {
    pub fn new(pool: Pool<Postgres>) -> Self {
        Self { pool }
    }

    pub async fn insert(&self, raw: &str, err: &str) -> Result<(), sqlx::Error> {
        sqlx::query!(
            "INSERT INTO t_erro_mensagem_email (raw_content, error_message) VALUES ($1, $2)",
            raw,
            err
        )
        .execute(&self.pool)
        .await?;
        Ok(())
    }

    pub async fn list_all(&self) -> Result<Vec<EmailError>, sqlx::Error> {
        sqlx::query_as!(
            EmailError,
            "SELECT * FROM t_erro_mensagem_email ORDER BY inserted_at DESC"
        )
        .fetch_all(&self.pool)
        .await
    }
}
