# M5 collected-history lifecycle implementation readiness

> Design only, October 16, 2026. No source changes, tests, installations or native probes were performed. Implementation requires root's explicit GO after M4 acceptance. Continue the established sole-implementer/independent-review workflow; do not dispatch additional implementers.

**Goal:** Collect bounded, process-local diagnostic observations through the real O7 transport and compare explicit retained ranges using their original identity and clock evidence.

**Architecture:** One TypeScript store owns retained series, one monotonic scheduler and two network permits. It consumes accepted M4 schemas/catalog and producer-owned MetricBatch evidence. A small lifecycle adapter connects disposal to the actual SDK/stdio lifetime; pure comparison consumes stored samples without issuing reads.

**Tech stack:** Existing TypeScript, Node >=18, installed MCP SDK 1.32.1, Zod and node:test. No dependencies, native projection, persistence or writer changes.

**Spec:** `docs/superpowers/plans/2026-10-16-metrics-history-detail.md`, `2026-10-16-collected-diagnostics.md`, and original M5 tools in `2026-10-11-bridge-management.md`. Source inspected at published O7/T1 checkpoint `1cc0d48` plus M4's actively evolving files. Freeze the accepted M4 commit before implementation.

## Scope and exact interfaces

The four public tools are `cs2_collect_observations`, `cs2_stop_observation_collection`, `cs2_read_observation_history` and `cs2_compare_observations`. No append, delete, export, arbitrary URL, automatic restart, native simulation control or replacement-world API is added.

M4 implementer confirmed these exports from `mcp-server/src/diagnostic-metrics.ts`: `metricReadRequestSchema`, `metricScopeSchema`, `metricBatchSchema`, `metricDefinitionSchema`, inferred `MetricReadRequest`/`MetricBatch`, and semantic `parseMetricBatch`/`parseMetricObservation` helpers. Current source also supplies `parseMetricBatchObservation`. Reconcile the accepted names once; reuse the semantic parser instead of repeating its native/source rules.

Admission calls `bridge.metricCatalog('all')` asynchronously and retains only requested definitions from that actual catalog. Do not duplicate definitions or native projection in M5. The catalog currently returns an array without a revision field; pin the accepted batch schema's literal `diagnosticMetrics.v1` alongside the selected definitions. Prefer an exported shared revision constant if M4 adds one; never infer revision from wall time. A later batch revision mismatch rejects publication.

Strict input scope is `{kind:'city'}` or `{kind:'selectedLine',line:EntityRef}`. Current native output city scope is `{kind:'city',line:null}`. Normalize using accepted input/output schemas; raw JSON shape inequality must not reject legitimate city output. The cohort is explicitly `{kind:'city',world,members:[]}` or `{kind:'fixedSelectedLine',world,members:[exactLineRef]}`. Bind the scope/world/cohort full bootId/epoch/index/version at admission and deep-copy/freeze all caller inputs and catalog definitions.

Store interface:

```text
startCollection(spec: SeriesSpec, options?: {signal?: AbortSignal}): Promise<SeriesStatus>
stopCollection(id: string): SeriesStatus
readSeries(id: string, {afterSequence?,limit?}): HistoryPage
compareObservations({baseline,current,cohort,policy}): Comparison
dispose(): void
```

`SeriesSpec` is the tracked world's queries/scope/cohort plus `maxSamples:1..512` and `intervalMs:1000..86400000`; query admission uses the real M4 request schema (1–64 distinct known keys, finite permitted parameters, currently no nonempty parameters). The internal create/append functions remain private. Process-scoped opaque IDs have a fixed length. Public callbacks do not supply arbitrary reader functions.

## SDK lifecycle facts and minimal wiring

The following are installed-source findings, not assumed API behavior:

