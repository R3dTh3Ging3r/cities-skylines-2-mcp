# Bridge Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a small first release with consistent encoding, truthful errors/units, capability discovery and regression coverage.

**Architecture:** Extract the shared Node transport without rewriting existing registrations. Add pure managed contract helpers and additive C# response metadata. Preserve native behavior while identifying uncertain fields explicitly.

**Tech Stack:** Existing TypeScript/Zod/MCP SDK, Node `node:test`, C# 9 / net472, BCL-only console regression runner.

**Spec:** [Shared delivery design](../specs/2026-10-11-bridge-improvements.md); roadmap R01/R12. [Delivery index](2026-10-11-bridge-implementation.md).

## Global Constraints

- Windows and Steam Cities: Skylines II; C# 9 / .NET Framework 4.7.2 for the mod; Node >=18 and the existing TypeScript ES2022/Node16 build target. Do not raise runtime floors in these releases.
- Use existing runtime dependencies. Node tests use `node:test`; pure C# regression tests use a small .NET Framework 4.7.2 console runner with linked production sources and no game assemblies. Keep existing reference-assembly package version 1.0.3.
- Compile the mod with `-p:SkipDeploy=true`. Deployment requires the game closed, a verified candidate, installed-build readback and a rollback copy.
- Preserve existing tool names and legacy success fields; add explicitly versioned contracts and documented deprecations. Correct false descriptions now; do not silently change a legacy field's unit or meaning.
- All remaining project-wide constraints in the shared spec apply. No game mutation, deployment or upstream contact is authorized merely by completing this document.

## Review Focus

1. Names containing both a literal plus and a space must survive exactly once; F1 tests mixed punctuation and Unicode.
2. HTTP 200 with invalid JSON is a protocol failure, not a successful empty city result; F2 tests it.
3. A missing tool in the client is not a missing game feature; F3 tests registered/server/client exposure separately.
4. Loading a paused city must not silently unpause it to refresh unlocks; F4/F5 report warming status.
5. A successfully queued setter may have no effective change yet; F5 tests deferred verification and range/step behavior.

---

## Commands and fixtures

Run commands from the repository root in PowerShell. `npm --prefix mcp-server test` will build then run `node --test test/*.test.mjs`; targeted runs use `node --test mcp-server/test/<file>.test.mjs` after build. F1 adds the test script and fake server. Core tests are built with `dotnet build tests/CS2MCP.CoreTests/CS2MCP.CoreTests.csproj -c Release`, then run with `& tests/CS2MCP.CoreTests/bin/Release/net472/CS2MCP.CoreTests.exe --suite <suite>`; absent suites and zero tests must fail. F2 adds that runner. No Node test imports `index.ts` and accidentally connects to a real bridge.

For native compile use `dotnet build CS2MCP.Bridge/CS2MCP.Bridge.csproj -c Release -p:SkipDeploy=true` with the actual game path configured as documented in [setup](../../codex-setup.md). A build failure blocks release. It is not a reason to enable automatic deployment.

### F1: Shared encoding and an offline MCP transport fixture

**Files:** Create `mcp-server/src/bridge-client.ts`, `mcp-server/test/fake-bridge.mjs`, `mcp-server/test/encoding.test.mjs`, `mcp-server/test/mcp-smoke.test.mjs`, `tests/contracts/legacy-tools.json`. Modify `mcp-server/src/index.ts` (`bridgeFetch`, `bridgeJson`, `bridgeQueryString` and query call sites), `mcp-server/package.json`.

**Interfaces:** Produces `encodeQuery(params: URLSearchParams): string`, `createBridgeClient(options: {baseUrl: string; timeoutMs?: number; fetchImpl?: typeof fetch}): BridgeClient`, and `BridgeClient.getJson<T>(path: string, query?: URLSearchParams): Promise<T>`. The fake bridge exposes `startFakeBridge(): Promise<{url: string; requests: object[]; close(): Promise<void>}>` bound to an ephemeral loopback port. Snapshot the current tool names/schema from the offline stdio server; 65 is the audited baseline, not a hard-coded permanent maximum.

