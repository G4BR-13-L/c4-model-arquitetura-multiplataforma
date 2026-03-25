require("./tracing");
const http = require("node:http");
const { URL } = require("node:url");

const config = require("./config");
const logger = require("./utils/logger");
const { readJsonBody, sendEmpty, sendJson } = require("./utils/http");
const { RentalRepository } = require("./repositories/rental-repository");
const { KeycloakClient } = require("./services/keycloak-client");
const { PaymentEventConsumer } = require("./services/payment-event-consumer");
const { RentalEventPublisher } = require("./services/rental-event-publisher");
const { RentalService } = require("./services/rental-service");
const { VehicleServiceClient } = require("./services/vehicle-service-client");

const repository = new RentalRepository();
const keycloakClient = new KeycloakClient(config, logger);
const vehicleServiceClient = new VehicleServiceClient(config.vehicleServiceBaseUrl, keycloakClient);
const eventPublisher = new RentalEventPublisher(config, logger);
const rentalService = new RentalService(repository, vehicleServiceClient, eventPublisher, logger);
const paymentEventConsumer = new PaymentEventConsumer(config, logger, rentalService);

const server = http.createServer(async (req, res) => {
  try {
    await routeRequest(req, res);
  } catch (error) {
    logger.error("request_failed", {
      method: req.method,
      url: req.url,
      status_code: error.statusCode ?? 500,
      error: error.message
    });

    sendJson(res, error.statusCode ?? 500, {
      message: error.message ?? "Internal server error."
    });
  }
});

async function routeRequest(req, res) {
  const requestUrl = new URL(req.url, `http://${req.headers.host ?? "localhost"}`);
  const pathname = trimTrailingSlash(requestUrl.pathname);
  const internalEventRentalId = getInternalRentalEventId(pathname);

  if (req.method === "GET" && pathname === "/health") {
    sendJson(res, 200, {
      service: "rental-service",
      status: "ok",
      timestamp: new Date().toISOString()
    });
    return;
  }

  if (req.method === "GET" && internalEventRentalId) {
    if (!eventPublisher.isCaptureEnabled()) {
      sendEmpty(res, 404);
      return;
    }

    const event = eventPublisher.getCapturedEvent(internalEventRentalId);
    if (!event) {
      sendEmpty(res, 404);
      return;
    }

    sendJson(res, 200, event);
    return;
  }

  if (req.method === "POST" && pathname === "/internal/events/payment") {
    if (!eventPublisher.isCaptureEnabled()) {
      sendEmpty(res, 404);
      return;
    }

    const body = await readJsonBody(req);
    const rentalId = body?.rental_id;
    const status = body?.status;

    if (!rentalId || !status) {
      sendJson(res, 400, {
        message: "rental_id and status are required."
      });
      return;
    }

    const event = {
      event_type: "payment.confirmed",
      occurred_at: new Date().toISOString(),
      data: {
        rental_id: rentalId,
        status
      }
    };

    await rentalService.handlePaymentEvent(event);
    sendEmpty(res, 204);
    return;
  }

  if (req.method === "POST" && isCreateRentalRoute(pathname)) {
    const authContext = await keycloakClient.authenticate(req);
    const body = await readJsonBody(req);
    const rental = await rentalService.createRental(authContext, body);

    sendJson(res, 201, rental, {
      location: `/rentals/${rental.id}`
    });
    return;
  }

  if (req.method === "GET" && isListRentalsRoute(pathname)) {
    const authContext = await keycloakClient.authenticate(req);
    const rentals = rentalService.listByUser(authContext.userId);
    sendJson(res, 200, rentals);
    return;
  }

  if (req.method === "GET") {
    const rentalId = getRentalIdFromPath(pathname);

    if (rentalId) {
      const authContext = await keycloakClient.authenticate(req);
      const rental = rentalService.getByIdForUser(rentalId, authContext.userId);

      if (!rental) {
        sendJson(res, 404, { message: "Rental not found." });
        return;
      }

      sendJson(res, 200, rental);
      return;
    }
  }

  sendEmpty(res, 404);
}

function trimTrailingSlash(pathname) {
  if (pathname.length > 1 && pathname.endsWith("/")) {
    return pathname.slice(0, -1);
  }

  return pathname;
}

function getInternalRentalEventId(pathname) {
  const matches = pathname.match(/^\/internal\/events\/rental-created\/([0-9a-f-]+)$/i);
  return matches?.[1] ?? null;
}

function isCreateRentalRoute(pathname) {
  return pathname === "/rentals" || pathname === "/v1/rental";
}

function isListRentalsRoute(pathname) {
  return pathname === "/rentals" || pathname === "/v1/rental";
}

function getRentalIdFromPath(pathname) {
  const matches = pathname.match(/^\/(?:rentals|v1\/rental)\/([0-9a-f-]+)$/i);
  return matches?.[1] ?? null;
}

server.listen(config.port, () => {
  logger.info("rental_service_started", {
    port: config.port,
    node_env: config.nodeEnv
  });

  paymentEventConsumer.start();
});

for (const signal of ["SIGINT", "SIGTERM"]) {
  process.on(signal, () => {
    logger.info("shutdown_requested", { signal });
    paymentEventConsumer.stop();

    server.close(() => {
      logger.info("server_stopped");
      process.exit(0);
    });
  });
}