| Installed source | Behavior and consequence |
|---|---|
| `@modelcontextprotocol/sdk/dist/esm/server/mcp.js:87` | `McpServer.connect` delegates to its public underlying `.server`; `.close()` delegates similarly. The close callback is `server.server.onclose`, not `McpServer.onclose`. |
| `shared/protocol.js:219` | `connect()` takes transport ownership and wraps existing transport callbacks. Never replace its `onclose`, `onerror` or `onmessage` after connection. A throwing earlier transport close callback can prevent SDK `_onclose()`. |
| `shared/protocol.js:252` | SDK `_onclose()` clears protocol timeouts, aborts active request handlers, clears transport, then calls public `onclose`, then rejects remaining response handlers. A throwing public callback can interrupt later SDK work. |
| `server/stdio.js:32` | `start()` listens to stdin `data`/`error` only. It has no stdin `end` or `close` listener. Collection timers can therefore survive client EOF unless the application closes the SDK. |
| `server/stdio.js:54` | Normal `close()` removes its own stdin listeners, conditionally pauses stdin, clears its buffer and invokes transport `onclose`. Reuse this path; do not destroy SDK internals. |
| `shared/protocol.js:274` and protocol declarations | Generic `onerror` is not necessarily fatal. Malformed notifications/messages can report recoverable errors. Do not dispose on every protocol error. |
| `client/stdio.js` | Client close ends child stdin, then falls back to SIGTERM/SIGKILL after waits. An EOF test must detect prompt cleanup independently of that fallback. |

Add `observation-history-lifecycle.ts` with `bindObservationHistoryLifecycle({server,transport,history,stdin,stdout,process,reportError})`, returning idempotent cleanup/shutdown handles for production and tests. `index.ts` creates the store, registers tools through its existing registration wrapper, binds lifecycle **before** `await server.connect(transport)`, and catches failed connection to dispose/close the partial connection before propagating the original failure.

The adapter owns only listeners it adds. Preserve existing public `server.server.onclose` and any pre-connect transport `onclose` callback: invoke each with its original receiver/arguments; guarantee synchronous `history.dispose()` even if it throws. Catch/report prior callback exceptions rather than allowing them to short-circuit SDK cleanup. Wrap pre-connect transport close before the SDK captures it; never overwrite the SDK wrapper after connection. The public server close wrapper disposes/removes hooks but does not recursively call `server.close()`.

Use explicit stdin `end`/`close` and fatal stdin/stdout stream-error listeners plus process `SIGINT`/`SIGTERM` listeners. EOF/stream failure/signal begins one guarded shutdown: synchronously dispose, then call normal `server.close()` and report rejected close on stderr. Actual SDK close also disposes. Remove owned listeners idempotently on close/failure; retain necessary error handling until close finishes so cleanup does not expose an unhandled stream error. Do not replace other signal listeners, call `removeAllListeners`, force `process.exit`, or treat recoverable protocol reports as disconnection. Do not change legacy writer timeout/cancellation behavior.

## Scheduling, cancellation and admission ownership

One scheduler injects `{nowMs,setTimeout,clearTimeout}` and a separate UTC supplier. Production monotonic time uses Node's monotonic performance clock. There is one owned wake timer, at most one poll per series and **two total network permits**. Round-robin due-series selection prevents an early series from monopolizing permits. Deadlines skip to the next future interval; missed/backpressured scheduled slots become gaps, never catch-up bursts. UTC changes affect labels only. Repeated native frames may be retained but do not establish elapsed simulation time.

Each poll performs exactly one real `bridge.observe` call with one fixed `metrics.read` item, matching outer `expectedWorld` and `args.world`, and `{signal:controller.signal}`. The real client already composes caller abort with timeout using Node18-compatible listeners/timers, caps streamed responses at4MiB and validates the group. M5 additionally verifies exactly one matching item ID, item success, original world, outer metadata and accepted M4 batch semantics before private append. Never read the direct dictionary facade to reconstruct missing source/cohort evidence.

Each in-flight attempt owns `{seriesId,generation,sequence,controller}` and one permit until the original promise settles in `finally`. Stop synchronously removes eligibility, increments generation, aborts the active controller and returns a bounded status. It records an in-flight cancellation gap once; a stale completion/finally cannot append data or a duplicate gap. **Abort never releases the permit early**, even if a test fetch ignores abort. Dispose similarly invalidates all generations and aborts/clears the timer, without setting active-permit accounting to zero. Late replies are ignored; only their own finally releases their reservation exactly once.

