const assert = require("node:assert/strict");
const { test } = require("node:test");
const { randomUUID } = require("node:crypto");
const {
  SQSClient,
  ReceiveMessageCommand,
  GetQueueUrlCommand,
  CreateQueueCommand,
  DeleteMessageCommand
} = require("@aws-sdk/client-sqs");

const ENV = {
  rentalServiceUrl: process.env.RENTAL_SERVICE_URL ?? "http://localhost:7003",
  userServiceUrl: process.env.USER_SERVICE_URL ?? "http://localhost:7001",
  vehicleServiceUrl: process.env.VEHICLE_SERVICE_URL ?? "http://localhost:7002",
  paymentServiceUrl: process.env.PAYMENT_SERVICE_URL ?? "http://localhost:3005",
  sqsEndpoint: process.env.SQS_ENDPOINT ?? "http://localhost:4566",
  awsRegion: process.env.AWS_REGION ?? "us-east-1",
  awsAccessKeyId: process.env.AWS_ACCESS_KEY_ID ?? "test",
  awsSecretAccessKey: process.env.AWS_SECRET_ACCESS_KEY ?? "test",
  rentalCreatedQueueName: process.env.RENTAL_CREATED_QUEUE_NAME ?? "rental_created_fifo",
  paymentConfirmedQueueName: process.env.PAYMENT_CONFIRMED_QUEUE_NAME ?? "payment_confirmed_fifo"
};

const sqsClient = new SQSClient({
  region: ENV.awsRegion,
  endpoint: ENV.sqsEndpoint,
  credentials: {
    accessKeyId: ENV.awsAccessKeyId,
    secretAccessKey: ENV.awsSecretAccessKey
  }
});

let rentalQueueUrl;
let paymentQueueUrl;

const WAIT_INTERVAL_MS = 1_500;
const MAX_WAIT_MS = 120_000;

const VEHICLE_BASE_PATH = process.env.VEHICLE_SERVICE_BASE_PATH ?? "/v1/vehicles";

const servicesToCheck = [
  { url: ENV.rentalServiceUrl, path: "/health" },
  { url: ENV.userServiceUrl, path: "/v1/users" },
  { url: ENV.vehicleServiceUrl, path: VEHICLE_BASE_PATH },
  { url: ENV.paymentServiceUrl, path: "/payments" }
];

function buildUrl(base, path = "") {
  const trimmedBase = base.replace(/\/+$/, "");
  if (!path) {
    return trimmedBase;
  }
  if (path.startsWith("/")) {
    return `${trimmedBase}${path}`;
  }
  return `${trimmedBase}/${path}`;
}

async function waitForService(baseUrl, { path = "", timeout = MAX_WAIT_MS } = {}) {
  const deadline = Date.now() + timeout;
  const targetUrl = buildUrl(baseUrl, path);

  while (Date.now() < deadline) {
    try {
      const response = await fetch(targetUrl, { method: "GET" });
      if (response.ok || response.status >= 400) {
        return;
      }
    } catch (error) {
      // ignore until service is available
    }
    await sleep(WAIT_INTERVAL_MS);
  }

  throw new Error(`Service ${targetUrl} did not become available after ${timeout}ms`);
}

async function rawRequest(url, { method = "GET", headers = {}, body } = {}) {
  const effectiveHeaders = {
    accept: "application/json",
    ...headers
  };

  if (body && !Object.keys(effectiveHeaders).some((key) => key.toLowerCase() === "content-type")) {
    effectiveHeaders["content-type"] = "application/json";
  }

  const response = await fetch(url, {
    method,
    headers: effectiveHeaders,
    body: body ? JSON.stringify(body) : undefined
  });

  const text = await response.text();
  let parsed = null;

  if (text) {
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = text;
    }
  }

  return {
    response,
    body: parsed,
    text
  };
}

async function requestJson(url, options = {}) {
  const { response, body, text } = await rawRequest(url, options);

  if (!response.ok) {
    const bodyMessage = body?.message ?? body ?? text ?? "unknown";
    throw new Error(`Request to ${url} failed (${response.status}): ${bodyMessage}`);
  }

  return body;
}

async function createTestUser() {
  const suffix = randomUUID().slice(0, 8);
  const username = `rental-e2e-${suffix}`;
  const password = `Test#${suffix}`;
  const payload = {
    first_name: `E2E ${suffix}`,
    last_name: "Tester",
    email: `${username}@example.com`,
    username,
    password,
    document_number: `${Date.now().toString().slice(-11)}`
  };

  await requestJson(buildUrl(ENV.userServiceUrl, "/v1/users"), {
    method: "POST",
    body: payload
  });

  return { username, password };
}

