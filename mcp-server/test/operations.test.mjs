import assert from "node:assert/strict";
import test from "node:test";
import { createServer } from "node:http";
import { once } from "node:events";
import { createBridgeClient } from "../dist/bridge-client.js";
const id = "b6f2a37a-5676-40ec-8b56-3553b32c28e4";
const command = { operationId: id, world: { bootId: "boot", epoch: 1 }, leaseId: "lease", name: "cs2_build_road", args: { x: 1 } };
test("lost_response_keeps_caller_id", async () => {
  let calls = 0;
  const bridge = createBridgeClient({ baseUrl: "http://127.0.0.1:8642", fetchImpl: async () => {
    calls++; throw new Error("socket closed after dispatch");
  }});
  assert.equal(typeof bridge.submit, "function", "v2 submit behavior is missing");
  await assert.rejects(bridge.submit(command), error => {
    assert.equal(error.details.operationId, id); assert.equal(error.details.outcome, "unknown");
    assert.equal(error.details.retryable, false); return true;
  });
  assert.equal(calls, 1); assert.equal(command.operationId, id);
});
test("submit_status_and_cancel_use_fixed_v2_routes_and_json_body", async () => {
  const calls = []; const server = createServer(async (req, res) => {
    let body = ""; for await (const part of req) body += part;
    calls.push({ method: req.method, url: req.url, body, type: req.headers['content-type'] });
    res.setHeader("content-type", "application/json"); res.end(JSON.stringify({ operationId: id, phase: "queued" }));
  });
  server.listen(0, "127.0.0.1"); await once(server, "listening");
  try {
    const bridge = createBridgeClient({ baseUrl: `http://127.0.0.1:${server.address().port}` });
    assert.equal(typeof bridge.submit, "function", "v2 submit behavior is missing");
    assert.equal((await bridge.submit(command)).operationId, id);
    await bridge.operation(id); await bridge.cancelOperation(id);
    assert.deepEqual(calls.map(c => [c.method,c.url]), [["POST","/v2/commands"],["GET",`/v2/operations/${id}`],["POST",`/v2/operations/${id}/cancel`]]);
    assert.deepEqual(JSON.parse(calls[0].body), command); assert.equal(calls[0].type,"application/json");
  } finally { server.closeAllConnections(); server.close(); await once(server,"close"); }
});
test("unknown_submit_results_preserve_id_for_invalid_json_and_http_errors", async () => {
  for (const response of [new Response("not JSON"), new Response("lost", {status:503})]) {
    const bridge = createBridgeClient({baseUrl:"http://127.0.0.1:8642", fetchImpl:async()=>response});
    assert.equal(typeof bridge.submit,"function","v2 submit behavior is missing");
    await assert.rejects(bridge.submit(command), e => e.details.operationId === id && !e.details.retryable);
  }
});
test("v2_client_refuses_nonloopback_and_invalid_ids_without_fetch", async () => {
  let calls = 0;
  const bridge = createBridgeClient({baseUrl:"http://example.com",fetchImpl:async()=>{calls++;return new Response("{}");}});
  assert.equal(typeof bridge.submit,"function","v2 submit behavior is missing");
  await assert.rejects(bridge.submit(command)); assert.equal(calls,0);
  const local = createBridgeClient({baseUrl:"http://127.0.0.1",fetchImpl:async()=>{calls++;return new Response("{}");}});
  await assert.rejects(local.operation("../takeover")); assert.equal(calls,0);
});

test("MCP exposes explicit v2 tools and does not migrate legacy writers", async () => {
  const {startFakeBridge,startOfflineMcp}=await import('./fake-bridge.mjs');
  const fake=await startFakeBridge(); let mcp;
  try {
    mcp=await startOfflineMcp(fake.url);
    const {tools}=await mcp.client.listTools();
    for(const name of ['cs2_submit_command','cs2_operation','cs2_cancel_operation']) assert.ok(tools.some(t=>t.name===name),`missing ${name}`);
  } finally { await mcp?.close(); await fake.close(); }
});

test("response_lost_after_acceptance_is_reconciled_using_the_same_id_once", async () => {
  let submissions = 0; let retained;
  const server = createServer(async (req, res) => {
    if (req.method === "POST") {
      let body = ""; for await (const part of req) body += part;
      const accepted = JSON.parse(body); submissions++;
      retained = {operationId: accepted.operationId, phase: "succeeded"};
      req.socket.destroy(); return;
    }
    assert.equal(req.url, `/v2/operations/${id}`);
    res.setHeader("content-type", "application/json"); res.end(JSON.stringify(retained));
  });
  server.listen(0, "127.0.0.1"); await once(server, "listening");
  try {
    const bridge = createBridgeClient({baseUrl: `http://127.0.0.1:${server.address().port}`});
    await assert.rejects(bridge.submit(command), e => e.details.operationId === id && e.details.outcome === "unknown");
    assert.equal((await bridge.operation(id)).phase, "succeeded");
    assert.equal(submissions, 1);
  } finally { server.closeAllConnections(); server.close(); await once(server, "close"); }
});