World mismatch/O7 `worldChanged` or observed backwards frame/reset terminally stops the series and retains original history. Ordinary transport/item/validation failures are bounded gaps and can continue at the next deadline. Native unavailable metric values remain null observations, not transport errors or zero. No retry within a poll and no new-world substitution.

Async catalog admission also needs explicit ownership: validate first, reserve one of eight retained/pending slots, capture the store lifetime generation, and bound catalog work with the same two-permit network gate. After the await, recheck disposed state, captured generation and caller cancellation before committing the reserved slot as a retained series. A failed/cancelled/disposed admission releases its pending slot once; it creates no active series. Tool `extra.signal` cancels admission. After successful start returns, collection belongs to the process-local series rather than that completed request signal. Disconnect still disposes every series. Parallel starts cannot oversubscribe slots or launch unbounded catalog fetches.

M4's current `BridgeClient.metricCatalog(family)` has no caller cancellation option. M5 may add an optional `{signal?: AbortSignal}` argument and thread it through the existing bounded HTTP reader using the accepted Node18-compatible timeout/caller-abort composition. Preserve existing catalog/transit callers and response caps. Test cancellation through actual pending catalog HTTP, including disconnect during admission; a generation check alone does not cancel that network request. Keep the shared network permit until the original promise settles.

## Retention and serialized output budgets

| Surface | Exact cap/behavior |
|---|---|
| Retained series |8 total, including stopped; ninth `SERIES_CAPACITY`, no silent deletion. Pending admissions reserve capacity. |
| Retained samples | Requested1–512, max128KiB serialized UTF8 per complete sample. Oversize -> explicit gap, never partial values. |
| Series/total |1MiB per series,8MiB total including spec, pinned definitions, samples, gaps and summaries. Reserve128KiB inside each1MiB for bounded metadata, validating admission against it. |
| Gaps |64 detailed entries per series; coalesce adjacent equal-reason ranges. Fixed-enum aged-out counters plus first/last lost sequence preserve loss after detailed entries age out. |
| Read page |1–128 samples, at most256KiB actual serialized tool result. Byte limit can reduce count; provide nextSequence. A first item that cannot fit returns explicit `RESULT_TOO_LARGE`, avoiding an empty-page continuation loop. |
| Collect/stop | At most256KiB actual serialized tool result; return compact status/spec/definition/count/span/gap summary, never dump the retained1MiB history. Stop is idempotent. |
| Compare | At most256KiB actual serialized tool result; if complete requested comparison exceeds cap, `RESULT_TOO_LARGE` with narrower-range guidance. No silently omitted pairs or substitute average. |

Use complete serialized UTF8 accounting, not JavaScript string length. Retained samples include sequence, original observation and any retained labels. Gap payloads retain bounded enum reasons/codes, not arbitrary exception text. Evict whole oldest samples for count/bytes and record their lost sequence coverage. Freeze retained data and return copies so callers cannot mutate stored truth.

Tool budgeting must include text escaping, wrappers and errors. Existing generic `jsonResult` pretty-prints into an MCP text block; measuring only its inner object is insufficient. Add one history-specific compact result encoder (or a shared bounded helper if already available), measure the **final CallToolResult JSON**, and use a fixed small bounded error fallback. If structuredContent duplicates text, both count. Do not claim these serialized limits are exact JS heap limits: object overhead and temporary buffers differ. Bounded schemas/counts/strings bound shape; the real transport's separate4MiB cap and at most two active network operations bound temporary response input. Avoid retaining duplicate text and object copies.

## Evidence preserved and actual stored comparison

Retain each original outer Observation plus `MetricBatch.sources`, `clock`, exact refs and `memberEvidence`. Never collapse source frames/freshness/warnings into one fabricated common timestamp. `refreshTimeUtc:null` stays unknown; observation UTC is not native source-refresh time. M4 review has approved producer-owned clock `sourceObservationId:'city.overview'|'state'`: consume that final field and its accepted validation, including exact source bounds. Do not guess clock origin from whether overview exists or from equal frames; overview can legitimately lack clock metadata while State supplies it. `frameEvidence:'sourceObservationEnd'` means frameIndex is that original source's end frame, not a verified time conversion. Preserve `nativeCalendarParityNotRun`, multi-frame non-atomic and sparse-sampling caveats.

