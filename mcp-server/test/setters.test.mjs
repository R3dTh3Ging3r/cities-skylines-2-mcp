import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { createServer } from "node:http";
import test from "node:test";
import { startOfflineMcp } from "./fake-bridge.mjs";

const fixtures = JSON.parse(await readFile(new URL("../../tests/contracts/setters.json", import.meta.url), "utf8"));

// These catch adapter claims/rewrites: bridge fixture state is external; no native parity is implied.
test("setter tools describe pending values, real constraints and deprecated fee totals", async () => {
  const requests = [];
  const server = createServer((req, res) => {
    requests.push(req.url);
    const path = req.url.split("?")[0];
    const body = { "/city/loan/set": fixtures.pending, "/city/loan": fixtures.later, "/transit/stops": fixtures.warming }[path];
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify(body ?? {}));
  });
  let mcp;
  try {
    await new Promise((resolve, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", resolve); });
    mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    const { tools } = await mcp.client.listTools();
    const tool = (name) => tools.find((value) => value.name === name);
    assert.match(tool("cs2_set_loan").description, /pending/i);
    assert.match(tool("cs2_set_loan").description, /cash/i);
    assert.match(tool("cs2_get_fees").description, /deprecated/i);
    assert.match(tool("cs2_set_fee").description, /constraints/i);
    assert.match(tool("cs2_set_service_budget").description, /legacy/i);
    assert.match(tool("cs2_set_line_policy").description, /step/i);
    assert.match(tool("cs2_set_line_policy").description, /pending/i);
    assert.match(tool("cs2_set_line_vehicles").description, /buffer/i);
    assert.match(tool("cs2_list_transit_stops").description, /warming/i);

    const ack = await mcp.client.callTool({ name: "cs2_set_loan", arguments: { amount: 25 } });
    const value = JSON.parse(ack.content[0].text);
    assert.equal(value.setterReadback.verification, "pending");
    assert.equal(value.setterReadback.effective, null);
    assert.equal(value.appliedAmount, 25); // retained candidate does not become readback
    assert.deepEqual(JSON.parse((await mcp.client.callTool({ name: "cs2_get_loan", arguments: {} })).content[0].text), fixtures.later);
    const stops = await mcp.client.callTool({ name: "cs2_list_transit_stops", arguments: { prefabs: true } });
    assert.deepEqual(JSON.parse(stops.content[0].text), fixtures.warming);
    assert.equal(requests.some((url) => url.startsWith("/sim/")), false);
    assert.deepEqual(requests.slice(0, 2), ["/city/loan/set?amount=25", "/city/loan"]);
  } finally {
    await mcp?.close();
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
  }
});
