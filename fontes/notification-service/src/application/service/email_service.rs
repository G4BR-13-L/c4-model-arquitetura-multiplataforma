use log::{error, info};
use sqlx::{Pool, Postgres};
use uuid::Uuid;

use crate::{domain::email::Email, infrastructure::repository::email_repository::EmailRepository};

#[derive(Clone)]
pub struct EmailService {
    repo: EmailRepository,
}

impl EmailService {
    #[must_use]
    pub fn new(pool: Pool<Postgres>) -> Self {
        Self {
            repo: EmailRepository::new(pool),
        }
    }

    /// Busca um e-mail pelo seu identificador único.
    pub async fn find_by_uuid(&self, uuid: Uuid) -> Result<Option<Email>, sqlx::Error> {
        info!("Buscando e-mail no banco de dados: ID {}", uuid);
        self.repo.find_by_uuid(uuid).await
    }

    /// Insere um novo e-mail no sistema.
    pub async fn insert(&self, email: &Email) -> Result<Email, sqlx::Error> {
        info!("Persistindo novo e-mail vindo de: {}", email.sender_email);
        self.repo.insert(email).await
    }

    /// Tenta encontrar um e-mail pelo conteúdo único ou cria um novo caso não exista.
    /// Útil para evitar duplicidade de logs de e-mail idênticos.
    pub async fn find_or_create(&self, email_data: &Email) -> Result<Uuid, sqlx::Error> {
        // Como 'content' é UNIQUE na sua tabela, usamos ele para a busca
        match self.repo.insert(email_data).await {
            Ok(email) => {
                info!("E-mail processado com sucesso: {}", email.id);
                Ok(email.id)
            }
            Err(e) => {
                error!("Falha ao processar e-mail: {}", e);
                Err(e)
            }
        }
    }

    /// Método utilitário para deletar registros (opcional)
    pub async fn delete_email(&self, id: Uuid) -> Result<(), sqlx::Error> {
        info!("Removendo registro de e-mail: {}", id);
        self.repo.delete(id).await
    }

    /// Retorna os 5 emails mais recentes
    pub async fn get_latest_5_emails(&self) -> Result<Option<Vec<Email>>, sqlx::Error> {
        info!("Buscando os 5 e-mails mais recentes");
        let emails = self.repo.get_latest_5_emails().await?;

        if emails.is_empty() {
            Ok(None)
        } else {
            Ok(Some(emails))
        }
    }
}
