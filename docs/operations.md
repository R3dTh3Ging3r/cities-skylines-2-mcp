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
These checks do **not** fence deferred rename/tile/tool callbacks, supply a
caller-observed world, disable legacy writes by default, or prevent a legacy
writer conflicting with a later v2 lease. O5 must deliver that migration together
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