- [ ] Write `encoding.test.mjs` asserting `encodeQuery(new URLSearchParams({name:"Campus + East & 50% / 東京"})) === "name=Campus%20%2B%20East%20%26%2050%25%20%2F%20%E6%9D%B1%E4%BA%AC"`. Exercise one old adapter and one newer adapter through stdio; fake server percent-decoding must recover the exact name, not `Campus+...`.
- [ ] Add the test script (`npm run build && node --test test/*.test.mjs`), build and run the targeted tests. Expected: failure for missing shared transport or old `+` encoding, not connection to port 8642.
- [ ] Implement the exports and route every adapter's query through `encodeQuery`, including arrays, negative coordinates and optional parameters. Keep the existing server's percent-decoder semantics; do not translate literal `+` into spaces server-side.
- [ ] Add `mcp-smoke.test.mjs`: initialize/list tools against the fake bridge, compare required legacy names and representative schemas to `legacy-tools.json`, close child/server after success and failure. Run `npm --prefix mcp-server test`; expect zero failed tests and no leaked handles.
- [ ] Commit only these task paths: `git commit -m "fix: unify bridge query encoding with contract tests"` after explicit `git add` of the listed files. Do not stage existing gameplay notes.

### F2: Typed failures without losing legacy diagnostics

**Files:** Create `mcp-server/src/contracts.ts`, `mcp-server/src/tool-results.ts`, `mcp-server/test/errors.test.mjs`, `CS2MCP.Bridge/Core/Contracts.cs`, `CS2MCP.Bridge/Core/ErrorCodes.cs`, `tests/CS2MCP.CoreTests/CS2MCP.CoreTests.csproj`, `Program.cs`, `ContractTests.cs` under that test directory. Modify `bridge-client.ts`, `index.ts`, `CS2MCP.Bridge/BridgeRequest.cs`, `HttpBridgeServer.cs`, `RequestHandlers.cs`.

**Interfaces:** Produces the spec's `BridgeErrorData`/`Capability` types; `BridgeClientError extends Error` with `details: BridgeErrorData`; `jsonResult(payload: unknown)` and `errorResult(error: unknown)` MCP results. C# adds `BridgeResponse.Error(int status, string message, string code, bool retryable = false, string outcome = "notApplied", string operationId = null, string[] nativeErrors = null)` retaining the existing overload and legacy `error` string. Core `ErrorCodes.FromStatus(int status): string` is a fallback, not a native-reason classifier.

- [ ] Write Node assertions: 409 `BRIDGE_BUSY` preserves status/native reasons; malformed JSON at 200 throws `PROTOCOL_ERROR`; a timed-out write is `outcome:"unknown"`; a known HTTP rejection is `notApplied`; fetch is called exactly once. Use injected fetch/AbortError fixtures rather than long sleeps. Add core assertions for status fallback and allowed outcome names.
- [ ] Run targeted `errors.test.mjs` and `--suite contracts`; expected failure because typed details/runner do not exist. The runner must count assertions, print suite/pass/fail totals and return nonzero for failure or unknown suite. Link only BCL-only Core files explicitly, not the whole game project.
- [ ] Implement transport parsing and additive errors. Non-JSON error bodies retain a bounded diagnostic excerpt; do not turn them into success. Preserve useful native validation messages. Introduce stable codes `INVALID_ARGUMENT`, `BRIDGE_BUSY`, `NOT_READY`, `UNSUPPORTED`, `LOCKED`, `NATIVE_REJECTED`, `PROTOCOL_ERROR`, `TRANSPORT_UNAVAILABLE`, `OUTCOME_UNKNOWN`; later plans extend the catalog.
- [ ] Verify targeted tests, full Node tests, core suite and SkipDeploy build. Check legacy text errors remain readable and structured data travels in the text JSON payload plus MCP `isError:true`; use `structuredContent` only if supported by the installed SDK contract.
- [ ] Stage listed task files and commit `fix: retain typed bridge errors and uncertain outcomes`.

### F3: Capability and endpoint inventory

**Files:** Create `CS2MCP.Bridge/EndpointCatalog.cs`, `RequestHandlers.Capabilities.cs`, `mcp-server/src/capabilities.ts`, `mcp-server/test/capabilities.test.mjs`, `tests/CS2MCP.CoreTests/EndpointTests.cs`, `docs/bridge-api.md`. Modify `RequestHandlers.cs` routing, `HttpBridgeServer.cs` ping path, `Mod.cs`, `index.ts`, `contracts.ts`, `Core/Contracts.cs`, test runner/project.

