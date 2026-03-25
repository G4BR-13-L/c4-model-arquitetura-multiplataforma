function getBoolean(value, defaultValue) {
  if (value === undefined) {
    return defaultValue;
  }

  return String(value).toLowerCase() === "true";
}

function parseList(value) {
  if (!value) {
    return [];
  }

  return value.split(",").map((item) => item.trim()).filter(Boolean);
}

module.exports = {
  port: Number(process.env.PORT ?? 8080),
  nodeEnv: process.env.NODE_ENV ?? "development",
  vehicleServiceBaseUrl: process.env.VEHICLE_SERVICE_BASE_URL ?? "http://localhost:7002",
  keycloakAuthority: process.env.KEYCLOAK_AUTHORITY ?? "http://localhost:7000/realms/master",
  keycloakClientId: process.env.KEYCLOAK_CLIENT_ID ?? "",
  keycloakClientSecret: process.env.KEYCLOAK_CLIENT_SECRET ?? "",
  keycloakAllowedClientIds: parseList(process.env.KEYCLOAK_ALLOWED_CLIENT_IDS),
  awsRegion: process.env.AWS_REGION ?? "us-east-1",
  sqsEndpoint: process.env.SQS_ENDPOINT ?? "http://localhost:4566",
  rentalCreatedQueueName: process.env.RENTAL_CREATED_QUEUE_NAME ?? "rental_created_fifo",
  paymentConfirmedQueueName: process.env.PAYMENT_CONFIRMED_QUEUE_NAME ?? "payment_confirmed_fifo",
  paymentEventsEnabled: getBoolean(process.env.PAYMENT_EVENTS_ENABLED, true),
  paymentPollingIntervalMs: Number(process.env.PAYMENT_POLLING_INTERVAL_MS ?? 5000),
  jwksCacheTtlMs: Number(process.env.JWKS_CACHE_TTL_MS ?? 300000)
};
