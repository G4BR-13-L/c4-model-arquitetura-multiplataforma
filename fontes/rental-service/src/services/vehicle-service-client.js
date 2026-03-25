class VehicleServiceClient {
  constructor(baseUrl, keycloakClient) {
    this.baseUrl = baseUrl.replace(/\/$/, "");
    this.keycloakClient = keycloakClient;
    this.vehiclesPath = `${this.baseUrl}/v1/vehicles`;
  }

  async getVehicle(vehicleId) {
    const response = await fetch(`${this.vehiclesPath}/${vehicleId}`);

    if (response.status === 404) {
      return null;
    }

    if (!response.ok) {
      throw new Error(`Vehicle service returned ${response.status} while fetching vehicle.`);
    }

    return response.json();
  }

  async reserveVehicle(vehicleId, userToken) {
    const token = userToken ?? await this.keycloakClient.getServiceAccessToken();
    const response = await fetch(`${this.vehiclesPath}/${vehicleId}/reservation`, {
      method: "POST",
      headers: this.createAuthHeaders(token)
    });

    if (response.status === 404) {
      return { ok: false, statusCode: 404, message: "Vehicle not found." };
    }

    if (response.status === 400) {
      const message = await this.tryReadText(response);
      return { ok: false, statusCode: 409, message: message || "Vehicle is not available." };
    }

    if (!response.ok) {
      throw new Error(`Vehicle service returned ${response.status} while reserving vehicle.`);
    }

    return { ok: true };
  }

  async returnVehicle(vehicleId) {
    const token = await this.keycloakClient.getServiceAccessToken();
    const response = await fetch(`${this.vehiclesPath}/${vehicleId}/return`, {
      method: "PUT",
      headers: this.createAuthHeaders(token)
    });

    if (response.status === 404) {
      return { ok: false, statusCode: 404 };
    }

    if (!response.ok) {
      throw new Error(`Vehicle service returned ${response.status} while returning vehicle.`);
    }

    return { ok: true };
  }

  createAuthHeaders(token) {
    if (!token) {
      return {};
    }

    return {
      authorization: `Bearer ${token}`
    };
  }

  async tryReadText(response) {
    try {
      return (await response.text()).trim();
    } catch {
      return "";
    }
  }
}

module.exports = {
  VehicleServiceClient
};
