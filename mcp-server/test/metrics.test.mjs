import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { createServer } from "node:http";
import test from "node:test";
import { startOfflineMcp } from "./fake-bridge.mjs";

const fixtures = JSON.parse(await readFile(new URL("../../tests/contracts/metrics.json", import.meta.url), "utf8"));

test("metric tools advertise units and uncertainty without false legacy meanings", async () => {
  const server = createServer((req, res) => {
    const value = { "/city/demand": fixtures.demand, "/city/budget": fixtures.budget, "/transit/lines": fixtures.transit }[req.url];
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify(value));
  });
  let mcp;
  try {
    await new Promise((resolve, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", resolve); });
    mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    const { tools } = await mcp.client.listTools();
    const tool = (name) => tools.find((value) => value.name === name);
    assert.match(tool("cs2_budget").description, /signed.*aggregate/i);
    assert.match(tool("cs2_budget").description, /hour/i);
    assert.match(tool("cs2_demand").description, /0-255/);
    assert.match(tool("cs2_list_transit_lines").description, /cargo.*unknown/i);
    assert.match(tool("cs2_game_state").description, /unverified/i);
    assert.match(tool("cs2_city_overview").description, /unverified/i);
    assert.match(tool("cs2_service_budgets").description, /count.*unverified/i);
    assert.match(tool("cs2_get_taxes").description, /effective.*span/i);
    for (const name of ["cs2_build_road", "cs2_connect_road"]) {
      assert.match(tool(name).description, /control point/i);
      assert.doesNotMatch(tool(name).description, /through (?:that control point|cx\/cz)/i);
    }
    // Exercise real MCP encoding: no demand rescaling, expense abs, or cargo/length normalization.
    for (const [name, fixture] of [["cs2_demand", fixtures.demand], ["cs2_budget", fixtures.budget], ["cs2_list_transit_lines", fixtures.transit]]) {
      const result = await mcp.client.callTool({ name, arguments: {} });
      assert.equal(result.isError, undefined);
      assert.deepEqual(JSON.parse(result.content[0].text), fixture);
    }
  } finally {
    await mcp?.close();
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
  }
});
