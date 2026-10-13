import assert from "node:assert/strict";
import http from "node:http";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

// Exercise the real MCP process against an ephemeral fixture, never a game.
test("MCP tool bridge", { timeout: 20_000 }, async (t) => {
  const requests = [];
  const pingFixture = { version: "test-fixture", mode: "Game", loading: false };
  const warning = "switching to default selection mode discards unfinished player tool previews";
  const cleared = { changed: true, previousTool: "NetToolSystem", activeTool: "DefaultToolSystem", warning };
  let clearStatus = 200;
  let clearBody = cleared;
  const png = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=", "base64");
  const bridge = http.createServer((req, res) => {
    requests.push({ method: req.method, url: req.url });
    if (req.url === "/screenshot?width=640") {
      res.writeHead(200, { "content-type": "image/png" });
      res.end(png);
      return;
    }
    const isClear = req.url === "/tools/clear";
    const isPing = req.url === "/ping";
    res.writeHead(isClear ? clearStatus : isPing ? 200 : 404, { "content-type": "application/json" });
    res.end(JSON.stringify(isClear ? clearBody : isPing ? pingFixture : { error: "unexpected fixture route" }));
  });
  await new Promise((resolve, reject) => {
    bridge.once("error", reject);
    bridge.listen(0, "127.0.0.1", resolve);
  });
  const closeBridge = async () => {
    if (!bridge.listening) return;
    await new Promise((resolve, reject) => {
      bridge.close((error) => error ? reject(error) : resolve());
      bridge.closeAllConnections();
    });
  };
  const client = new Client({ name: "cs2-mcp-tests", version: "1.0.0" });
  t.after(async () => {
    await client.close();
    await closeBridge();
  });
  const transport = new StdioClientTransport({
    command: process.execPath,
    args: ["dist/index.js"],
    cwd: fileURLToPath(new URL("../", import.meta.url)),
    env: { ...process.env, CS2_BRIDGE_URL: `http://127.0.0.1:${bridge.address().port}` },
  });
  await client.connect(transport);

  await t.test("advertises clear tool alongside existing tools", async () => {
    const { tools } = await client.listTools();
    assert.equal(tools.length, 66);
    assert.equal(new Set(tools.map((tool) => tool.name)).size, tools.length);
    const clear = tools.find((tool) => tool.name === "cs2_clear_tool");
    assert.ok(clear);
    assert.deepEqual(clear.inputSchema.properties, {});
    assert.ok(tools.some((tool) => tool.name === "cs2_ping"));
    assert.ok(tools.some((tool) => tool.name === "cs2_screenshot"));
  });

  await t.test("clear tool forwards the command and returns the bridge result", async () => {
    const result = await client.callTool({ name: "cs2_clear_tool", arguments: {} });
    assert.ok(!result.isError);
    assert.deepEqual(JSON.parse(result.content[0].text), cleared);
    assert.deepEqual(requests.at(-1), { method: "GET", url: "/tools/clear" });
  });

  await t.test("preserves an already-clear response", async () => {
    clearBody = { changed: false, previousTool: "DefaultToolSystem", activeTool: "DefaultToolSystem", warning };
    const result = await client.callTool({ name: "cs2_clear_tool", arguments: {} });
    assert.ok(!result.isError);
    assert.deepEqual(JSON.parse(result.content[0].text), clearBody);
  });

  await t.test("surfaces bridge refusal without retrying the command", async () => {
    for (const [status, message] of [
      [409, "another build operation is in progress, retry shortly"],
      [409, "game is loading, retry after the city has loaded"],
      [409, "no city loaded (in main menu or editor); load a save first"],
      [503, "game tool systems unavailable, retry shortly"],
    ]) {
      clearStatus = status;
      clearBody = { error: message };
      const count = requests.length;
      const result = await client.callTool({ name: "cs2_clear_tool", arguments: {} });
      assert.equal(result.isError, true);
      assert.equal(result.content[0].text, message);
      assert.equal(requests.length, count + 1);
    }
  });

  await t.test("ping remains available", async () => {
    const result = await client.callTool({ name: "cs2_ping", arguments: {} });
    assert.ok(!result.isError);
    assert.deepEqual(JSON.parse(result.content[0].text), pingFixture);
  });

  await t.test("screenshot stays a separate explicit command", async () => {
    const count = requests.length;
    const result = await client.callTool({ name: "cs2_screenshot", arguments: { width: 640 } });
    assert.ok(!result.isError);
    assert.deepEqual(result.content, [{ type: "image", data: png.toString("base64"), mimeType: "image/png" }]);
    assert.deepEqual(requests.slice(count), [{ method: "GET", url: "/screenshot?width=640" }]);
  });

  await t.test("unavailable bridge returns a useful tool error", async () => {
    await closeBridge();
    const result = await client.callTool({ name: "cs2_clear_tool", arguments: {} });
    assert.equal(result.isError, true);
    assert.match(result.content[0].text, /Cannot reach the CS2 bridge/);
  });
});
