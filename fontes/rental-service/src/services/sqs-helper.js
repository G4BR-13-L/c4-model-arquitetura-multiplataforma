const {
  CreateQueueCommand,
  GetQueueUrlCommand
} = require("@aws-sdk/client-sqs");
const { URL } = require("node:url");

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

function normalizeQueueUrl(queueUrl, endpointUrl) {
  if (!queueUrl || !endpointUrl) {
    return queueUrl;
  }

  try {
    const parsedQueueUrl = new URL(queueUrl);
    const parsedEndpointUrl = new URL(endpointUrl);

    parsedQueueUrl.protocol = parsedEndpointUrl.protocol;
    parsedQueueUrl.hostname = parsedEndpointUrl.hostname;
    parsedQueueUrl.port = parsedEndpointUrl.port;

    return parsedQueueUrl.toString();
  } catch (error) {
    return queueUrl;
  }
}

async function getQueueUrlOrCreate(sqsClient, queueName, logger, endpointUrl) {
  try {
    const response = await sqsClient.send(new GetQueueUrlCommand({
      QueueName: queueName
    }));
    return normalizeQueueUrl(response.QueueUrl, endpointUrl);
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

    return normalizeQueueUrl(response.QueueUrl, endpointUrl);
  }
}

module.exports = {
  getQueueUrlOrCreate,
  isQueueDoesNotExistError
};
