use crate::domain::email_error::EmailError;
use crate::infrastructure::repository::email_error_repository::EmailErrorRepository;
use sqlx::{Pool, Postgres};
use tracing::{info, error};
use uuid::Uuid;

#[derive(Clone)]
pub struct EmailErrorService {
    repo: EmailErrorRepository,
}

impl EmailErrorService {
    #[must_use]
    pub fn new(pool: Pool<Postgres>) -> Self {
        Self {
            repo: EmailErrorRepository::new(pool),
        }
    }

    /// Registra um erro de processamento de e-mail no banco de dados.
    pub async fn log_error(&self, raw_content: &str, error_msg: &str) -> Result<(), sqlx::Error> {
        info!("Registrando erro de processamento. Causa: {}", error_msg);

        if let Err(e) = self.repo.insert(raw_content, error_msg).await {
            error!("Falha crítica ao persistir log de erro no banco: {}", e);
            return Err(e);
        }

        Ok(())
    }

    /// Lista todos os erros registrados para fins de auditoria ou reprocessamento manual.
    pub async fn get_all_errors(&self) -> Result<Vec<EmailError>, sqlx::Error> {
        info!("Buscando todos os registros de erro de e-mail");
        self.repo.list_all().await
    }

    /// Busca um erro específico (opcional, útil para debugging)
    pub async fn get_error_by_id(&self, id: Uuid) -> Result<Option<EmailError>, sqlx::Error> {
        // Você pode adicionar este método no repo se precisar
        todo!("Implementar busca por ID se necessário")
    }
}