async function login(username, password) {
  const payload = await requestJson(buildUrl(ENV.userServiceUrl, "/v1/auth/token"), {
    method: "POST",
    body: { userName: username, password }
  });

  if (!payload?.access_token) {
    throw new Error("User token not returned");
  }

  return payload.access_token;
}

async function createUserAndToken() {
  const credentials = await createTestUser();
  const token = await login(credentials.username, credentials.password);
  return { ...credentials, token };
}

async function listVehicles() {
  const vehicles = await requestJson(buildUrl(ENV.vehicleServiceUrl, VEHICLE_BASE_PATH), {
    method: "GET"
  });

  if (!Array.isArray(vehicles)) {
    throw new Error("Vehicle service returned invalid list");
  }

  return vehicles;
}

async function returnVehicle(vehicleId, token) {
  const { response, body, text } = await rawRequest(buildUrl(ENV.vehicleServiceUrl, `${VEHICLE_BASE_PATH}/${vehicleId}/return`), {
    method: "PUT",
    headers: { authorization: `Bearer ${token}` }
  });

  if (response.status === 400) {
    return;
  }

  if (!response.ok) {
    const message = body?.message ?? body ?? text ?? "unknown";
    throw new Error(`Returning vehicle ${vehicleId} failed (${response.status}): ${message}`);
  }
}

async function tryReleaseReservedVehicle(vehicles, token) {
  if (!token) {
    return;
  }

  const reserved = vehicles.find((vehicle) => vehicle.available === false);
  if (!reserved) {
    return;
  }

  await returnVehicle(reserved.id, token);
}

async function pickAvailableVehicle(token) {
  let vehicles = await listVehicles();
  let available = vehicles.find((vehicle) => vehicle.available);

  if (!available && token) {
    await tryReleaseReservedVehicle(vehicles, token);
    vehicles = await listVehicles();
    available = vehicles.find((vehicle) => vehicle.available);
  }

  if (!available) {
    throw new Error("No available vehicle found");
  }

  return available;
}

function buildRentalPayload(vehicleId, overrides = {}) {
  const startDate = new Date(Date.now() + 24 * 60 * 60 * 1000);
  const endDate = new Date(startDate.getTime() + 2 * 24 * 60 * 60 * 1000);
  return {
    vehicle_id: vehicleId,
    start_date: startDate.toISOString(),
    end_date: endDate.toISOString(),
    ...overrides
  };
}

async function postRental(vehicleId, token, overrides = {}) {
  const payload = buildRentalPayload(vehicleId, overrides);
  return await rawRequest(buildUrl(ENV.rentalServiceUrl, "/v1/rentals"), {
    method: "POST",
    headers: token ? { authorization: `Bearer ${token}` } : {},
    body: payload
  });
}

async function createRental(vehicleId, token) {
  const { response, body } = await postRental(vehicleId, token);

  if (!response.ok) {
    const errorMessage = body?.message ?? body ?? "unknown";
    throw new Error(`Request to ${buildUrl(ENV.rentalServiceUrl, "/v1/rentals")} failed (${response.status}): ${errorMessage}`);
  }

  return body;
}

async function getRental(rentalId, token) {
  return await requestJson(buildUrl(ENV.rentalServiceUrl, `/v1/rentals/${rentalId}`), {
    method: "GET",
    headers: { authorization: `Bearer ${token}` }
  });
}

async function waitForRental(rentalId, token, predicate, { timeout = MAX_WAIT_MS, interval = WAIT_INTERVAL_MS } = {}) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const rental = await getRental(rentalId, token);
    if (predicate(rental)) {
      return rental;
    }
    await sleep(interval);
  }
  throw new Error(`Rental ${rentalId} did not satisfy predicate within ${timeout}ms`);
}

async function fetchVehicle(vehicleId) {
  return await requestJson(buildUrl(ENV.vehicleServiceUrl, `${VEHICLE_BASE_PATH}/${vehicleId}`), {
    method: "GET"
  });
}

async function waitForVehicle(vehicleId, predicate, options = {}) {
  return await waitForCondition(
    `vehicle ${vehicleId}`,
    async () => {
      const vehicleData = await fetchVehicle(vehicleId);
      return predicate(vehicleData) ? vehicleData : null;
    },
    options
  );
}

async function getRentals(token) {
  return await requestJson(buildUrl(ENV.rentalServiceUrl, "/v1/rentals"), {
    method: "GET",
    headers: { authorization: `Bearer ${token}` }
  });
}

