const crypto = require("node:crypto");

function decodeBase64Url(value) {
  return Buffer.from(value, "base64url").toString("utf8");
}

function safeNowEpochSeconds() {
  return Math.floor(Date.now() / 1000);
}

class KeycloakClient {
  constructor(config, logger) {
    this.config = config;
    this.logger = logger;
    this.discoveryCache = null;
    this.discoveryLoadedAt = 0;
    this.jwksCache = null;
    this.jwksLoadedAt = 0;
    this.serviceToken = null;
    this.serviceTokenExpiresAt = 0;
  }

  getAllowedClientIds() {
    const configured = Array.isArray(this.config.keycloakAllowedClientIds)
      ? this.config.keycloakAllowedClientIds
      : [];
    const base = this.config.keycloakClientId ? [this.config.keycloakClientId] : [];
    return [...new Set([...configured, ...base])].filter(Boolean);
  }

  async authenticate(req) {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith("Bearer ")) {
      const error = new Error("Missing bearer token.");
      error.statusCode = 401;
      throw error;
    }

    const token = authHeader.slice("Bearer ".length).trim();
    const claims = await this.verifyToken(token);

    return {
      token,
      claims,
      userId: claims.sub
    };
  }

  async verifyToken(token) {
    const [encodedHeader, encodedPayload, encodedSignature] = token.split(".");

    if (!encodedHeader || !encodedPayload || !encodedSignature) {
      const error = new Error("Malformed JWT.");
      error.statusCode = 401;
      throw error;
    }

    let header;
    let payload;

    try {
      header = JSON.parse(decodeBase64Url(encodedHeader));
      payload = JSON.parse(decodeBase64Url(encodedPayload));
    } catch {
      const error = new Error("Invalid JWT payload.");
      error.statusCode = 401;
      throw error;
    }

    if (header.alg !== "RS256" || !header.kid) {
      const error = new Error("Unsupported JWT algorithm.");
      error.statusCode = 401;
      throw error;
    }

    const jwk = await this.getJwk(header.kid);

    if (!jwk) {
      const error = new Error("Signing key not found.");
      error.statusCode = 401;
      throw error;
    }

    const verifier = crypto.createVerify("RSA-SHA256");
    verifier.update(`${encodedHeader}.${encodedPayload}`);
    verifier.end();

    const publicKey = crypto.createPublicKey({
      key: jwk,
      format: "jwk"
    });

    const signature = Buffer.from(encodedSignature, "base64url");
    const validSignature = verifier.verify(publicKey, signature);

    if (!validSignature) {
      const error = new Error("Invalid JWT signature.");
      error.statusCode = 401;
      throw error;
    }

    this.validateClaims(payload);

    return payload;
  }

  validateClaims(claims) {
    const now = safeNowEpochSeconds();
    const issuer = claims.iss;

    if (issuer !== this.config.keycloakAuthority) {
      const error = new Error("Invalid token issuer.");
      error.statusCode = 401;
      throw error;
    }

    if (!claims.sub) {
      const error = new Error("Token subject is missing.");
      error.statusCode = 401;
      throw error;
    }

    if (typeof claims.exp === "number" && claims.exp <= now) {
      const error = new Error("Token expired.");
      error.statusCode = 401;
      throw error;
    }

    if (typeof claims.nbf === "number" && claims.nbf > now) {
      const error = new Error("Token not active yet.");
      error.statusCode = 401;
      throw error;
    }

    const allowedClientIds = this.getAllowedClientIds();

    if (!allowedClientIds.length) {
      return;
    }

    const audiences = Array.isArray(claims.aud) ? claims.aud : [claims.aud].filter(Boolean);
    const matchesAudience = audiences.some((aud) => allowedClientIds.includes(aud));
    const matchesAuthorizedParty = allowedClientIds.includes(claims.azp);

    if (!matchesAudience && !matchesAuthorizedParty) {
      const error = new Error("Token audience is not allowed.");
      error.statusCode = 401;
      throw error;
    }
  }

  async getServiceAccessToken() {
    if (!this.config.keycloakClientId || !this.config.keycloakClientSecret) {
      return null;
    }

    const now = safeNowEpochSeconds();

    if (this.serviceToken && this.serviceTokenExpiresAt - 30 > now) {
      return this.serviceToken;
    }

    const discovery = await this.getDiscovery();
    const body = new URLSearchParams({
      grant_type: "client_credentials",
      client_id: this.config.keycloakClientId,
      client_secret: this.config.keycloakClientSecret
    });

    const response = await fetch(discovery.token_endpoint, {
      method: "POST",
      headers: {
        "content-type": "application/x-www-form-urlencoded"
      },
      body
    });

    if (!response.ok) {
      this.logger.warn("failed_to_fetch_service_token", {
        status_code: response.status
      });
      return null;
    }

    const payload = await response.json();
    this.serviceToken = payload.access_token ?? null;
    this.serviceTokenExpiresAt = now + Number(payload.expires_in ?? 0);

    return this.serviceToken;
  }

  async getJwk(kid) {
    const jwks = await this.getJwks();
    return jwks.keys.find((key) => key.kid === kid) ?? null;
  }

  async getDiscovery() {
    const now = Date.now();

    if (this.discoveryCache && now - this.discoveryLoadedAt < this.config.jwksCacheTtlMs) {
      return this.discoveryCache;
    }

    const url = `${this.config.keycloakAuthority}/.well-known/openid-configuration`;
    const response = await fetch(url);

    if (!response.ok) {
      throw new Error(`Unable to load OpenID configuration from ${url}.`);
    }

    this.discoveryCache = await response.json();
    this.discoveryLoadedAt = now;
    return this.discoveryCache;
  }

  async getJwks() {
    const now = Date.now();

    if (this.jwksCache && now - this.jwksLoadedAt < this.config.jwksCacheTtlMs) {
      return this.jwksCache;
    }

    const discovery = await this.getDiscovery();
    const response = await fetch(discovery.jwks_uri);

    if (!response.ok) {
      throw new Error(`Unable to load JWKS from ${discovery.jwks_uri}.`);
    }

    this.jwksCache = await response.json();
    this.jwksLoadedAt = now;
    return this.jwksCache;
  }
}

module.exports = {
  KeycloakClient
};