**Interfaces:** `EndpointDescriptor` contains path, legacy HTTP verbs, access (`read`, `cityWrite`, `uiWrite`, `nativePreview`, `control`), capability and completion mechanism. `EndpointCatalog.All` is the authoritative route classification used later by `CommandCatalog`. `GetCapabilities(BridgeRequest request): BridgeResponse` serves `/capabilities`; `cs2_capabilities` adds optional `clientTools:string[]`. Node `compareExposure(server: Capability[], registeredTools: string[], clientTools?: string[]): ExposureReport` leaves client exposure `unknown` when absent.

- [ ] Write tests: a capability implemented by the bridge but missing from supplied client tools is `notExposed`, not `unsupported`; no client list means `unknown`. Version fixture assembly0.4.0/mod0.9.0/MCP0.8.0 must preserve all three. Table-driven route coverage fails for any dispatch case missing classification, including loan, camera, save, line policy and tile refresh writers.
- [ ] Run `capabilities.test.mjs` and `--suite endpoints`; expected missing manifest/classification failure.
- [ ] Implement additive manifest (`protocolVersion:1`, `schemaVersion:1` in this release), source/build identity when available, native support and degraded reasons. Report game not loaded/loading without inferring permanent incompatibility. Include observed tool registrations and separate client exposure; do not edit the allowlist. Future protocol2 is advertised only after O2 passes.
- [ ] Verify all tests and SkipDeploy build. Read `/capabilities` at main menu and loaded disposable city during F6; missing game/version evidence must be null with a reason. Ensure ping remains usable while the main thread is unavailable without unsafe Unity access on network threads.
- [ ] Stage listed paths and commit `feat: report bridge capabilities and endpoint contracts`.

### F4: Truthful metrics and descriptions

**Files:** Create `CS2MCP.Bridge/Core/MetricContracts.cs`, `tests/CS2MCP.CoreTests/MetricTests.cs`, `mcp-server/test/metrics.test.mjs`, `tests/contracts/metrics.json`. Modify `RequestHandlers.cs` demand/date readback, `RequestHandlers.Economy.cs` `GetBudget`, `RequestHandlers.Transit.cs` `ListTransitLines`, `index.ts` descriptions, `docs/bridge-api.md`, test runner/project.

**Interfaces:** `MetricContracts.DescribeDemand(): DemandMetadata` states scale0–255; `DescribeBudget(): BudgetMetadata` states monthly rate/hourly refresh and distinguishes native signed aggregate from positive expense components; `DescribeRoute(float nativeLength, bool isCargo, int load): RouteMetricMetadata` exposes `validatedLengthMeters:float?`, validity, passenger/cargo load kind and unit evidence. Keep raw legacy values, mark misleading aliases deprecated. New cargo amount unit starts `unknown` until T1 verifies it.

- [ ] Write fixtures asserting demand0 and255 retain those values; income100/aggregate expense−40/component costs40/balance60 are described consistently without applying `abs` to arbitrary raw fields. Negative route length yields `validatedLengthMeters:null`, `lengthStatus:"invalid"`, preserved raw value and a warning. Cargo12 is not described as12 passengers. Quadratic `(cx,cz)` is documented as a control point, not a through-point.
- [ ] Run `metrics.test.mjs` and `--suite metrics`; expect missing metadata or incorrect legacy descriptions.
- [ ] Implement known corrections and additive metadata. Mark date conversion and service-building counts `unverified` if F6 parity cannot establish their semantics; include raw time/count values and a reason. Preserve tax-rate span vs allowed-range distinction. Do not invent unit conversions to make a panel match.
- [ ] Verify tests and SkipDeploy build. F6 checks native budget/calendar/count parity; no unpause solely to populate these reads. Update `docs/bridge-api.md` with raw/deprecated/new field mapping and refresh periods.
- [ ] Stage listed paths and commit `fix: qualify demand budget and route measurements`.

### F5: Valid stop discovery and effective setter values

**Files:** Create `CS2MCP.Bridge/Core/ValueValidation.cs`, `tests/CS2MCP.CoreTests/ValueTests.cs`, `mcp-server/test/setters.test.mjs`, `tests/contracts/setters.json`. Modify `RequestHandlers.Transit.cs` prefab/stop filtering and prerequisite reporting, `RequestHandlers.Economy.cs`, `RequestHandlers.Management.cs`, `RequestHandlers.LinePolicies.cs`, `RequestHandlers.LineVehicles.cs`, corresponding schemas/descriptions in `index.ts`, `docs/bridge-api.md` and test runner/project.