Use M4's producer-owned structural fingerprint and coverage `structuralSampleOnly`; do not recompute T1/native structure in M5. Missing/incomplete evidence prevents equivalence. Different boot/epoch/member version/fingerprint, new or missing members remain descriptive; no replacement-member gain. Reject nonempty caller reconciliation receipts; used/unused stays unknown.

Comparison reads inclusive `{id,fromSequence,toSequence}` ranges from retained storage only. Require ordered safe bounds and at least two retained clock-bearing samples per range. Unknown/not-yet-observed/evicted bounds, holes and recorded skipped/error/retention gaps crossing a range produce explicit unavailable/notComparable reasons. Never silently clamp to current retention. Report actual retained and requested spans to help the caller choose valid ranges. A series may be active, but comparison uses an immutable synchronous snapshot of its current retained range, without a new read.

Require exact world, selected definitions/revision/query parameters/scope/cohort, metric units/periods/sources, complete supported values and complete unchanged member evidence. Validate every clock: finite normalized time/date in[0,1), positive days/year, stable year/days-per-year within each window, nondecreasing date, strictly increasing safe frame and normalized phase. Reject wrapping, paused zero-duration, backwards/reset and observed phase-step beyond the supplied policy. Different absolute frames/dates **between** baseline/current are allowed.

Use tracked `rawNormalizedTime` policy: endpoint phases within0..0.05 tolerance; positive phase-step bound<1; nonnegative safe frame-duration tolerance and maxSourceSpanFrames. Every sample's span is max(all source/clock endFrames) minus min(all starts); missing/reversed/unsafe evidence rejects. Match all samples in ordered one-to-one phase pairs, no reuse/interpolation. Requested interval does not establish alignment. Fully paired sparse windows can be qualified `rawNormalizedOnly`; they cannot prove continuous coverage, no unseen pause/speed change/wrap, or native hours.

Return metric definitions once plus complete ordered paired point values/deltas and first-to-last changes for stock/count metrics. No irregular unweighted averages, rate integration or cross-unit aggregation. Null/unavailable/estimated/unknown-unit values never zero-fill. Relative change is null for nonpositive baseline; nonfinite arithmetic is unavailable. `improvement:null` for every v1 metric. A512×16 comparison may exceed output budget despite bounded storage; refuse the complete response explicitly and require narrower ranges rather than truncate it.

## Implementation/test file map and execution checkpoints

New production files:

- `mcp-server/src/observation-history.ts`: strict admission, retained ring/gaps, central scheduler, real bridge reads and generation/permit ownership.
- `mcp-server/src/observation-comparison.ts`: pure immutable retained-range validation/pairing and qualified deltas.
- `mcp-server/src/observation-history-tools.ts`: strict full Zod registrations and bounded result encoding for all four tools; use SDK `extra.signal` for admission.
- `mcp-server/src/observation-history-lifecycle.ts`: actual server/transport/stream/process cleanup, injectable real event sources for tests.
- Optional `observation-history-contracts.ts` only if separating schemas/DTOs keeps the above readable; import accepted M4 types rather than copying them.

Modify `mcp-server/src/index.ts` for one store, actual tool registration and lifetime wiring. Update existing client-tool exposure accounting if necessary without inventing native endpoint descriptors or claiming allowlist exposure. Explicitly add test files in `mcp-server/package.json`; document workflow/limits in `docs/operations.md` and applicable observation docs. No C# projection change is expected for M5. Root owns portable PC handoff/plans; leave those files alone.

### Checkpoint 1: actual collector and bounded storage

