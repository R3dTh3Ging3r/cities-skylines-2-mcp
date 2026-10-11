import assert from "node:assert/strict";
import test from "node:test";
import { createServer } from "node:http";
import { createBridgeClient } from "../dist/bridge-client.js";
import { startOfflineMcp } from "./fake-bridge.mjs";

const baseUrl = "http://fixture.invalid";
function clientFor(response) {
  let calls = 0;
  const client = createBridgeClient({ baseUrl, fetchImpl: async () => {
    calls++;
    if (response instanceof Error) throw response;
    return response;
  } });
  return { client, calls: () => calls };
}

test("typed failures retain legacy BridgeError construction and instanceof compatibility", async () => {
  const { BridgeError } = await import("../dist/bridge-client.js");
  assert.equal(new BridgeError("legacy diagnostic").message, "legacy diagnostic");
  const fixture = clientFor(new Response('{"error":"fixture rejection"}', { status: 409 }));
  await assert.rejects(fixture.client.getJson("/build/road"), (error) => error instanceof BridgeError);
});

test("typed HTTP rejection preserves native reasons, status and operation identity without retrying", async () => {
  const fixture = clientFor(new Response(JSON.stringify({ error: "native writer busy", errorData: {
    code: "BRIDGE_BUSY", message: "native writer busy", retryable: true, outcome: "notApplied",
    nativeErrors: ["Tool already active", "Cannot acquire writer"], operationId: "op-123",
  } }), { status: 409 }));
  await assert.rejects(fixture.client.getJson("/build/road"), (error) => {
    assert.equal(error.name, "BridgeClientError");
    assert.deepEqual(error.details, { code: "BRIDGE_BUSY", message: "native writer busy", retryable: true,
      outcome: "notApplied", httpStatus: 409, nativeErrors: ["Tool already active", "Cannot acquire writer"], operationId: "op-123" });
    return true;
  });
  assert.equal(fixture.calls(), 1);
});

test("malformed success is a protocol error with uncertain outcome", async () => {
  const fixture = clientFor(new Response("not JSON", { status: 200 }));
  await assert.rejects(fixture.client.getJson("/build/place"), (error) => {
    assert.equal(error.details.code, "PROTOCOL_ERROR");
    assert.equal(error.details.httpStatus, 200);
    assert.equal(error.details.outcome, "unknown");
    return true;
  });
  assert.equal(fixture.calls(), 1);
});

test("aborted write has unknown outcome and is never automatically retried", async () => {
  const fixture = clientFor(new DOMException("fixture timeout", "AbortError"));
  await assert.rejects(fixture.client.getJson("/city/taxes/set"), (error) => {
    assert.equal(error.details.code, "OUTCOME_UNKNOWN");
    assert.equal(error.details.outcome, "unknown");
    assert.equal(error.details.retryable, false);
    assert.match(error.message, /fixture timeout/);
    return true;
  });
  assert.equal(fixture.calls(), 1);
});

test("legacy HTTP rejection remains readable without claiming no effects", async () => {
  const fixture = clientFor(new Response('{"error":"bad tax category"}', { status: 400 }));
  await assert.rejects(fixture.client.getJson("/city/taxes/set"), (error) => {
    assert.equal(error.message, "bad tax category");
    assert.equal(error.details.code, "INVALID_ARGUMENT");
    assert.equal(error.details.outcome, "unknown");
    return true;
  });
});

test("server wait timeout preserves unknown outcome despite an HTTP response", async () => {
  const fixture = clientFor(new Response(JSON.stringify({ error: "simulation wait expired", errorData: {
    code: "OUTCOME_UNKNOWN", message: "simulation wait expired", retryable: false, outcome: "unknown",
  } }), { status: 503 }));
  await assert.rejects(fixture.client.getJson("/game/save"), (error) => {
    assert.equal(error.details.code, "OUTCOME_UNKNOWN");
    assert.equal(error.details.outcome, "unknown");
    assert.equal(error.details.httpStatus, 503);
    return true;
  });
});

test("non-JSON failures retain a bounded diagnostic excerpt and binary path rejects them", async () => {
  for (const method of ["getJson", "getResponse"]) {
    const fixture = clientFor(new Response("native unavailable " + "x".repeat(2000), { status: 503 }));
    await assert.rejects(fixture.client[method]("/screenshot"), (error) => {
      assert.match(error.message, /bridge returned HTTP 503.*native unavailable/);
      assert.ok(error.message.length < 700);
      assert.equal(error.details.httpStatus, 503);
      assert.equal(error.details.outcome, "unknown");
      return true;
    });
  }
});