**Interfaces:** `ValueValidation.Check(double requested, double min, double max, double step): ValueCheck` returns accepted/requested/effectiveCandidate/reason (finite numbers only). `SetterReadback` fields: requested, effective nullable, range, step, `verification:"pending"|"verified"|"unsupported"`, mechanism, warning. Native field timing remains per-handler; O3 later owns uniform asynchronous completion. A setter with no trustworthy readback is pending, never verified by its input value.

- [ ] Write synthetic tests: min0/max100/step5 accepts25, rejects26 under strict validation; NaN/infinity reject; legacy clamping is explicitly reported where retained. Fake deferred setter initially returns pending and later effective25; acknowledgment alone is not verified. Placeholder/integrated-only stop prefabs are absent from normal placeable results; a loaded-but-not-ticked fixture reports warming prerequisites and performs no simulation control call.
- [ ] Run `setters.test.mjs` and `--suite values`; expect missing validation and misleading setter readbacks.
- [ ] Implement the helper, discover actual native bounds/steps for each setter and use them in MCP validation or response constraints. Do not assert that synthetic0–100/step5 is a game fee range. Preserve documented legacy clamping; new strict operations reject out-of-range inputs. Filter placeholders by native roles, not one hard-coded localized name. Distinguish unlock-node, depot and stop prerequisites.
- [ ] Verify tests, SkipDeploy build and F6 read-only prerequisite/range observations. Setter mutation parity is completed in O3's disposable fixtures before claiming all setter mechanisms verified. A pending field is an acceptable first-release limitation, not a completed feature claim.
- [ ] Stage listed task paths and commit `fix: validate placeable stops and report setter readback`.

### F6: Native parity, release evidence and build protection

**Files:** Create `scripts/Test-NativeFixture.ps1`, `tests/native/contracts.json`, `docs/testing.md`, `docs/validation/contracts.md`, `mcp-server/test/evidence.test.mjs`. Modify `docs/codex-setup.md`, `docs/bridge-api.md`, `README.md`. Raw output only `.local/v01/`.

**Interfaces:** `Test-NativeFixture.ps1 -Fixture <string> -EvidenceDir <string>` initially supports the read-only `contracts` fixture. Separate parameter set `-ValidateEvidence <string>` validates an existing report without contacting the bridge, returning exit0 only for a structurally valid report with all required checks passed; exit2 denotes valid incomplete evidence, exit1 invalid evidence. It refuses mutations; O1 adds explicit `-AllowMutation` for registered disposable fixtures. Evidence JSON contains fixture/build/world, expected/actual values, per-check `passed|failed|notRun`, timestamps, limitations and artifact paths. Native UI comparisons can be manually supplied observations; the runner must not fabricate panel parity.

- [ ] Write `evidence.test.mjs` against synthetic evidence: any failed/notRun required check prevents `validated:true`; invalid fixture names fail; artifact paths over240 chars reject; version identity can be unknown without claiming compatibility. Read-only fixture definitions contain only catalogued read operations.
- [ ] Run the targeted test; expected missing evidence validator/fixture runner behavior. Implement validation in `mcp-server/test/evidence.test.mjs` using fixture schema/data plus runner process tests; keep operational runner logic in PowerShell, test scripts with `powershell -NoProfile -File` on Windows.
- [ ] Implement fixture runner/reporting and document a manual probe for budget signs, date month/year boundaries, service counts, placeholder stops and slider bounds. If no verified disposable-city panel evidence is available, record `notRun`/unverified capability; do not access the user's active city to finish a report. Record installed/source build hashes separately.
- [ ] Run full Node/core suites and SkipDeploy build; compare installed mod hash before/after compilation to prove no deployment. For candidate native validation, close game, preserve installed mod, deploy verified candidate, reopen disposable fixture, run `powershell -NoProfile -File scripts/Test-NativeFixture.ps1 -Fixture contracts -EvidenceDir .local/v01`; capture required panel evidence and review report. Record rollback steps and tool schema/version differences. Native validation waits for an appropriate testing session if the user is playing.
- [ ] Stage only listed tracked files and commit `test: document bridge contract validation and release gates`. Release eligibility: known regressions pass, unresolved calendar/count fields explicitly qualified, no new mutation capability claimed, and build identity/tool exposure verified. No upstream publication in this task.
