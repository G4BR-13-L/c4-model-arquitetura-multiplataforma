use sqlx::{PgPool, postgres::PgPoolOptions};

pub async fn create_pool(database_url: &str) -> Result<PgPool, sqlx::Error> {
    PgPoolOptions::new()
        .max_connections(50)
        .acquire_timeout(std::time::Duration::from_secs(10))
        .connect(database_url)
        .await
}
