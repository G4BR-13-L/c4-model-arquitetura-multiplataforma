async function readJsonBody(req) {
  const chunks = [];

  for await (const chunk of req) {
    chunks.push(chunk);
  }

  if (chunks.length === 0) {
    return {};
  }

  const raw = Buffer.concat(chunks).toString("utf8").trim();

  if (!raw) {
    return {};
  }

  try {
    return JSON.parse(raw);
  } catch {
    const error = new Error("Invalid JSON body.");
    error.statusCode = 400;
    throw error;
  }
}

function sendJson(res, statusCode, payload, headers = {}) {
  const body = payload === undefined ? "" : JSON.stringify(payload);
  const baseHeaders = {
    "content-type": "application/json; charset=utf-8"
  };

  if (payload !== undefined) {
    baseHeaders["content-length"] = Buffer.byteLength(body);
  }

  res.writeHead(statusCode, { ...baseHeaders, ...headers });
  res.end(body);
}

function sendEmpty(res, statusCode = 204, headers = {}) {
  res.writeHead(statusCode, headers);
  res.end();
}

module.exports = {
  readJsonBody,
  sendJson,
  sendEmpty
};
