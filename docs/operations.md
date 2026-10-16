# World-scoped operations: O2 preparation

The O2 candidate adds tested managed policies and an explicit v2 transport. It
is not release 2 writer enablement: native submit, status and cancel remain
unavailable until O3 completion and O5 scoped lease authority exist. Existing
legacy MCP mutation tools keep their existing routes and success fields. They
have not acquired operation IDs, leases, verified receipts or all-writer fencing.

`GET /capabilities` retains protocol/schema 1 for that legacy surface and adds
`operationProtocol.version:2`, the managed world/readiness snapshot, bounds and
explicit disabled reasons. The three v2 operation capabilities remain `unverified`.
Native validation startup opt-in still grants no dispatch authority.

## Requests

- `POST /v2/commands`, JSON `CommandRequest`:
  `{operationId,world:{bootId,epoch},leaseId,name,args,expectedFingerprint?}`.
  Supply a UUID **before** the request and retain the complete immutable request.
  Names are fixed catalogued legacy writer capability names, such as
  `cs2_build_road`; reads and arbitrary URLs/reflection are rejected. Resolving a
  name is not proof that its native lifecycle is supported. O2 returns a typed
  `NOT_READY`, `WORLD_MISMATCH` or guard-unavailable `UNSUPPORTED` error with the
  original ID, without enqueueing native work or fabricating an admitted record.
- `GET /v2/operations/{operationId}` and
  `POST /v2/operations/{operationId}/cancel` take no body. Both explicitly refuse
  with `UNSUPPORTED` until scoped lookup/cancel authority exists. A UUID alone
  does not establish that authority; the core registry's `Get`/`RequestCancel`
  are internal policy mechanics, not public authorization.

Methods, route/UUID, framing and JSON shape are checked before any enqueue.
Bodies are limited to 1,048,576 UTF-8 bytes; headers to 16,384 bytes; JSON nesting
to 64. Command POST requires `Content-Length` and `application/json`. Duplicate
headers/JSON keys, transfer encoding, invalid lengths/UTF-8, trailing JSON,
unknown fields and bodies on other routes reject. Cancel POST requires
`Content-Length:0`. The listener remains bound to IPv4 loopback.

The TypeScript `BridgeClient` retains `getJson`/`getResponse` and adds `submit`,
`operation` and `cancelOperation`. The corresponding explicit MCP tools are
`cs2_submit_command`, `cs2_operation` and `cs2_cancel_operation`. New operation calls
require a loopback HTTP base URL. They never retry automatically or replace the
caller UUID; timeout, response-body loss, malformed response or transport failure
preserve that UUID in typed error data. An unknown outcome requires reconciliation
before any new operation ID is created. Status is deliberately unavailable in this
candidate, so a native unknown outcome cannot yet be reconciled through v2.

## Managed policy and remaining producers

World identity uses a random boot ID for the mod lifetime, including listener
restart, and a monotonically increasing epoch. Retained metadata supports an
`OnGamePreload` override that fences the old world before load cleanup. A managed
observer also fences newly observed loading; completion remains pending until a
later update observes a loaded game outside loading. Completion while the manager
still reports loading cannot erase that signal. Callback thread, cardinality,
first safe update and actual reload behavior still require G1 evidence. Readiness
stays unavailable when the necessary callback/observation evidence is missing.

Each registry owns exactly one epoch, keeps all admitted keys up to 2,048 and
refuses new admission at capacity. It never evicts a completed key to permit
replay. Its atomic admission result distinguishes new from replay; pending work
is bounded at 256. Caller-visible records, receipts, entity arrays and JSON result
containers are detached from retained state. Applying rechecks world and the
independent authority policy. A changed world interrupts unstarted work and marks
started work indeterminate; the frozen registry cannot admit another epoch.
The bridge replaces that registry for the new epoch, so old-world status is not a
durable cross-world history. No cross-process exactly-once guarantee exists.

SHA-256 identity includes world, command, canonical parsed arguments and optional
`expectedFingerprint`. Object keys sort ordinally, whitespace/escape spelling
normalizes, and numeric lexemes are deliberately retained exactly; `1` and `1.0`
may therefore conflict conservatively. Lease ID is separately bound to the
admitted record and revalidated on admission/replay/apply. Idempotency never grants
lease authority. Ordinary transitions cannot replace terminal results. Explicit
native reconciliation, writer ownership/quarantine/cleanup, lease producers,
batches and native executors are later O3/O5/O7 work; O2 does not invent them.

The existing legacy queue now bounds pending requests at 256 and starts at most
one writer plus eight reads or 2 ms additional queued work per update. Existing
legacy writers capture a managed ingress world and reject at dispatch when it
changed or remains unavailable. Legacy reads retain their existing behavior.
The original O2 checks did not fence deferred callbacks. The subsequent bounded
O3 slice now fences deferred transit naming (including its response/name getter),
tile refresh and screenshot capture/publication, and disarms old-world auto-pause.
It still does **not** fence generic tool creation/apply, the earlier transit
CreateDefinitions/WaitForPaths/OnStopRunning stages, supply a caller-observed
legacy world, disable legacy writes by default, or prevent a legacy writer
conflicting with a later v2 lease. O5 must deliver that migration together
with explicit local compatibility opt-in and `retrySafe:false` server-assigned
legacy IDs. Until then, no release 2 or all-writer safety claim is made.