test("response-body transport failures are typed and preserve uncertainty", async () => {
  const fixture = clientFor({ ok: true, status: 200, text: async () => { throw new Error("body disconnected"); } });
  await assert.rejects(fixture.client.getJson("/build/road"), (error) => {
    assert.equal(error.details.code, "TRANSPORT_UNAVAILABLE");
    assert.equal(error.details.outcome, "unknown");
    assert.match(error.message, /body disconnected/);
    return true;
  });
});

test("MCP errors carry typed data in readable JSON with isError", async () => {
  const { errorResult, jsonResult } = await import("../dist/tool-results.js");
  const fixture = clientFor(new Response(JSON.stringify({ error: "blocked by native validation", errorData: {
    code: "NATIVE_REJECTED", message: "blocked by native validation", retryable: false, outcome: "notApplied",
  } }), { status: 422 }));
  try { await fixture.client.getJson("/build/place"); assert.fail("expected rejection"); }
  catch (error) {
    const result = errorResult(error);
    assert.equal(result.isError, true);
    const payload = JSON.parse(result.content[0].text);
    assert.equal(payload.error, "blocked by native validation");
    assert.equal(payload.errorData.code, "NATIVE_REJECTED");
    assert.equal(payload.errorData.outcome, "notApplied");
    assert.deepEqual(result.structuredContent, payload);
  }
  assert.deepEqual(JSON.parse(jsonResult({ ok: true }).content[0].text), { ok: true });
  const unknown = JSON.parse(errorResult(new Error("unexpected local failure")).content[0].text);
  assert.equal(unknown.errorData.outcome, "unknown");
});

test("upgraded and old-server road interruptions at HTTP409 remain unknown and non-retryable", async () => {
  const message = "road replacement interrupted because another tool became active; check cs2_list_roads before retrying";
  for (const payload of [{ error: message }, { error: message, errorData: {
    code: "BRIDGE_BUSY", message, retryable: false, outcome: "unknown", httpStatus: 409,
  } }]) {
    const fixture = clientFor(new Response(JSON.stringify(payload), { status: 409 }));
    await assert.rejects(fixture.client.getJson("/roads/replace"), (error) => {
      assert.deepEqual(error.details, { code: "BRIDGE_BUSY", message, retryable: false,
        outcome: "unknown", httpStatus: 409 });
      return true;
    });
    assert.equal(fixture.calls(), 1);
  }
});

test("legacy server exceptions and malformed typed errors never claim no effects", async () => {
  for (const [status, payload] of [[500, { error: "native failure after apply" }],
    [409, { error: "invalid typed response", errorData: { outcome: "cancelled" } }]]) {
    const fixture = clientFor(new Response(JSON.stringify(payload), { status }));
    await assert.rejects(fixture.client.getJson("/build/road"), (error) => {
      assert.equal(error.details.outcome, "unknown");
      assert.equal(error.details.retryable, false);
      return true;
    });
  }
});

test("offline stdio transmits native diagnostics and uncertain outcome to the MCP client", async () => {
  const wire = { error: "simulation thread did not respond within 10s", errorData: {
    code: "OUTCOME_UNKNOWN", message: "simulation thread did not respond within 10s",
    retryable: false, outcome: "unknown", httpStatus: 503,
    nativeErrors: ["fixture wait expired"], operationId: "fixture-op",
  } };
  const requests = [];
  const server = createServer((request, response) => {
    requests.push(request.url);
    response.writeHead(503, { "Content-Type": "application/json" });
    response.end(JSON.stringify(wire));
  });
  await new Promise((resolve, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", resolve); });
  let mcp;
  try {
    mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    const result = await mcp.client.callTool({ name: "cs2_ping", arguments: {} });
    assert.equal(result.isError, true);
    assert.deepEqual(JSON.parse(result.content[0].text), wire);
    assert.deepEqual(result.structuredContent, wire);
    assert.deepEqual(requests, ["/ping"]);
  } finally {
    try { await mcp?.close(); } finally { await new Promise((resolve, reject) => server.close((error) => error ? reject(error) : resolve())); }
  }
});
