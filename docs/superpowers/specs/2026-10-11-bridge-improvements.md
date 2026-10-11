# Bridge improvement delivery design

October 11, 2026 · Proposed implementation design · No implementation or deployment performed

This translates the [bridge roadmap](../../BRIDGE-ROADMAP.md) into shared decisions for the [implementation plans](../plans/2026-10-11-bridge-implementation.md). It extends the original setup design; it does not change the running city. Source observations are from this checkout, not evidence that a feature works against every game update.

## Scope and delivery boundaries

Deliver independently usable releases: (1) truthful contracts, (2) recoverable operations and batches, (3) precise construction, (4) cargo and transit editing, and (5) city management and diagnostics. Construction and transit can proceed independently after their stated shared dependencies. Early asset inventory and service readbacks precede facility mutations. R12 testing/release rules apply to every release. Retain the existing C# mod → loopback HTTP → Node MCP architecture and native game validation.

This is a reviewable plan, not a promise to implement every native feature regardless of feasibility. A failed native investigation produces a documented unsupported capability and blocks only its dependent task. Never substitute direct ECS writes that bypass normal validation, fabricated metrics, or demolition/rebuy for an unsupported native operation.

### Project-wide constraints

- Windows and Steam Cities: Skylines II; C# 9 / .NET Framework 4.7.2 for the mod; Node >=18 and the existing TypeScript ES2022/Node16 build target. Do not raise runtime floors in these releases.
- Use existing runtime dependencies. Node tests use `node:test`; pure C# regression tests use a small .NET Framework 4.7.2 console runner with linked production sources and no game assemblies. Keep existing reference-assembly package version 1.0.3.
- All game/ECS reads and writes stay on the game thread. A batch performs sequential mutations, never parallel native construction.
- Preserve existing tool names and legacy success fields; add explicitly versioned contracts and documented deprecations. Correct false descriptions now; do not silently change a legacy field's unit or meaning.
- Keep the listener on loopback. No remote hosting, client allowlist expansion, or automatic machine-configuration edits are part of this plan.
- Compile the mod with `-p:SkipDeploy=true`. Deployment requires the game closed, a verified candidate, installed-build readback and a rollback copy.
- Native tests use a disposable city and a separately named verified checkpoint. Do not use the player's developed city as a test fixture.
- Keep raw game data, screenshots, saves, binaries, credentials and machine paths in ignored `.local/` paths. Use compact run IDs and paths below 240 characters.
- Preserve upstream history, attribution and unrelated work. Use focused `codex/` branches in the personal fork. No upstream messages or PRs without user instruction.
- A timeout is an unknown outcome, not cancellation; a successful placement, connected graph, good appearance and working service are separate checks.

## Shared interfaces and ownership

These are **new bridge-owned contracts**, not names asserted to exist in the game API. Mirror their wire types in `mcp-server/src/contracts.ts` and `CS2MCP.Bridge/Core/Contracts.cs`. Domain-specific DTOs belong beside their feature. Implement each type with the task that first consumes it; do not scaffold all future types in release 1.

| Contract | Required content / semantics |
| --- | --- |
| `WorldRef` | `{ bootId: string, epoch: number }`. A random boot ID per mod lifetime; increment epoch and invalidate agent work at preload/start, including reload of the same save. A city name or simulation frame alone is not identity. |
| `EntityRef` | `{ world: WorldRef, index: number, version: number }`; reject mismatched worlds and recycled ECS versions. IDs in historical notes are not reusable handles. |
| `Observation<T>` | `{ data: T, world: WorldRef|null, sampledAtUtc: string, startFrame: number|null, endFrame: number|null, freshness: "current"|"warming"|"unavailable", warnings: string[] }`. A multi-frame read is never called atomic. |
| `BridgeErrorData` | `{ code: string, message: string, retryable: boolean, outcome: "notApplied"|"unknown"|"partial", httpStatus?: number, nativeErrors?: string[], operationId?: string }`. Unknown outcome requires reconciliation even when transport retry is possible. |
| `Capability` | `{ name: string, support: "supported"|"degraded"|"unsupported"|"unverified", reason?: string }`. Manifest includes protocol/schema, assembly/mod/MCP/game versions and tested-build identity separately. Client exposure is a separate supplied/discovered list, never guessed by the mod. |
| `CommandRequest` | `{ operationId: string, world: WorldRef, leaseId: string, name: string, args: object, expectedFingerprint?: string }`. UUID operation ID is supplied before dispatch and is also the idempotency key. Name resolves through a fixed command catalog, not an arbitrary URL. |
| `OperationRecord` | Request hash, world, phase, error, receipt, creation/update times, cancellation request, and `retrySafe`. Phases: `queued`, `validating`, `applying`, `verifying`, `succeeded`, `failed`, `cancelled`, `interrupted`, `indeterminate`. `indeterminate` quarantines the writer until reconciliation; it is not permission to retry. |
| `MutationReceipt` | `created`, `changed`, `reused`, `removed` EntityRefs; actual cost if known, `costStatus`, result payload, warnings and verification state. Unknown cost is null, not zero. A native-tool acknowledgment is not a verified receipt. |
| `PreviewRef` | `{ id: string, world: WorldRef, fingerprint: string, validation: "computed"|"nativeValidated", expiresAtUtc: string }`. Fingerprint covers the relevant entities, geometry, occupancy, ownership, asset/unlock state and reservations; apply rechecks them. |
| `NativeProbeResult` | API/type signatures observed, game/bridge build, success/failure/cancellation evidence, native outcome and supported/degraded/unsupported conclusion. Portable findings in Markdown; raw assembly inspection and city artifacts in `.local/`. |

