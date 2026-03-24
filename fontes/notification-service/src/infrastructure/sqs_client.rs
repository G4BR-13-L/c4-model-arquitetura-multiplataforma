use aws_config::Region;
use aws_sdk_sqs::{Client, config::Credentials};

pub async fn create_sqs_client() -> Client {
    // Usamos o nome do serviço definido no docker-compose
    let endpoint_url = "http://localstack:4566";

    let config = aws_config::defaults(aws_config::BehaviorVersion::latest())
        .region(Region::new("us-east-1"))
        .credentials_provider(Credentials::new("test", "test", None, None, "localstack"))
        .endpoint_url(endpoint_url)
        .load()
        .await;

    // O SDK v1.x já gerencia o roteamento corretamente
    // quando o endpoint_url é fornecido.
    Client::new(&config)
}
