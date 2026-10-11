import assert from "node:assert/strict";
import test from "node:test";
import { startFakeBridge, startOfflineMcp } from "./fake-bridge.mjs";

const name = "Campus + East & 50% / 東京";

test("shared encoder preserves spaces, literal plus, punctuation and Unicode", async () => {
  const { encodeQuery } = await import("../dist/bridge-client.js");
  assert.equal(encodeQuery(new URLSearchParams({ name })),
    "name=Campus%20%2B%20East%20%26%2050%25%20%2F%20%E6%9D%B1%E4%BA%AC");
  assert.equal(encodeQuery(new URLSearchParams([["tag", "a b"], ["tag", "+"], ["", ""]])),
    "tag=a%20b&tag=%2B&=");
  assert.equal(encodeQuery(new URLSearchParams()), "");
});

test("old and newer stdio adapters preserve names, arrays, negatives and optional values", async () => {
  const bridge = await startFakeBridge();
  let mcp;
  try {
    mcp = await startOfflineMcp(bridge.url);
    const saved = await mcp.client.callTool({ name: "cs2_save_game", arguments: { name } });
    assert.equal(saved.isError, undefined);
    assert.equal(JSON.parse(saved.content[0].text).query.name, name);
    const line = await mcp.client.callTool({ name: "cs2_create_transit_line", arguments: {
      type: "bus", name, stops: [{ index: 123, version: 2 }, { x: -25.5, z: -10 }],
      snapRadius: 5, color: "#12AB34", force: true,
    } });
    assert.equal(line.isError, undefined);
    assert.deepEqual(JSON.parse(line.content[0].text).query, {
      type: "bus", stops: "123:2;-25.5,-10", name, color: "#12AB34", snapRadius: "5", force: "true",
    });
    await mcp.client.callTool({ name: "cs2_set_simulation", arguments: { paused: false, speed: 0 } });
    assert.deepEqual(bridge.requests.at(-1).query, { speed: "0", paused: "false" });
    assert.equal(bridge.requests.length, 3);
    assert.ok(bridge.requests.every((request) => !request.rawQuery.includes("+")));
  } finally {
    try { await mcp?.close(); } finally { await bridge.close(); }
  }
});

test("JSON client handles encoded queries, trailing base slashes and raw legacy payloads", async () => {
  const { createBridgeClient } = await import("../dist/bridge-client.js");
  const bridge = await startFakeBridge();
  try {
    const client = createBridgeClient({ baseUrl: `${bridge.url}///` });
    const result = await client.getJson("/echo", new URLSearchParams({ name, x: "-1.25" }));
    assert.deepEqual(result.query, { name, x: "-1.25" });
    assert.deepEqual(await client.getJson("/test/raw"), { raw: "not JSON" });
    assert.equal(bridge.requests[1].rawQuery, "");
    await assert.rejects(client.getJson("/test/http-error"), { message: "fixture conflict" });
    await assert.rejects(client.getJson("/test/http-error-raw"), { message: "bridge returned HTTP 503" });
  } finally {
    await bridge.close();
  }
});

test("JSON client preserves transport errors and honors an injected fetch", async () => {
  const { createBridgeClient } = await import("../dist/bridge-client.js");
  const bridge = await startFakeBridge();
  try {
    const client = createBridgeClient({ baseUrl: bridge.url, fetchImpl: async () => {
      throw new Error("fixture offline");
    } });
    await assert.rejects(client.getJson("/echo"), {
      message: `Cannot reach the CS2 bridge at ${bridge.url} (fixture offline). ` +
        "Make sure Cities: Skylines II is running and the CS2MCP mod is enabled.",
    });
    assert.equal(bridge.requests.length, 0);
  } finally {
    await bridge.close();
  }
});

test("JSON client applies the configured request deadline", async () => {
  const { createBridgeClient } = await import("../dist/bridge-client.js");
  const bridge = await startFakeBridge();
  try {
    const client = createBridgeClient({ baseUrl: bridge.url, timeoutMs: 50 });
    await assert.rejects(client.getJson("/test/slow"), /Cannot reach the CS2 bridge/);
  } finally {
    await bridge.close();
  }
});
