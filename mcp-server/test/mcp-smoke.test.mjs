import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { screenshotPng, startFakeBridge, startOfflineMcp } from "./fake-bridge.mjs";

test("offline stdio initializes and preserves required legacy names and schemas", async () => {
  const baseline = JSON.parse(await readFile(new URL("../../tests/contracts/legacy-tools.json", import.meta.url)));
  const bridge = await startFakeBridge();
  let mcp;
  try {
    mcp = await startOfflineMcp(bridge.url);
    const { tools } = await mcp.client.listTools();
    const byName = new Map(tools.map((tool) => [tool.name, tool]));
    for (const name of baseline.requiredNames) assert.ok(byName.has(name), `Missing legacy tool ${name}`);
    for (const [name, schema] of Object.entries(baseline.representativeSchemas)) {
      assert.deepEqual(byName.get(name).inputSchema, schema, `Schema changed for ${name}`);
    }
    assert.equal(bridge.requests.length, 0, "Tool discovery must not contact the bridge");
    const ping = await mcp.client.callTool({ name: "cs2_ping", arguments: {} });
    assert.equal(ping.isError, undefined);
    assert.equal(JSON.parse(ping.content[0].text).path, "/ping");
  } finally {
    try { await mcp?.close(); } finally { await bridge.close(); }
  }
});

test("offline screenshot keeps PNG bytes and default/explicit widths", async () => {
  const bridge = await startFakeBridge();
  let mcp;
  try {
    mcp = await startOfflineMcp(bridge.url);
    for (const [args, width] of [[{}, "1280"], [{ width: 640 }, "640"]]) {
      const result = await mcp.client.callTool({ name: "cs2_screenshot", arguments: args });
      assert.equal(result.isError, undefined);
      assert.deepEqual(result.content, [{ type: "image", data: screenshotPng.toString("base64"), mimeType: "image/png" }]);
      assert.equal(bridge.requests.at(-1).path, "/screenshot");
      assert.deepEqual(bridge.requests.at(-1).query, { width });
    }
  } finally {
    try { await mcp?.close(); } finally { await bridge.close(); }
  }
});
