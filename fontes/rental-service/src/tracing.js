const { NodeSDK } = require("@opentelemetry/sdk-node");
const { AwsInstrumentation } = require("@opentelemetry/instrumentation-aws-sdk");
const { HttpInstrumentation } = require("@opentelemetry/instrumentation-http");
const { OTLPTraceExporter } = require("@opentelemetry/exporter-trace-otlp-grpc");
const { defaultResource, resourceFromAttributes } = require("@opentelemetry/resources");
const { SemanticResourceAttributes } = require("@opentelemetry/semantic-conventions");
const { diag, DiagConsoleLogger, DiagLogLevel } = require("@opentelemetry/api");

const config = require("./config");
const logger = require("./utils/logger");

const { openTelemetry } = config;

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ERROR);

if (!openTelemetry.enabled) {
  logger.info("telemetry.disabled", {
    reason: "OPEN_TELEMETRY_ENABLED=false"
  });

  module.exports = {};
  return;
}

const exporter = new OTLPTraceExporter({
  url: openTelemetry.otlpEndpoint
});

const sdk = new NodeSDK({
  resource: defaultResource().merge(
    resourceFromAttributes({
      [SemanticResourceAttributes.SERVICE_NAME]: openTelemetry.serviceName
    })
  ),
  traceExporter: exporter,
  instrumentations: [new HttpInstrumentation(), new AwsInstrumentation()],
  metricReaders: []
});

try {
  sdk.start();
  logger.info("telemetry.started", {
    endpoint: openTelemetry.otlpEndpoint
  });
} catch (error) {
  logger.error("telemetry.start_failed", {
    error: error.message
  });
}

let shuttingDown = false;
async function shutdownTracing() {
  if (shuttingDown) {
    return;
  }

  shuttingDown = true;
  try {
    await sdk.shutdown();
    logger.info("telemetry.shutdown");
  } catch (error) {
    logger.error("telemetry.shutdown_failed", {
      error: error.message
    });
  }
}

["SIGINT", "SIGTERM"].forEach((signal) => {
  process.on(signal, () => {
    shutdownTracing();
  });
});

module.exports = {
  shutdownTracing
};