- [ ] Add failing `mcp-server/test/observation-history.test.mjs` against the production store and real BridgeClient/fake loopback HTTP: authoritative catalog admission, actual one-item O7 request, source evidence survives, strict queries/world/cohort, eight retained/pending starts, no external append.
- [ ] Pin deterministic scheduler behavior: one per series/global2, fair due order, monotonic deadlines despite UTC jumps, skipped intervals without bursts, real abort signal, held permit with an abort-ignoring pending promise, stale-generation success ignored, terminal world/frame reset, no duplicate gap/release. Hold eight concurrent catalog admissions and reject the ninth; dispose during admission and reject late successful catalog commit, releasing each provisional slot once.
- [ ] Pin count/UTF8 budgets including multibyte metadata,128KiB sample boundary,1MiB/8MiB accounting,512 retention,64-gap coalescing/aged summary and exact eviction coverage. Test all collect/stop/read result wrappers at256KiB, not merely raw data.
- [ ] Run named tests first to record meaningful red (distinguish missing API/compile red from behavioral red), implement minimum consumed path, then run the same tests green and `npm run build` from `mcp-server`. Record commands/counts before review.

### Checkpoint 2: real lifecycle and registered tools

- [ ] Add `mcp-server/test/observation-history-lifecycle.test.mjs`: real McpServer/StdioServerTransport with owned stream events, preexisting close callbacks called once even when throwing, SDK cleanup continues, connect failure, idempotent EOF/close/error/signal cleanup, recoverable protocol report does not kill collection, listener/timer cleanup.
- [ ] Spawn actual compiled stdio index with fake HTTP bridge: start collection, hold an observe response, end stdin and assert HTTP closes promptly and child exits without SDK client's delayed SIGTERM fallback. Exercise explicit SDK close and SIGTERM while pending. Confirm no poll after disconnect and no mutation route called.
- [ ] Add `mcp-server/test/observation-history-tools.test.mjs` (or a clearly named existing suite) over actual MCP registration: unknown fields rejected before HTTP, cancellation reaches async admission, stop returns summary, read paging is useful, client-only exposure truth. Keep O7 actual MCP cancellation regression scoped.
- [ ] Run new lifecycle/tool tests and existing `test/observations.test.mjs`, then build. Do not rerun unavailable PowerShell/native suites.

### Checkpoint 3: stored comparison and coherent workflow

- [ ] Add failing `mcp-server/test/observation-comparison.test.mjs`: distinct absolute-frame/date but aligned normalized windows qualify; legitimate producer-declared State clock fallback survives unchanged; peak/night, missing/paused/wrapped/discontinuous clock, source-span limits, missing/gapped/evicted ranges, revision/period/source/unit mismatch do not. Exact city-empty cohort, recycled version, changed/null fingerprint, new healthy replacement after unavailable old line cannot improve the old cohort.
- [ ] Exercise baseline zero, finite-overflow delta, ambiguous point pairing, nonempty receipts and512×16 paired-output cap. Assert complete bounded output or explicit `RESULT_TOO_LARGE`; no dropped pair, interpolation or unweighted mean.
- [ ] Actual registered workflow: catalog -> observe -> collect A -> stop/read -> collect B -> compare stored explicit ranges; preserve source freshness/refresh/clock caveats and prove comparison issues zero HTTP requests.
- [ ] Implement pure comparison and wire the fourth tool, run changed suites/build plus the agreed portable integration set once, document exact counts and limitations, commit scoped source/tests/docs after root GO and request fresh independent whole-path review.

## Remaining gates and limitations

No unresolved SDK API discovery remains: underlying public server close and explicit stdio EOF hooks provide the integration seam. Their throwing-callback/EOF behavior still requires the real tests above; source inspection is not execution proof. Signal/stream listener teardown ordering must be verified against actual Node streams, not only an EventEmitter fake.

M4 remains actively implemented/reviewed. Accepted export names, semantic parser behavior, source/clock bounds and revision pin must be checked against its frozen commit before source work. Catalog admission's shared network reservation and bounded status/result DTOs are M5 implementation decisions above, not existing shipped behavior.

Native units/calendar/panel/refresh/performance remain unverified. Fixed cached getters can contain opaque nonpreemptible native work; M5 does not certify their latency or recover native historical telemetry. The whole managed workflow can be implemented and tested in cloud, while original full M5 and native acceptance remain partial. No PC packaging/deployment is part of this work.
