const {
  SQSClient,
  DeleteMessageCommand,
  ReceiveMessageCommand
} = require("@aws-sdk/client-sqs");
const { AwsQueryProtocol } = require("@aws-sdk/core/protocols");
const { getQueueUrlOrCreate } = require("./sqs-helper");

class PaymentEventConsumer {
  constructor(config, logger, rentalService) {
    this.config = config;
    this.logger = logger;
    this.rentalService = rentalService;
    this.queueUrl = null;
    this.stopped = false;
    this.timer = null;
    this.sqsClient = new SQSClient({
      region: config.awsRegion,
      endpoint: config.sqsEndpoint,
      protocol: AwsQueryProtocol
    });
  }

  start() {
    if (!this.config.paymentEventsEnabled) {
      this.logger.info("payment_event_consumer_disabled");
      return;
    }

    this.logger.info("payment_event_consumer_started", {
      queue_name: this.config.paymentConfirmedQueueName
    });

    this.scheduleNextPoll(0);
  }

  stop() {
    this.stopped = true;

    if (this.timer) {
      clearTimeout(this.timer);
    }
  }

  scheduleNextPoll(delayMs) {
    if (this.stopped) {
      return;
    }

    this.timer = setTimeout(() => {
      this.poll().catch((error) => {
        this.logger.warn("payment_event_poll_failed", {
          error: error.message
        });
        this.scheduleNextPoll(this.config.paymentPollingIntervalMs);
      });
    }, delayMs);
  }

  async poll() {
    const queueUrl = await this.getQueueUrl();
    const response = await this.sqsClient.send(new ReceiveMessageCommand({
      QueueUrl: queueUrl,
      MaxNumberOfMessages: 5,
      WaitTimeSeconds: 2
    }));

    for (const message of response.Messages ?? []) {
      const processed = await this.processMessage(queueUrl, message);

      if (processed && message.ReceiptHandle) {
        await this.sqsClient.send(new DeleteMessageCommand({
          QueueUrl: queueUrl,
          ReceiptHandle: message.ReceiptHandle
        }));
      }
    }

    this.scheduleNextPoll(this.config.paymentPollingIntervalMs);
  }

  async processMessage(queueUrl, message) {
    try {
      const event = JSON.parse(message.Body ?? "{}");

      if (event.event_type !== "payment.confirmed") {
        this.logger.warn("payment_event_ignored", {
          event_type: event.event_type ?? "unknown",
          queue_url: queueUrl
        });
        return true;
      }

      await this.rentalService.handlePaymentEvent(event);

      this.logger.info("payment_event_processed", {
        rental_id: event.data?.rental_id ?? null,
        status: event.data?.status ?? null
      });

      return true;
    } catch (error) {
      this.logger.warn("payment_event_processing_failed", {
        error: error.message,
        message_id: message.MessageId ?? null
      });
      return false;
    }
  }

  async getQueueUrl() {
    if (this.queueUrl) {
      return this.queueUrl;
    }

    this.queueUrl = await getQueueUrlOrCreate(
      this.sqsClient,
      this.config.paymentConfirmedQueueName,
      this.logger,
      this.config.sqsEndpoint
    );

    return this.queueUrl;
  }
}

module.exports = {
  PaymentEventConsumer
};
