import { createServer } from "node:http";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

export const screenshotPng = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=",
  "base64",
);

// Match Uri.UnescapeDataString: percent-decode, but keep literal '+' intact.
function decodeQuery(raw) {
  const entries = raw ? raw.split("&").map((item) => {
    const separator = item.indexOf("=");
    const key = separator < 0 ? item : item.slice(0, separator);
    const value = separator < 0 ? "" : item.slice(separator + 1);
    return [decodeURIComponent(key), decodeURIComponent(value)];
  }) : [];
  return { entries, query: Object.fromEntries(entries) };
}

export async function startFakeBridge() {
  const requests = [];
  const sockets = new Set();
  const timers = new Set();
  const server = createServer((req, res) => {
    const [path, rawQuery = ""] = req.url.split("?");
    const request = { method: req.method, path, rawQuery, ...decodeQuery(rawQuery) };
    requests.push(request);
    if (path === "/test/slow") {
      const timer = setTimeout(() => {
        timers.delete(timer);
        res.end('{"ok":true}');
      }, 500);
      timers.add(timer);
      return;
    }
    if (path === "/screenshot") {
      res.writeHead(200, { "Content-Type": "image/png" });
      res.end(screenshotPng);
      return;
    }
    if (path === "/test/raw") {
      res.end("not JSON");
      return;
    }
    if (path === "/test/http-error") {
      res.writeHead(409, { "Content-Type": "application/json" });
      res.end('{"error":"fixture conflict"}');
      return;
    }
    if (path === "/test/http-error-raw") {
      res.writeHead(503);
      res.end("unavailable");
      return;
    }
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify(request));
  });
  server.on("connection", (socket) => {
    sockets.add(socket);
    socket.on("close", () => sockets.delete(socket));
  });
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  const { port } = server.address();
  return {
    url: `http://127.0.0.1:${port}`,
    requests,
    async close() {
      for (const timer of timers) clearTimeout(timer);
      for (const socket of sockets) socket.destroy();
      await new Promise((resolve, reject) => server.close((err) => err ? reject(err) : resolve()));
    },
  };
}

// Never import the executable index: spawn it with an explicit ephemeral URL.
export async function startOfflineMcp(bridgeUrl) {
  const transport = new StdioClientTransport({
    command: process.execPath,
    args: [fileURLToPath(new URL("../dist/index.js", import.meta.url))],
    env: { ...process.env, CS2_BRIDGE_URL: bridgeUrl },
    stderr: "pipe",
  });
  transport.stderr?.on("data", () => {});
  const client = new Client({ name: "cs2-offline-tests", version: "1.0.0" });
  try {
    await client.connect(transport);
  } catch (err) {
    await transport.close();
    throw err;
  }
  return { client, close: () => client.close() };
}
