const {
  SQSClient,
  GetQueueUrlCommand,
  SendMessageCommand
} = require("@aws-sdk/client-sqs");

class RentalEventPublisher {
  constructor(config, logger) {
    this.logger = logger;
    this.queueName = config.rentalCreatedQueueName;
    this.queueUrl = null;
    this.sqsClient = new SQSClient({
      region: config.awsRegion,
      endpoint: config.sqsEndpoint
    });
  }

  async publishRentalCreated(rental) {
    const event = {
      event_type: "rental.created",
      occurred_at: new Date().toISOString(),
      data: {
        id: rental.id,
        vehicle_id: rental.vehicle_id,
        user_id: rental.user_id,
        start_date: rental.start_date,
        end_date: rental.end_date,
        total_amount: rental.total_amount,
        payment_status: rental.payment_status,
        status: rental.status
      }
    };

    try {
      const queueUrl = await this.getQueueUrl();
      await this.sqsClient.send(new SendMessageCommand({
        QueueUrl: queueUrl,
        MessageBody: JSON.stringify(event)
      }));

      this.logger.info("rental_created_event_published", {
        queue_name: this.queueName,
        rental_id: rental.id
      });
    } catch (error) {
      this.logger.warn("rental_created_event_publish_failed", {
        queue_name: this.queueName,
        rental_id: rental.id,
        error: error.message
      });
    }
  }

  async getQueueUrl() {
    if (this.queueUrl) {
      return this.queueUrl;
    }

    const response = await this.sqsClient.send(new GetQueueUrlCommand({
      QueueName: this.queueName
    }));

    this.queueUrl = response.QueueUrl;
    return this.queueUrl;
  }
}

module.exports = {
  RentalEventPublisher
};
