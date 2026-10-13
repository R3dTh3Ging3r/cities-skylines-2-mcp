import assert from "node:assert/strict";
import { createServer } from "node:http";
import test from "node:test";
import { startFakeBridge, startOfflineMcp } from "./fake-bridge.mjs";

test("road graph advertises optional cubic curve readback", async () => {
  const bridge = await startFakeBridge();
  let mcp;
  try {
    mcp = await startOfflineMcp(bridge.url);
    const { tools } = await mcp.client.listTools();
    const graph = tools.find((tool) => tool.name === "cs2_road_graph");
    assert.equal(graph.inputSchema.properties.includeCurve?.type, "boolean");
    assert.ok(!graph.inputSchema.required.includes("includeCurve"));
    assert.equal(bridge.requests.length, 0);
  } finally {
    try { await mcp?.close(); } finally { await bridge.close(); }
  }
});

test("road graph forwards opt-in and explicit false without changing existing query encoding", async () => {
  const bridge = await startFakeBridge();
  let mcp;
  try {
    mcp = await startOfflineMcp(bridge.url);
    const base = { x: -12.25, z: 34.5, radius: 90, limit: 7, query: "Track + East & 東京", allNets: true };
    for (const options of [{}, { includeCurve: true }, { includeCurve: false }]) {
      const result = await mcp.client.callTool({ name: "cs2_road_graph", arguments: { ...base, ...options } });
      assert.equal(result.isError, undefined);
      const expected = { x: "-12.25", z: "34.5", radius: "90", limit: "7", query: base.query, nets: "all" };
      if (options.includeCurve !== undefined) expected.includeCurve = String(options.includeCurve);
      const request = bridge.requests.at(-1);
      assert.equal(request.method, "GET");
      assert.equal(request.path, "/city/road-graph");
      assert.deepEqual(request.query, expected);
      assert.deepEqual(JSON.parse(result.content[0].text).query, expected);
      assert.ok(!request.rawQuery.includes("+"), "Literal plus and spaces must use the shared encoder");
    }
    await mcp.client.callTool({ name: "cs2_road_graph", arguments: { x: 0, z: 0, allNets: false } });
    assert.deepEqual(bridge.requests.at(-1).query, { x: "0", z: "0" });
    assert.equal(bridge.requests.length, 4);
  } finally {
    try { await mcp?.close(); } finally { await bridge.close(); }
  }
});

test("stdio preserves raw curved and elevated cubic handles and legacy graph fields", async () => {
  // Synthetic transport evidence only; it does not execute the C# handler or validate native geometry.
  const curve = {
    a: { x: -100.25, y: 12.5, z: 0 },
    b: { x: -75, y: 28.75, z: 40 },
    c: { x: 45, y: -3.5, z: 80.125 },
    d: { x: 120.5, y: 8.25, z: 100 },
  };
  const graph = {
    area: { x: 0, z: 50, radius: 300 }, roadsMatched: 1,
    roads: [{ road: { index: 123, version: 2 }, prefab: "Train Track", startNode: 10, endNode: 11,
      start: curve.a, end: curve.d, length: 265, partOfBuilding: true, flowPercent: null, volume: null, curve }],
    junctions: [], note: "synthetic fixture",
  };
  const requests = [];
  const server = createServer((req, res) => {
    requests.push(req.url);
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify(graph));
  });
  let mcp;
  try {
    await new Promise((resolve, reject) => {
      server.once("error", reject);
      server.listen(0, "127.0.0.1", resolve);
    });
    mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    const result = await mcp.client.callTool({ name: "cs2_road_graph", arguments: { x: 0, z: 50, includeCurve: true, allNets: true } });
    assert.equal(result.isError, undefined);
    assert.deepEqual(JSON.parse(result.content[0].text), graph);
    assert.deepEqual(requests, ["/city/road-graph?x=0&z=50&nets=all&includeCurve=true"]);
  } finally {
    await mcp?.close();
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
  }
});