### Transport and compatibility

Release 1 keeps current routes and response shapes. Extract shared query encoding and typed transport; encode a space as `%20` and a literal plus as `%2B` to match the current server's percent decoder. Add `/capabilities`, error details and descriptive metadata without requiring the operation protocol yet. Preserve the legacy `error` string beside structured error data.

Release 2 adds `POST /v2/commands`, `GET /v2/operations/{operationId}`, `POST /v2/operations/{operationId}/cancel`, `POST /v2/batches`, `GET /v2/batches/{batchId}`, `POST /v2/observations`, `POST /v2/control/acquire`, and `POST /v2/control/takeover`. New bulk payloads use JSON bodies under the existing 1 MiB limit. Reject unsupported verbs and malformed bodies before enqueueing. Ordinary read routes remain GET; `/v2/observations` is an explicitly read-only JSON batch of catalogued reads. HTTP POST alone does not determine mutation authority. `/v2/control/takeover` is an interrupt request processed before normal work; it never starts a competing native tool.

Every legacy mutation routes through the same lifecycle and writer guard. In release 2, legacy writes without world/lease fields require explicit local compatibility opt-in; otherwise return `UPGRADE_REQUIRED`, with reads unchanged. Under opt-in, capture the world at ingress, never at delayed execution, and reject during an active v2 lease or load. Legacy callers without IDs receive server-assigned IDs and `retrySafe:false`; they keep existing success fields. They cannot promise reconciliation if they lost the response containing that ID. The updated MCP adapter uses caller-stable operation IDs for mutations, surfaces the ID before/with uncertain results, and never generates a second ID for an automatic retry. Existing tools accept optional IDs; their outputs expose the assigned ID. Batch step IDs are always explicit. Document this deliberate legacy-write migration and its weaker guarantees; do not call legacy ingress equivalent to caller-supplied world fencing.

No exactly-once claim across process crashes. Operation records are in memory for one boot/world. Retain at most 2,048 admitted operations per epoch, including completed keys; refuse new admission at capacity rather than evicting a key and permitting replay. A different request hash with the same ID is `IDEMPOTENCY_CONFLICT`. Lost status after a restart is `WORLD_MISMATCH`/`OUTCOME_UNKNOWN`; a new ID requires fresh observation, not automatic resubmission. Admission limits are reported in capabilities and may be tuned only with evidence.

## Operations, saves and player control

`BridgeSystem` owns the registry, lease and queue. Pure lifecycle/selection helpers live under `CS2MCP.Bridge/Core/`; handlers remain partial classes and native tool systems remain responsible for native validation/apply/finish. Do not split the entire existing Node registration file just to reorganize it; extract transport and add feature registration modules as new features need them.

- One mutation may own the native writer at a time. Management setters, zoning, saves, simulation, camera/tool changes and legacy requests are classified in the command catalog. Native previews also acquire the lease because they can change temporary tool state. Read-only status/cancel/takeover requests remain available during a build.
- `queued` can cancel without effects. After apply begins, cancellation stops subsequent work; report committed entities and reconcile the active action. Never report cancelled with zero changes unless verified.
- On player takeover or native tool displacement, revoke the lease, invalidate previews, cancel unstarted steps and reconcile in-flight work. Add a visible bridge stop/takeover action; generic mouse movement is not reliable takeover detection. Capture relevant tool transitions and allow explicit user control to override timers. Audit native callbacks before selecting the exact hook.
- At most 32 mutation steps or 64 read items per batch. Start at most one mutation per update. Dispatch at most eight read handlers or 2 ms of additional queued read work per update, whichever is reached first; a single native call cannot be preempted. Cap the pending queue at 256. Large feature scans must paginate/yield rather than hide an unbounded scan in one read. Keep control requests responsive and report actual queue timing.
- Batches have explicit step IDs and dependencies on earlier receipts. Stop on the first failure, stale precondition or takeover. Resume reuses the same completed operation IDs after readback. There is no transactional rollback promise.
- Frame-based simulation and wall-clock deadlines have separate fields. Use a monotonic deadline for elapsed time and UTC for reporting; distinguish user pause, target reached, modal blockage, load and cancelled run. A hung game thread can prevent pause; report that limit.
- Installed `GameManager.Save` metadata shows `Task<bool>`. Observe task completion and its boolean result; then verify the intended saved asset/metadata through an investigated native signal. `completedWrite` and `reloadVerified` are separate. Failure/false must propagate, preview textures must be disposed according to native ownership, and saving must not use a detached fire-and-forget call.

