const crypto = require("node:crypto");

const PAYMENT_STATUS = {
  PENDING: "PENDING",
  CONFIRMED: "CONFIRMED",
  FAILED: "FAILED"
};

const RENTAL_STATUS = {
  PENDING: "PENDING",
  COMPLETED: "COMPLETED",
  CANCELLED: "CANCELLED"
};

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const DAY_IN_MS = 24 * 60 * 60 * 1000;

function isUuid(value) {
  return UUID_REGEX.test(value);
}

function toIsoDate(value, fieldName) {
  if (typeof value !== "string" || !value.trim()) {
    const error = new Error(`${fieldName} is required.`);
    error.statusCode = 400;
    throw error;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    const error = new Error(`${fieldName} must be a valid ISO 8601 date.`);
    error.statusCode = 400;
    throw error;
  }

  return date;
}

function calculateRentalDays(startDate, endDate) {
  const diff = endDate.getTime() - startDate.getTime();

  if (diff <= 0) {
    const error = new Error("end_date must be greater than start_date.");
    error.statusCode = 400;
    throw error;
  }

  return Math.ceil(diff / DAY_IN_MS);
}

function roundCurrency(value) {
  return Number(value.toFixed(2));
}

class RentalService {
  constructor(repository, vehicleServiceClient, eventPublisher, logger) {
    this.repository = repository;
    this.vehicleServiceClient = vehicleServiceClient;
    this.eventPublisher = eventPublisher;
    this.logger = logger;
  }

  listByUser(userId) {
    return this.repository.listByUserId(userId);
  }

  getByIdForUser(rentalId, userId) {
    return this.repository.getByIdForUser(rentalId, userId);
  }

  async createRental(authContext, input) {
    if (!isUuid(authContext.userId)) {
      const error = new Error("Authenticated user id is invalid.");
      error.statusCode = 401;
      throw error;
    }

    if (!isUuid(input.vehicle_id)) {
      const error = new Error("vehicle_id must be a valid UUID.");
      error.statusCode = 400;
      throw error;
    }

    const startDate = toIsoDate(input.start_date, "start_date");
    const endDate = toIsoDate(input.end_date, "end_date");
    const rentalDays = calculateRentalDays(startDate, endDate);

    const vehicle = await this.vehicleServiceClient.getVehicle(input.vehicle_id);

    if (!vehicle) {
      const error = new Error("Vehicle not found.");
      error.statusCode = 404;
      throw error;
    }

    if (!vehicle.available) {
      const error = new Error("Vehicle is not available.");
      error.statusCode = 409;
      throw error;
    }

    const reservationResult = await this.vehicleServiceClient.reserveVehicle(input.vehicle_id, authContext.token);

    if (!reservationResult.ok) {
      const error = new Error(reservationResult.message);
      error.statusCode = reservationResult.statusCode;
      throw error;
    }

    const totalAmount = roundCurrency(Number(vehicle.daily_price) * rentalDays);
    const rental = {
      id: crypto.randomUUID(),
      vehicle_id: input.vehicle_id,
      user_id: authContext.userId,
      start_date: startDate.toISOString(),
      end_date: endDate.toISOString(),
      total_amount: totalAmount,
      payment_status: PAYMENT_STATUS.PENDING,
      status: RENTAL_STATUS.PENDING,
      created_at: new Date().toISOString()
    };

    this.repository.create(rental);

    await this.eventPublisher.publishRentalCreated(rental);

    this.logger.info("rental_created", {
      rental_id: rental.id,
      user_id: rental.user_id,
      vehicle_id: rental.vehicle_id
    });

    return rental;
  }

  async handlePaymentEvent(event) {
    const rentalId = event.data?.rental_id;
    const status = event.data?.status;

    if (!rentalId || !status) {
      throw new Error("payment.confirmed event is invalid.");
    }

    const rental = this.repository.getById(rentalId);

    if (!rental) {
      this.logger.warn("payment_event_for_unknown_rental", {
        rental_id: rentalId
      });
      return;
    }

    if (status === PAYMENT_STATUS.CONFIRMED) {
      rental.payment_status = PAYMENT_STATUS.CONFIRMED;
      this.repository.update(rental);
      return;
    }

    if (status === PAYMENT_STATUS.FAILED) {
      rental.payment_status = PAYMENT_STATUS.FAILED;
      rental.status = RENTAL_STATUS.CANCELLED;
      this.repository.update(rental);

      try {
        await this.vehicleServiceClient.returnVehicle(rental.vehicle_id);
      } catch (error) {
        this.logger.warn("failed_to_return_vehicle_after_payment_failure", {
          rental_id: rental.id,
          vehicle_id: rental.vehicle_id,
          error: error.message
        });
      }
    }
  }
}

module.exports = {
  RentalService
};