async function fetchRentalRaw(rentalId, token) {
  return await rawRequest(buildUrl(ENV.rentalServiceUrl, `/v1/rentals/${rentalId}`), {
    method: "GET",
    headers: token ? { authorization: `Bearer ${token}` } : {}
  });
}

async function waitForCondition(name, callback, { timeout = MAX_WAIT_MS, interval = WAIT_INTERVAL_MS } = {}) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const result = await callback();
    if (result) {
      return result;
    }
    await sleep(interval);
  }
  throw new Error(`${name} did not meet condition within ${timeout}ms`);
}

async function ensureQueueExists(name) {
  try {
    const response = await sqsClient.send(new GetQueueUrlCommand({ QueueName: name }));
    return response.QueueUrl;
  } catch (error) {
    if (!isQueueDoesNotExist(error)) {
      throw error;
    }
    const response = await sqsClient.send(new CreateQueueCommand({ QueueName: name }));
    return response.QueueUrl;
  }
}

function isQueueDoesNotExist(error) {
  const code = String(error?.name ?? "").toLowerCase();
  const message = String(error?.message ?? "").toLowerCase();
  return code.includes("queue" ) && message.includes("exists") ||
    message.includes("does not exist") ||
    message.includes("nonexistent queue");
}

async function purgeQueue(queueUrl) {
  if (!queueUrl) {
    return;
  }

  while (true) {
    const response = await sqsClient.send(new ReceiveMessageCommand({
      QueueUrl: queueUrl,
      MaxNumberOfMessages: 10,
      WaitTimeSeconds: 1
    }));

    if (!response.Messages || response.Messages.length === 0) {
      break;
    }

    await Promise.all(response.Messages.map((message) => deleteMessage(queueUrl, message.ReceiptHandle)));
  }
}

async function deleteMessage(queueUrl, receiptHandle) {
  if (!receiptHandle) {
    return;
  }
  await sqsClient.send(new DeleteMessageCommand({ QueueUrl: queueUrl, ReceiptHandle: receiptHandle }));
}

async function expectRentalCreatedEvent(rentalId, { timeout = MAX_WAIT_MS, interval = WAIT_INTERVAL_MS } = {}) {
  const deadline = Date.now() + timeout;
  const eventUrl = buildUrl(ENV.rentalServiceUrl, `/internal/events/rental-created/${rentalId}`);

  while (Date.now() < deadline) {
    try {
      const { response, body } = await rawRequest(eventUrl, { method: "GET" });
      if (response.status === 200) {
        return body;
      }
    } catch (error) {
      // service might not be ready yet; continue polling
    }

    await sleep(interval);
  }

  throw new Error(`Did not receive rental.created event for ${rentalId} within ${timeout}ms`);
}

