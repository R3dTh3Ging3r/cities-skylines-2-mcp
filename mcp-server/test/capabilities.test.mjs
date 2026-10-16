import assert from "node:assert/strict";
import { createServer } from "node:http";
import test from "node:test";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { startOfflineMcp } from "./fake-bridge.mjs";

const implementation = await import("../dist/capabilities.js").catch(() => null);
const capabilities = [
  { name: "cs2_build_road", support: "supported" },
  { name: "cs2_air_line", support: "unsupported", reason: "creation unavailable" },
];

test("nullable capability reasons compile against the production wire contract", () => {
  const root = fileURLToPath(new URL("../", import.meta.url));
  const result = spawnSync(process.execPath, ["node_modules/typescript/bin/tsc", "--noEmit", "--strict",
    "--skipLibCheck", "--target", "ES2022", "--module", "Node16", "--moduleResolution", "Node16",
    "test/capability-types.ts"], { cwd: root, encoding: "utf8", windowsHide: true });
  assert.equal(result.status, 0, result.stdout + result.stderr);
});

test("bridge implementation missing from client tools is notExposed, not unsupported", () => {
  assert.ok(implementation, "capability comparison implementation is missing");
  assert.deepEqual(implementation.compareExposure(capabilities, ["cs2_build_road"], []).capabilities, [
    { name: "cs2_build_road", support: "supported", registered: true, clientExposure: "notExposed" },
    { name: "cs2_air_line", support: "unsupported", reason: "creation unavailable", registered: false, clientExposure: "notExposed" },
  ]);
});

test("no supplied client list leaves exposure unknown and registration independent", () => {
  assert.ok(implementation, "capability comparison implementation is missing");
  const report = implementation.compareExposure(capabilities, []);
  assert.equal(report.clientTools, null);
  assert.deepEqual(report.registeredTools, []);
  assert.equal(report.capabilities[0].registered, false);
  assert.equal(report.capabilities[0].clientExposure, "unknown");
  assert.equal(report.capabilities[0].support, "supported");
  assert.equal(implementation.compareExposure(capabilities, [], ["cs2_build_road"]).capabilities[0].clientExposure, "exposed");
});

test("offline capability tool preserves distinct versions and reports actual registrations", async () => {
  const manifest = { protocolVersion: 1, schemaVersion: 1,
    identity: { assemblyVersion: "0.4.0", modVersion: "0.9.0", buildIdentity: null, buildIdentityReason: "buildIdentityUnavailable",
      sourceRevision: null, sourceRevisionReason: "sourceRevisionUnavailable", gameVersion: null, gameVersionReason: "gameVersionNotObserved" },
    runtime: { observedAtUtc: "2026-10-11T12:00:00Z", ageMilliseconds: 0, gameMode: "MainMenu", isLoading: false, cityLoaded: false, reason: null },
    capabilities: [{ name: "cs2_budget", support: "supported", reason: null }, { name: "cs2_missing", support: "supported" }],
    endpoints: [], nativeSupport: [], degradedReasons: ["gameNotLoaded"] };
  const requests = [];
  const server = createServer((req, res) => {
    requests.push(req.url);
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify(manifest));
  });
  let mcp;
  try {
    await new Promise((resolve, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", resolve); });
    mcp = await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);
    const { tools } = await mcp.client.listTools();
    assert.ok(tools.some((tool) => tool.name === "cs2_capabilities"), "additive capability tool is missing");
    assert.equal(tools.length, 70);
    assert.equal(requests.length, 0);
    for (const args of [{}, { clientTools: ["cs2_ping"] }]) {
      const result = await mcp.client.callTool({ name: "cs2_capabilities", arguments: args });
      assert.equal(result.isError, undefined, JSON.stringify(result));
      const payload = JSON.parse(result.content[0].text);
      assert.equal(payload.identity.assemblyVersion, "0.4.0");
      assert.equal(payload.identity.modVersion, "0.9.0");
      assert.equal(payload.identity.mcpVersion, "0.8.0");
      assert.equal(payload.identity.gameVersion, null);
      assert.equal(payload.protocolVersion, 1);
      assert.equal(payload.schemaVersion, 1);
      assert.deepEqual(new Set(payload.exposure.registeredTools), new Set(tools.map((tool) => tool.name)));
      assert.equal(payload.exposure.capabilities[0].registered, true, "helper registrations must be observed");
      assert.equal(payload.exposure.capabilities[1].registered, false);
      assert.equal(payload.exposure.capabilities[0].clientExposure, args.clientTools ? "notExposed" : "unknown");
      assert.equal(payload.exposure.capabilities[0].support, "supported");
      assert.equal(payload.capabilities[0].reason, null);
      assert.equal(payload.exposure.capabilities[0].reason, null);
    }
    assert.deepEqual(requests, ["/capabilities", "/capabilities"], "client exposure stays in Node");
  } finally {
    await mcp?.close();
    server.closeAllConnections();
    await new Promise((resolve) => server.close(resolve));
  }
});