The lifecycle fixture descriptor now requires one tiny disposable action with a
retained UUID, intentional response loss, query of that same UUID, exact final
entity count/cost readback, and an old-world rejection after same-save reload.
Its collector remains read-only and refuses mutation dispatch. All native steps
remain `notRun`; descriptor assertions are preparation, not execution evidence.

The separate partial C1 `GET /v2/construction/inventory` read uses the main-thread
queue and explicitly validates caller-observed world/readiness before and after
its bounded entity reads. Only the three fixed operation routes use listener-side
managed refusal. Inventory supports query parameters as documented in
[the bridge API](bridge-api.md#bounded-explicit-construction-inspection-partial-c1);
operation routes still reject them. It supplies no writer or lease authority.

## Deferred-world safety slice

The named O3 continuations retain the original ingress WorldRef. Each native
callback and delayed response builder checks world/readiness before and after
execution. Failure permanently discards that continuation, so later readiness
recovery cannot revive old work or idle tool restoration. A refused line finish
clears managed state without reading/restoring native tool state; this is not a
native cleanup or writer-readiness acknowledgment. Other tool lifecycle paths
remain outside this slice and require O3/O5/G1.

A stale/failed post-create line continuation returns a non-retryable partial or
unknown outcome. It never inspects the replacement city's entity to reconcile
old effects. Stale tile refresh is dropped without undoing the earlier purchase.
Legacy auto-pause retains its start world and clears on preload, observed
readiness loss and system destruction. It is not the O6 monotonic run supervisor.

Successful PNG responses add X-CS2MCP-Boot-Id, X-CS2MCP-Epoch and
X-CS2MCP-World-Ready headers. The MCP screenshot tool preserves its image and
adds structuredContent.captureWorld/captureWorldReady when these headers exist.
Main-menu/loading diagnostic images may report readiness=false; that is not a
ready-city observation. A changed world/readiness during deferred capture makes
an explicit error instead of publishing an image. Older bridges without these
headers preserve legacy image output; malformed/partial metadata produces a
protocol error. No native G1 assertion has been executed or passed by this work.

Partial O7 adds [bounded read-only observation groups](observations.md), with a
fixed catalog, shared ordinary/group pending and dispatch accounting, and actual
HTTP timeout/disconnect cleanup. `POST /v2/observations` accepts a strict JSON body
and rejects query parameters. It introduces no mutation batch, operation registry
replay, lease or writer completion authority. Ordinary writer timeouts retain
their existing unknown-outcome semantics.

Partial M4 adds `cs2_metric_catalog` and `cs2_observe` over the
[typed diagnostic metric API](bridge-api.md#typed-diagnostic-metrics-partial-m4).
First obtain a ready observed world from capabilities or a state observation
group, discover the catalog, then copy that exact world into an explicit city or
selected-line request. For example, `cs2_observe` with
`scope={kind:"city"}` and keys `city.population`, `city.money`, `budget.balance`
returns typed values with units, periods and unavailable reasons. A missing
Population component produces unavailable population, not a measured zero.
Monthly signed costs remain monthly signed costs.

Use O7 `metrics.read` when source-specific frames, raw normalized clock evidence
and fixed member metadata are required; the direct metric read returns the
dictionary facade of the same producer. Source sampling is sequential and never
atomic. Refresh timestamps, native clock conversion, panel parity and performance
remain unverified. Selected-line fault counts require complete per-slot evidence;
unknown distance does not become zero invalid segments. M4 itself adds no automatic score, simulation change or new writer capability.

The bounded [M5 collection workflow](observations.md#collected-diagnostic-history--bounded-m5-slice)
now uses those same metric batches. Call `cs2_collect_observations` with explicit
world/queries/scope/cohort and an interval of at least 1000ms; retain the returned
process-local ID. Collect two or more observations, call
`cs2_stop_observation_collection`, then page `cs2_read_observation_history`.
Repeat for a second history and pass explicit retained inclusive sequence ranges,
the exact cohort and raw-normalized-time policy to `cs2_compare_observations`.
Stop returns a bounded summary, not a full history. Eight histories remain
retained including stopped histories; there is no deletion or persistence.
Comparison requires compatible source/clock/member evidence and never turns a
new empty replacement line into improvement. All v1 improvement fields are null;
large complete comparisons explicitly require narrower ranges. Disconnect/EOF
and shutdown abort pending collection reads without affecting simulation or
writer operations. Native history, calendar conversion, source refresh and
performance remain unverified.

To inspect a selected stop or facility before proposing construction, call
`cs2_construction_inventory` with its exact observed world/ref and optional
`relationships:"directDepth1"`. Owner/Attached and four fixed instance buffers
lead to deduplicated related building root bounds/network curves, with explicit
unsupported or missing targets. The global128 slot budget includes null and
duplicate entries; exceeding it refuses before dereference. Source-slot
completeness is distinct from complete facility/platform/approach geometry,
upgrade effects and connectivity, which remain unverified. The same literal
works in the fixed O7 inventory entry; no scan, recursion or writer is added.