async function sendPaymentEvent(rentalId, status) {
  const { response, body } = await rawRequest(
    buildUrl(ENV.rentalServiceUrl, "/internal/events/payment"),
    {
      method: "POST",
      body: {
        rental_id: rentalId,
        status
      }
    }
  );

  if (!response.ok) {
    const errorMessage = body?.message ?? body ?? "unknown";
    throw new Error(
      `Payment event ${status} for ${rentalId} failed (${response.status}): ${errorMessage}`
    );
  }
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

test.before(async () => {
  await Promise.all(servicesToCheck.map((service) => waitForService(service.url, { path: service.path })));

  rentalQueueUrl = await ensureQueueExists(ENV.rentalCreatedQueueName);
  paymentQueueUrl = await ensureQueueExists(ENV.paymentConfirmedQueueName);

  await purgeQueue(rentalQueueUrl);
  await purgeQueue(paymentQueueUrl);
});

test.beforeEach(async () => {
  await purgeQueue(rentalQueueUrl);
  await purgeQueue(paymentQueueUrl);
});

const defaultTimeout = 20000;

test("rental creation publishes an event and honors payment.confirmed", { timeout: defaultTimeout }, async () => {
  const { username, password } = await createTestUser();
  console.log("⏳ rental + payment flow: usuário criado", username);
  const token = await login(username, password);
  console.log("🔐 token obtido, escolhendo veículo disponível");
  const vehicle = await pickAvailableVehicle(token);
  console.log("🚗 veículo", vehicle.id, "selecionado, criando locação");
  const rental = await createRental(vehicle.id, token);

  const event = await expectRentalCreatedEvent(rental.id);
  console.log("📬 evento rental.created recebido para", rental.id);
  assert.equal(event.event_type, "rental.created");
  assert.equal(event.data.id, rental.id);
  assert.equal(event.data.vehicle_id, rental.vehicle_id);
  assert.equal(event.data.user_id, rental.user_id);
  assert.equal(event.data.total_amount, rental.total_amount);
  assert.equal(event.data.payment_status, rental.payment_status);
  assert.equal(event.data.status, rental.status);
  assert.equal(event.data.start_date, rental.start_date);
  assert.equal(event.data.end_date, rental.end_date);

  await sendPaymentEvent(rental.id, "CONFIRMED");
  console.log("✉️ payment.confirmed -> status CONFIRMED");

  const confirmedRental = await waitForRental(rental.id, token, (value) => value.payment_status === "CONFIRMED");
  assert.equal(confirmedRental.payment_status, "CONFIRMED");

  const reservedVehicle = await fetchVehicle(vehicle.id);
  assert.equal(reservedVehicle.available, false, "Vehicle should stay reserved after payment success");
});

test("payment.failure cancels the rental and returns the vehicle", { timeout: defaultTimeout }, async () => {
  const { username, password } = await createTestUser();
  console.log("⏳ payment failure: criando usuário", username);
  const token = await login(username, password);
  console.log("🔐 token pronto, escolhendo veículo");
  const vehicle = await pickAvailableVehicle(token);
  console.log("🚗 criando locação para falha");
  const rental = await createRental(vehicle.id, token);

  await expectRentalCreatedEvent(rental.id);
  console.log("📬 evento rental.created recebido, enviando payment.failure");
  await sendPaymentEvent(rental.id, "FAILED");

  const failedRental = await waitForRental(
    rental.id,
    token,
    (value) => value.payment_status === "FAILED" && value.status === "CANCELLED"
  );
  assert.equal(failedRental.payment_status, "FAILED");
  assert.equal(failedRental.status, "CANCELLED");

  const returnedVehicle = await waitForVehicle(vehicle.id, (vehicleData) => vehicleData.available === true);
  assert.equal(returnedVehicle.available, true);
});

test("POST /v1/rentals retorna Location e o corpo esperado", async () => {
  const { token } = await createUserAndToken();
  console.log("✅ validando Location/resposta de POST /v1/rentals");
  const vehicle = await pickAvailableVehicle(token);

  const { response, body } = await postRental(vehicle.id, token);
  assert.equal(response.status, 201);
  assert.equal(response.headers.get("location"), `/v1/rentals/${body.id}`);
  assert.equal(body.vehicle_id, vehicle.id);
  assert.equal(body.payment_status, "PENDING");
  assert.equal(body.status, "PENDING");
  assert.ok(body.total_amount > 0);
  assert.ok(body.id);
});

test("POST /v1/rentals rejeita payloads inválidos", async () => {
  const { token } = await createUserAndToken();
  console.log("⚠️ testando payloads inválidos no POST /v1/rentals");
  const invalidVehicle = await postRental(undefined, token);
  assert.equal(invalidVehicle.response.status, 400);
  assert.ok(
    invalidVehicle.body?.message?.includes("vehicle_id must be a valid UUID.")
  );

  const vehicle = await pickAvailableVehicle(token);
  const invalidDate = await postRental(vehicle.id, token, { start_date: "" });
  assert.equal(invalidDate.response.status, 400);
  assert.ok(
    invalidDate.body?.message?.includes("start_date is required.")
  );
});

test("GET /v1/rentals lista apenas as locações do usuário", async () => {
  const { token } = await createUserAndToken();
  console.log("📋 validando GET /v1/rentals para o usuário");
  const vehicle = await pickAvailableVehicle(token);
  const rental = await createRental(vehicle.id, token);

  const rentals = await getRentals(token);
  assert.ok(Array.isArray(rentals));

  const found = rentals.find((item) => item.id === rental.id);
  assert.ok(found);
  assert.equal(found.payment_status, "PENDING");
});

test("GET /v1/rentals/{id} exige autenticação e isola usuários", async () => {
  const { token: ownerToken } = await createUserAndToken();
  console.log("🔐 testando GET /v1/rentals/{id} com e sem token");
  const { token: otherToken } = await createUserAndToken();
  const vehicle = await pickAvailableVehicle(ownerToken);
  const rental = await createRental(vehicle.id, ownerToken);

  const ownerDetail = await getRental(rental.id, ownerToken);
  assert.equal(ownerDetail.id, rental.id);

  const anonymous = await fetchRentalRaw(rental.id);
  assert.equal(anonymous.response.status, 401);

  const otherUser = await fetchRentalRaw(rental.id, otherToken);
  assert.equal(otherUser.response.status, 404);
});
