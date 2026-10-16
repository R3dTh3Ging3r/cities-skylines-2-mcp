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

test("screenshot carries captured world and diagnostic readiness without changing PNG", async () => {
  const { createServer } = await import("node:http");
  const server = createServer((req,res) => {
    res.writeHead(200,{"content-type":"image/png","X-CS2MCP-Boot-Id":"captured-boot","X-CS2MCP-Epoch":"7","X-CS2MCP-World-Ready":"false"});
    res.end(screenshotPng);
  });
  await new Promise(resolve=>server.listen(0,"127.0.0.1",resolve)); let mcp;
  try {
    mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    const result=await mcp.client.callTool({name:"cs2_screenshot",arguments:{}});
    assert.equal(result.isError,undefined);
    assert.deepEqual(result.structuredContent,{captureWorld:{bootId:"captured-boot",epoch:7},captureWorldReady:false});
    assert.deepEqual(Buffer.from(result.content[0].data,"base64"),screenshotPng);
  } finally {
    await mcp?.close(); server.closeAllConnections(); await new Promise(resolve=>server.close(resolve));
  }
});

test("screenshot refuses malformed or partial capture-world metadata", async () => {
  const { createServer } = await import("node:http");
  const cases = [
    {"X-CS2MCP-Boot-Id":"boot","X-CS2MCP-Epoch":"NaN","X-CS2MCP-World-Ready":"true"},
    {"X-CS2MCP-Boot-Id":"boot","X-CS2MCP-Epoch":"9007199254740992","X-CS2MCP-World-Ready":"true"},
    {"X-CS2MCP-Boot-Id":"boot"},
  ];
  let index = 0;
  const server = createServer((req,res) => {
    res.writeHead(200,{"content-type":"image/png",...cases[index++]});res.end(screenshotPng);
  });
  await new Promise(resolve=>server.listen(0,"127.0.0.1",resolve));let mcp;
  try {
    mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    for(let i=0;i<cases.length;i++) {
      const result=await mcp.client.callTool({name:"cs2_screenshot",arguments:{}});
      assert.equal(result.isError,true);assert.equal(result.structuredContent.errorData.code,"PROTOCOL_ERROR");
      assert.ok(result.content.every(item=>item.type!=="image"));
    }
  } finally {
    await mcp?.close();server.closeAllConnections();await new Promise(resolve=>server.close(resolve));
  }
});