## Feature decisions

### Construction

Exact zoning uses native block identity/version and cell indices, not synthetic screen coordinates. Query occupied and unzoned separately. Select exact cells, frontages, rectangles or polygons, with ownership-based avenue exclusions and reserved parcels. Default preserves buildings and occupied cells. Use a maximum of 4,096 selected cells per operation, chunked through normal validated updates with a full per-cell receipt. Undo restores only unchanged zone values in the same geometry/world, never buildings.

Bike/bus overlays are add/remove patches by left/right/both relative to edge start→end; preserve unrelated flags. Whole-road replacement remains distinct. Full cubic readback and explicit 3D endpoints precede geometry tooling. Do not silently clamp a requested elevation or attach to another level. Roundabouts/presets require complete native previews and lane/approach checks.

Read asset families, installed/possible upgrades, exterior gates, underground platforms and approach envelopes before exposing facility transformations. Native previews must leave committed entities, treasury and zones unchanged and clean up temporary state. Reservations are polygons stored in an optional mod sidecar keyed to a native save identity, rebound and revalidated after load; entity handles and previews never persist. If stable save identity cannot be proven, support explicit import/export and report persistence unavailable. Relocation preserves native identity/storage/upgrades where supported; unsupported preservation rejects the operation.

### Transit

Creation accepts mode and purpose independently. Cargo rail, cargo air and passenger air are three native fixtures and three capability flags. Resolve exact station platforms/gates, outside endpoints and return paths; replace passenger-only depot assumptions with mode-specific dispatch evidence. Existing route editing must retain line identity, fares, fleet and schedule. Reversed stop lists are candidate duplicates, not permission to delete. Report cargo amount/capacity with verified units or explicit `unknown`; do not label freight as passengers. The creation result and observed vehicle/freight operation are separate outcomes.

### Management and diagnostics

Early service readings expose actual capacity/storage/staffing/connectivity for facility checks. Later summaries add budget reconciliation, loan quotes, detailed dispatch, progression, resource and traffic history. Rates declare periods; stores declare units; unsupported measurements are null with reasons. Point purchases use normal prerequisites and verified debit, never forced unlock. Loan quoting must not call a mutation path. Traffic comparisons use fixed cohorts and matched time windows; new empty roads and nightfall do not count as a demonstrated traffic improvement.

## Native investigation gates

| Gate | Answer before enabling dependent mutation |
| --- | --- |
| G1 lifecycle | Preload/takeover/apply/finish ordering, saved-asset identity after `Task<bool>`, cancellation boundary and every writer's completion signal |
| G2 construction | Native zone cells/frontage, overlay masks and preservation, temporary preview cleanup, upgrade/relocation connectors and owner-area edit APIs |
| G3 transit | Cargo/passenger prefabs, platform/gate roles, original-route edit definitions, outside dispatch, cargo amount units |
| G4 services | UI-matching periods/units and parent-extension aggregation, read-only loan evaluation, point purchase/unlock replay, notification severity |

An investigator records exact native signatures from this installed build and tests them only in a disposable fixture during implementation. No full assembly dumps enter tracked docs. Dependent code is enabled only on a passing fixture; unverified builds advertise unverified/degraded support and require validation before mutations.

To avoid making a fixture depend on its own passing result, development candidates may enable fixed probe commands with local startup opt-in `CS2MCP_NATIVE_VALIDATION=true` (default false). Such commands remain `unverified`/`validationOnly` in capabilities, require the normal world/lease guards and explicit fixture-runner `-AllowMutation`, and never accept arbitrary native calls. O1 owns this probe-mode contract; O8 verifies ordinary releases reject validation-only commands without opt-in. This is a proposed development setting, not a configuration change made by planning or a bypass of game validation.

## Acceptance and release evidence

Each plan task identifies offline assertions and, where necessary, a named native fixture. Use native fixture IDs in evidence reports with `passed`, `failed` or `notRun`; never silently skip a native test and call a release validated. Measure individual calls versus batches against the same restored fixture: request count, wall time, game frames, exact final diff, errors and collateral changes. Establish a baseline before promising speed gains.

Release 1 may ship without enabling any new mutations. Later feature releases need passing shared lifecycle tests and their own native fixture. Publish a capability/build matrix, preserve a rollback candidate and document precise limitations. Planning self-review verifies task/spec coverage; it is not execution evidence.
