use aws_config::Region;
use aws_sdk_sqs::{Client, config::Credentials};

pub async fn create_sqs_client(endpoint_url: &str) -> Client {
    let config = aws_config::defaults(aws_config::BehaviorVersion::latest())
        .endpoint_url(endpoint_url)
        .region(Region::new("us-east-1"))
        .credentials_provider(Credentials::new("test", "test", None, None, "manual"))
        .load()
        .await;

    Client::new(&config)
}
