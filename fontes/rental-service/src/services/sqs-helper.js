const {
  CreateQueueCommand,
  GetQueueUrlCommand
} = require("@aws-sdk/client-sqs");

function isQueueDoesNotExistError(error) {
  if (!error) {
    return false;
  }

  const name = String(error.name ?? "").trim();

  if (name === "QueueDoesNotExist" || name === "AWS.SimpleQueueService.NonExistentQueue") {
    return true;
  }

  const message = String(error.message ?? "");
  return /specified queue does not exist/i.test(message);
}

async function getQueueUrlOrCreate(sqsClient, queueName, logger) {
  try {
    const response = await sqsClient.send(new GetQueueUrlCommand({
      QueueName: queueName
    }));
    return response.QueueUrl;
  } catch (error) {
    if (!isQueueDoesNotExistError(error)) {
      throw error;
    }

    const response = await sqsClient.send(new CreateQueueCommand({
      QueueName: queueName
    }));

    if (logger && typeof logger.info === "function") {
      logger.info("sqs_queue_created", {
        queue_name: queueName
      });
    }

    return response.QueueUrl;
  }
}

module.exports = {
  getQueueUrlOrCreate,
  isQueueDoesNotExistError
};
