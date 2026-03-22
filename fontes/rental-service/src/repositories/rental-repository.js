class RentalRepository {
  constructor() {
    this.rentals = new Map();
  }

  create(rental) {
    this.rentals.set(rental.id, rental);
    return rental;
  }

  listByUserId(userId) {
    return Array.from(this.rentals.values())
      .filter((rental) => rental.user_id === userId)
      .sort((left, right) => right.created_at.localeCompare(left.created_at));
  }

  getById(id) {
    return this.rentals.get(id) ?? null;
  }

  getByIdForUser(id, userId) {
    const rental = this.getById(id);

    if (!rental || rental.user_id !== userId) {
      return null;
    }

    return rental;
  }

  update(rental) {
    this.rentals.set(rental.id, rental);
    return rental;
  }
}

module.exports = {
  RentalRepository
};
