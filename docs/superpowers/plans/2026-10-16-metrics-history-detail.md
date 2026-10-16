# Collected city and selected-line diagnostics implementation plan

> For the next explicitly authorized implementer: execute the tasks below with the established single-implementer/independent-review workflow and test-first changes. This document authorizes no source work. Root must give GO after accepted O7 and selected T1 integration.

**Goal:** Make bounded, typed city and selected-line observations collectable and comparable without inventing native historical telemetry or gameplay authority.

**Architecture:** One native metric producer projects existing typed readers. Direct dictionary observations and the fixed O7 `metrics.read` batch entry consume that producer. A process-local TypeScript collector polls that entry; comparison consumes retained samples and their original identity, source and clock evidence.

**Tech stack:** Existing C#9/net472 BCL core, native game-thread bindings, TypeScript/Node>=18, existing MCP SDK/Zod/node:test. No dependencies or machine changes.

**Spec:** `docs/superpowers/plans/2026-10-16-collected-diagnostics.md`, original M4/M5 in `docs/superpowers/plans/2026-10-11-bridge-management.md`, and retained decisions157–161 in `docs/handoff/IMPLEMENTATION-DECISIONS.md`. This is the bounded consumed M4/M5 slice, not the complete original tasks.

## Source basis and boundaries

Inspected the current typed `Core/CityObservationData.cs`, `Core/MetricContracts.cs`, native `RequestHandlers.cs`, `.CityData.cs`, `.Economy.cs`, `.Observations.cs`, the O7 catalog/scheduler/wire/client contracts, and `/workspace/handoffs/selected-t1-design.md`. O7/T1 are being delivered by other workers: reconcile their accepted names/signatures before source changes; do not replace their producer or add an alternate observation scheduler.

`ReadOverviewData` actually reads `Game.City.Population`, `CitySystem.moneyAmount/XP` and raw `TimeSystem` metadata. Population presence is retained in the internal `PopulationAvailable` flag. Legacy numeric fields intentionally default to zero when that component is absent. `ReadLaborData` reads cached `CountHouseholdDataSystem` and `CountWorkplacesSystem` counters. `ReadBudgetData` reads `CityServiceBudgetSystem`, including bounded native category dictionaries. No extra entity scan is needed for these projections. Cached-source refresh timestamps remain unknown even when an observation is fresh.

The selected T1 page describes at most one exact line with a shared128 waypoint+segment entry budget. Its ordered refs/positions/raw distances support bounded structural evidence only. It cannot supply verified load, capacity, headway, dispatch, used/unused classification or complete route geometry/topology. In particular, `MetricContracts.DescribeRoute` qualifies the **different** legacy `UITransportLineData.length/cargo` source; its metre/passenger labels must not be borrowed for T1 segment distances.

Native G1, calendar, budget-panel and transit parity remain `notRun`. Cloud can implement and verify the complete managed producer→transport→collection→comparison path; it cannot certify native units, callback behavior, panel parity or performance. No writer, lease, native completion, save, simulation control, persistence, deployment or client allowlist changes belong here.

## Fixed initial catalog

Catalog revision is an explicit constant, initially `diagnosticMetrics.v1`; persist the complete selected definitions with each series. Keys below are literal, not arbitrary paths or a parameterized field selector. All have `parameters:[]` and `parameterDefinitions:[]`. `period:null` means a snapshot stock/count, never a rate. Each source string below is retained on its MetricValue.

| Key | Native source → typed field | Kind; unit; period | Availability and refresh |
|---|---|---|---|
| `city.population` | `Population.m_Population` → overview.population | count; `citizens`; null | Component present; live component snapshot |
| `city.populationWithMoveIn` | `Population.m_PopulationWithMoveIn` → overview.populationWithMoveIn | count; `citizens`; null | Component present; label explicitly includes native move-in counter, not a forecast |
| `city.money` | `CitySystem.moneyAmount` → overview.money | stock; `nativeMoney`; null | Finite signed raw treasury value; no currency conversion |
| `city.xp` | `CitySystem.XP` → overview.xp | stock; `XP`; null | Finite raw counter |
| `labor.employed` | `CountHouseholdDataSystem.CityWorkerCount` → labor.employed | count; `workers`; null | Cached city worker count, not a resident employment ratio |
| `labor.homelessCitizens` | `CountHouseholdDataSystem.HomelessCitizenCount` → labor.homelessCitizens | count; `citizens`; null | Cached counter |
| `labor.jobs.total` | `CountWorkplacesSystem.GetTotalWorkplaces().TotalCount` → labor.jobs.total | count; `workplaceSlots`; null | Cached counter |
| `labor.jobs.free` | `CountWorkplacesSystem.GetFreeWorkplaces().TotalCount` → labor.jobs.free | count; `workplaceSlots`; null | Cached counter |
| `budget.income` | `CityServiceBudgetSystem.GetTotalIncome` → budget.totalIncome | rate; `nativeMoney`; `month` | Native hourly-refreshed monthly rate |
| `budget.expenses` | `CityServiceBudgetSystem.GetTotalExpenses` → budget.totalExpenses | rate; `nativeMoney`; `month` | Preserve signed negative costs |
| `budget.taxIncome` | `CityServiceBudgetSystem.GetTotalTaxIncome` → budget.totalTaxIncome | rate; `nativeMoney`; `month` | Native monthly rate |
| `budget.balance` | `CityServiceBudgetSystem.GetBalance` → budget.balance | rate; `nativeMoney`; `month` | Preserve native value; convention is income + signed expenses |
| `transit.waypointCount` | T1 completeness.waypointCount | count; `waypoints`; null | Exact reported count only when line/buffer facts complete |
| `transit.segmentCount` | T1 completeness.segmentCount | count; `segments`; null | Same completeness requirement |
| `transit.brokenWaypointCount` | T1 waypoint structuralAssessment | count; `waypoints`; null | Complete inspected route with every waypoint assessed; count knownFault slots, including null references |
| `transit.invalidSegmentCount` | T1 segment structuralAssessment | count; `segments`; null | Complete inspected route with every segment assessed; count knownFault slots, including null references |

Count values require finite, nonnegative safe integers; invalid native counters become `{value:null,support:"unavailable",reason:"invalidNativeCount"}`. All other nonfinite values become unavailable with a specific reason. Do not clamp, silently rescale, substitute zero or recompute budget balance by subtracting signed expenses. A native expense sign contrary to the declared convention is retained with a warning rather than normalized. Every finite city projection still carries native-parity/source-refresh caveats.

Missing Population produces null/unavailable for the two population keys plus `populationComponentUnavailableLegacyDefaults`; money/XP remain usable. **Present** Population with zero inhabitants produces a supported zero. The actual typed flag, not a heuristic on zero or a JSON warning search, decides availability.

Use existing MetricValue `{value,unit,period,source,support,reason}` unchanged. `MetricDefinition.support` is a Capability describing the adapter as `unverified` with a native-parity reason; per-value `supported` means the declared raw field was obtained, not that native correctness was certified. Catalog `refresh` is `liveComponentSnapshot`, `cachedSimulationCounterRefreshTimeUnknown`, `gameHour`, or `selectedLineStructuralSnapshot` as appropriate. No automatic metric has an improvement direction in v1.

Do not add initial happiness/health/unemployment/homelessness ratios, moneyDelta rates, electricity/water/garbage units, route length/load/occupancy or arbitrary enum statistics. Their scale, rate period, physical unit or required reader is not proven here. The catalog is discoverable real functionality, not a long list of unused unavailable placeholders. T1 raw distance remains readable in the selected-line diagnostic page; add a length metric later only with an explicit raw-unit contract or verified native unit, never an accidental metre claim.

## Producer and wire interfaces

Create `CS2MCP.Bridge/Core/DiagnosticMetrics.cs` for BCL contracts, strict normalization, definitions and projection; use existing `MetricContracts.cs` metadata without replacing it. Create `RequestHandlers.Metrics.cs` for the game-thread binding and GET facades. Extend the accepted `ObservationCatalog`, native binding, endpoint catalog, and mirrored TS observation contracts only at fixed entries. Create `mcp-server/src/diagnostic-metrics.ts` for public schemas/tools and definitions; avoid an unrelated management framework refactor.

Recommended exact interfaces (names may be adjusted once to accepted T1 DTO names):

```text
MetricQuery = {key:string, parameters:Record<string, string|number|boolean>}
ReadScope = {kind:"city"} | {kind:"selectedLine", line:EntityRef}
MetricReadRequest = {world:WorldRef, queries:MetricQuery[], scope:ReadScope}

ReadMetricCatalog(family:string): MetricDefinition[]
IDiagnosticMetricSources: typed Overview/Labor/Budget/State delegates,
  plus SelectedLine(EntityRef):Observation<TransitPage>
DiagnosticMetricReader.Read(request:MetricReadRequest):Observation<MetricBatch>
```

The source interface returns typed city DTOs (`CityOverviewData`, `CityLaborData`, `CityBudgetData`, `CityStateData`); DiagnosticMetricReader owns their per-source observation wrapping using the same frame/UTC/warming suppliers as O7. SelectedLine already returns its original Observation and preserves it. The native RequestHandlers implementation delegates directly to the existing private typed readers. Pure tests instantiate this actual reader with counted delegates and the original DTOs; native binding tests link the actual RequestHandlers partial, following the existing O7 binding test pattern.

Implement only this consumed normalized producer plus GET dictionary and O7 batch facades. The retained parameterless-key compatibility contract is a translation to empty parameter maps at the actual public input; do not add unused native ReadMetrics overloads or extra interfaces. Register `cs2_metric_catalog` and `cs2_observe`. The latter accepts explicit world/scope and exactly one of `keys` or `queries`, translates keys immediately, and uses `BridgeClient.readMetrics(MetricReadRequest)` against the GET facade. The catalog tool uses `BridgeClient.metricCatalog(family)`; both typed methods are actual tool consumers. An unscoped API must never silently choose a current world.

Accept1–64 distinct keys; key/family/parameter strings max128 characters; at most8 named scalar parameters/query, all finite; reject duplicates, unknown fields/keys/families/parameters, arrays/nesting and unsupported scopes before any reader callback. In v1 any nonempty parameter map rejects because no definition permits parameters. City keys require explicit city scope; transit keys require selected-line scope. Reject a mixed-scope key set, area/building/facility/pagination/mode/purpose filters. No wildcard line discovery.

`GET /v2/metrics/catalog` returns the compatibility definition array; optional explicit `envelope=true` adds observed availability/build/freshness using the retained envelope ruling. Family is `all|city|labor|budget|transit`, no free-form reflection. `GET /v2/metrics` carries one bounded, shared-encoder JSON `query` value containing MetricReadRequest and returns the dictionary Observation. Preserve the existing16KiB whole-header limit; oversized encoded requests reject before enqueue and can use the existing bounded O7 POST instead. Do not increase ingress limits. Add strict GET decoding/normalization in ingress before dispatch. Both routes accept GET only and no body.

Fixed O7 descriptor is `{id,name:"metrics.read",args:MetricReadRequest}` and returns `Observation<MetricBatch>`. Its copied original world must participate in O7 admission even when outer expectedWorld is absent, including every line EntityRef. Collection always supplies both matching expectedWorld and args.world. The direct dictionary facade is explicitly `batch.data.values` with the same outer observation metadata; it does not pretend that the dictionary itself contains member/source metadata.

```text
MetricBatch = {
  schemaVersion:1, definitionRevision:"diagnosticMetrics.v1", scope:ReadScope,
  values:Record<string,MetricValue>, members:EntityRef[],
  perMember:{member:EntityRef,metrics:Record<string,MetricValue>}[],
  sources:SourceObservation[], clock:ObservedClock|null,
  memberEvidence:{member:EntityRef,fingerprint:string|null,
    coverage:"structuralSampleOnly",complete:boolean,reasons:string[]}[]
}
SourceObservation = {id,world,startFrame,endFrame,sampledAtUtc,freshness,warnings,
  completeness:"complete"|"incomplete",refreshTimeUtc:null}
ObservedClock = {source:"TimeSystem",rawNormalizedTime,rawNormalizedDate,
  rawYear,rawDaysPerYear,frameIndex,startFrame,endFrame,
  conversionStatus:"unverified",reason:"nativeCalendarParityNotRun"}
```

Every actual required typed source is read **once**, in fixed order, per batch, plus one state/clock read if not already available from a required source. Use typed provider delegates over the existing native readers, with counted callback tests; never project through arbitrary object paths or deserialize legacy JSON. Record frames/UTC immediately around each source and preserve its warming/warnings. Include no unrequested service scan, traffic scan or native history fetch. There are at most four fixed city source calls (state/overview/labor/budget) or state plus one bounded selected-line call; no per-key repeated native reads. Report multi-source/multi-frame observations as non-atomic. Source failure makes affected values unavailable with source reason while preserving successful sources; a world/readiness change fails the whole publication through O7 fencing.

Use the accepted O7 capped encoder for both routes and entry:256KiB/item,4MiB/group, finite-only JSON before publication. Preserve actual original per-source metadata through projection, not one fabricated common frame. Clock capture is an observation of TimeSystem, not source refresh time and not a conversion to hours.

## Fixed cohorts and structural evidence

`Cohort = {kind:"city",world,members:[]}` or `{kind:"fixedSelectedLine",world,members:[EntityRef]}`. The latter has exactly one member in this slice because T1 reads one explicitly selected line. `members:[]` only means the explicitly declared city scope, never all current roads/routes. Bind and deep-copy refs at series admission; validate bootId/epoch/index/version exactly and retain the original scope. No names, index-only lookup, latest-version substitution or refreshed cohort discovery.

For the line, canonicalize only T1's ordered waypoint refs, connected refs and XYZ, segment refs/raw finite distances, mode/purpose, world and completeness. Hash with a deterministic explicit canonical schema (TS built-in SHA256 over fixed field ordering is sufficient); record `structuralSampleOnly`. Missing/incomplete facts make fingerprint unavailable. This identifies changes in the sampled structure, not the full native path. Different version or fingerprint prevents an equivalence/improvement verdict; expose `missing/new/modified` descriptive classifications. A changed member cannot replace the old member in a score. No authenticated reconciliation producer exists: reject nonempty caller reconciliation receipts and leave used/unused `unknown`. Keep original full refs on all perMember values and evidence.

## Collection contracts and lifecycle

Create `mcp-server/src/observation-history.ts` for the actual collector/storage and `observation-comparison.ts` for pure stored-sample comparison. Register the four planned tools in a small `observation-history-tools.ts`; `index.ts` constructs one store and wires disposal. No API accepts caller-submitted observations or arbitrary URLs.

```text
SeriesSpec = {world,queries:MetricQuery[],scope:ReadScope,cohort:Cohort,
  maxSamples:integer1..512,intervalMs:integer1000..86400000}
createSeries(spec):SeriesId                 // validation/capacity, private to start
startCollection(spec):{id,status:"active"}
stopCollection(id):CollectedSeries          // idempotent, retain observations
readSeries(id,{afterSequence?,limit?}):HistoryPage
compareObservations({baseline:{id,fromSequence,toSequence},
  current:{id,fromSequence,toSequence},cohort,policy}):Comparison
dispose():void                             // idempotent, synchronous abort/clear
```

IDs are process-scoped opaque strings; no disk/file export, external observation append, deletion, persistence or auto-restart API. Eight **retained** series total including stopped series; a ninth rejects with `SERIES_CAPACITY`, never silently deletes another series. Admission validates/refuses malformed scope/query/cohort before reserving the slot. Stopped histories remain readable until process exit. Native capabilities advertise native metric endpoints separately from client-only collection tools; registration does not assert actual client exposure.

Use a count+byte bounded retained ring per series. Concrete caps:512 samples maximum requested,128KiB serialized UTF8/sample,1MiB/series,8MiB total including spec/definitions/sample and gap metadata. Reserve fixed per-series metadata within that1MiB cap. Cap native warnings/source strings while validating against their documented bounds; do not truncate unknown payloads into plausible values. A sample exceeding128KiB becomes an explicit oversized-sample gap, with no partial metrics append. Evict oldest whole samples until count/byte limits fit and record retention loss. Bound gaps to64 entries/series, coalesce adjacent same-reason sequence ranges, and summarize aged-out detail in fixed-enum counters with first/last lost sequence; never silently drop the fact of lost coverage. Bound returned history pages to1–128 samples,256KiB encoded; stop at the byte bound with nextSequence. Comparison can traverse the at-most512 stored samples directly, never fetch fresh values to fill gaps.

One central injected monotonic scheduler (`nowMs`, `setTimeout`, `clearTimeout`) and UTC supplier make behavior testable. At most one in-flight read per series and two global polls; round-robin selection among due series. Skip elapsed intervals to the next future deadline, recording `missedInterval` or `backpressure` counts rather than issuing catch-up bursts. An immediate first poll is allowed. UTC jumps change labels only. Paused repeated frames may be retained as repeated observations; they do not establish elapsed simulation time or an aggregate refresh.

Every poll calls the real `BridgeClient.observe` with exactly one `metrics.read` item and `{signal:controller.signal}`. Validate one matching item ID, non-null exact world, expected MetricBatch schema/revision/keys/scope/full refs, safe frame numbers, and bounded payload before private append. Do not infer unsupported fields. A native unavailable field is retained as null, not zero; transport/item errors are explicit gaps.

Stopping increments generation, aborts the active controller and clears eligibility before returning. A poll captures the series generation; its late reply cannot append after stop/dispose even if its fetch ignores abort. Hold its global permit until the original promise settles in finally, rather than starting unbounded replacement fetches on abort. The real client timeout bounds stalled reads. World mismatch, O7 worldChanged or observed backward frame/reset terminally stops the series, records a reason and preserves old history; it cannot adopt the new world. Ordinary bounded read errors produce gaps and can continue on the next requested interval. No automatic transport retry inside one poll.

Wire idempotent disposal to actual SDK/server transport close, connection failure and process SIGINT/SIGTERM; preserve preexisting callbacks, run cleanup in finally even if an earlier callback throws, remove owned signal listeners, and do not leave timers alive. Confirm the exact SDK hook in the installed package before implementing. Disposal never calls native simulation control, pause timers, writer cancellation or game cleanup. MCP disconnect drops the collector's transport requests; O7's actual transport cancellation must release read slots.

## Useful, evidence-based comparison

Comparison selects explicit **retained** sequence ranges from two stored series, with at least two clock-bearing observations each. Different absolute frame numbers and different calendar dates are expected and allowed. The caller supplies:

```text
PeriodPolicy = {mode:"rawNormalizedTime",phaseTolerance:number0..0.05,
  maxObservedPhaseStep:numberGreaterThan0LessThan1,
  frameDurationTolerance:nonnegativeSafeInteger,maxSourceSpanFrames:nonnegativeSafeInteger}
```

Use only captured evidence to construct each actual window: first/last raw normalized phase, positive `last.frameIndex-first.frameIndex`, source observation bounds and all intermediate captured clock points. Require finite normalized time/date in[0,1), positive rawDaysPerYear, stable year/daysPerYear within each window, nondecreasing raw date, strictly increasing safe frame and normalized-time values. Each successive phase step must be no greater than the caller's maxObservedPhaseStep; this is a bound on observed discontinuity, not a claim about native hours. This initial comparison supports non-wrapping windows only; midnight/year rollover, backwards/over-limit phase evidence or absent clock is `notComparable`, rather than inventing an unwrap/calendar conversion. Do not require baseline/current absolute date or frame equality.

Require equal selected definitions/revision/query parameters/world/cohort/unit/period/source, complete supported values, no gap/retention loss crossing either range, and frame-duration difference within the caller's absolute frame tolerance. At every retained point `max(all source endFrames,clock.endFrame)-min(all source startFrames,clock.startFrame)` must be no greater than maxSourceSpanFrames; missing/unsafe/reversed bounds reject. Phase endpoints must match within phaseTolerance. Match each retained point to one unique point in the other series within that tolerance, in order; require complete pairing and no reuse/interpolation. A point with no counterpart makes that metric comparison unavailable. Do not use the caller's requested schedule as evidence that observations occurred.

Report `comparable` only with the explicit `rawNormalizedOnly` qualification and `nativeCalendarParityNotRun`; this never means verified hours or continuous coverage. No finite sparse sampling can prove that the simulation never paused/sped/wrapped between samples: report that sampling limitation and reject any observed discontinuity. Different observed monthly-rate refresh behavior remains qualified by unknown source-refresh time.

Return qualified paired values/deltas and first-to-last stock/count changes; no unweighted irregular-window averages, no interpolation, no zero fill, no cumulative income integration from frame durations, and no cross-unit aggregation. Relative change is null where baseline zero/nonpositive makes it ambiguous. `improvement:null` for all v1 metrics: lower population or fewer waypoints is not inherently better, and structural fault counts do not prove service performance. A changed/new/recycled line cannot generate gain for the old cohort. Incomplete/null/estimated-unknown-unit values remain descriptive/unavailable and do not get scored. No positive-icon or inactivity failure classification exists in this slice.

## Implementation sequence and proof

### 1. Consumed M4 producer and public reads

- [ ] Add failing production projection tests in `tests/CS2MCP.CoreTests/DiagnosticMetricTests.cs`, link exact producer/native binding seams in the canonical csproj and runner. Assert every table mapping, signed costs/native balance, no rate on stocks, actual source called once for repeated-source keys, missing Population null versus present-zero supported, nonfinite/invalid counts null, and no accidental service/traffic/length/load reads.
- [ ] Observe the intended red failures, then implement the catalog/query normalization and actual native typed provider binding. Add tests for unsupported keys/params/scopes and conflicting world refs producing zero native callbacks.
- [ ] Add actual strict GET/O7 ingress, shared capped serialization, BridgeClient/schema/tool registration tests in `mcp-server/test/diagnostic-metrics.test.mjs`. Prove direct dictionary values equal O7 batch values from the same producer, source frames/warnings survive, oversized URL/body/response rejects, unknown args do not dispatch, malformed scalar numbers cannot serialize as objects. Keep legacy shape regression.
- [ ] Run targeted core/net472 build and exact-linked net8 assertions if needed, Node build and new suite; record target distinction. Commit the real consumed path after independent review; keep native G1 notRun.

### 2. Actual collection and lifecycle

- [ ] Add failing `mcp-server/test/observation-history.test.mjs` against production collector, real registration and real BridgeClient with an injected fake HTTP responder/clock. Test first poll, source-to-batch preservation, 1–64 key validation, eight retained series, one poll/series/global2, fair backpressure, UTC jumps, no catch-up bursts,512 retention and UTF8 byte caps (including multi-byte strings), bounded/coalesced eviction gaps and paged reads.
- [ ] Implement bounded store/scheduler and the collect/stop/read tools with actual abort signal propagation. Test stop/disconnect while fetch is pending, captured generation ignoring a late successful response, held global permit until settlement, world mismatch stopping without replacement, no external append, and idempotent shutdown preserving earlier callbacks (also when those callbacks throw). Assert no simulation endpoint is called.
- [ ] Run Node build/new suites and scoped O7 transport integration regression. Commit only the consumed lifecycle, not a timer utility unconnected to MCP shutdown.

### 3. Stored comparison and coherent workflow

- [ ] Add failing `mcp-server/test/observation-comparison.test.mjs`: distinct absolute-frame but aligned normalized windows compare; peak/night, missing/discontinuous clocks, zero-duration paused windows, gaps, period/revision mismatch and unknown units do not. Test exact city-empty scope, recycled EntityRef version, different fingerprint, new empty replacement line, unavailable old sample followed by healthy new sample, and baseline zero. Assert no generic improvement or native-history claim.
- [ ] Implement actual comparison tool over the same retained histories. Add end-to-end registration test: catalog→observe→collect A→stop/read→collect B→compare selected stored ranges; verify all source identity/freshness caveats survive.
- [ ] Document that workflow in `docs/operations.md`, metric/support tables and Node test script registration. Run changed suites, portable integration set and build once; request independent whole-path review before draft publication. No missing-PowerShell/game probe reruns.

## Review focus and remaining unknowns

1. Absent Population must not become a population crash/rebound: production typed flag plus native binding test, not just a fake JSON shape.
2. An old poll completing after disconnect must not append or free a permit twice: actual transport abort plus generation/finally tests.
3. Similar frame counts at peak versus night must not compare: captured normalized phase pairing, not user-entered labels or absolute-frame equality.
4. Rebuilt routes/new empty members must not improve old cohorts: exact refs and qualified structural fingerprint, no trusted caller receipts.
5. Count caps must also bound bytes/strings/gaps and tool responses: UTF8 multi-byte and oversize-source tests through the production encoder/store.

Execution is blocked only on explicit GO and accepted O7/T1 interfaces, not a new platform preflight. Confirm whether selected T1's final DTO still names candidate raw length `lengthMeters`; M4 must consume no such metre claim without source evidence. The installed MCP SDK's actual close callback wiring must be read before implementing disposal. Native unit/calendar/panel/refresh/performance semantics remain later PC fixtures. Those unknowns do not block the bounded raw city counters or the real client collection lifecycle described here, but they prevent full original M4/M5 or native validation completion claims.

Root accepted this design's scope on October16: no T1 length metric without actual unit proof, no unused overload/scaffold, and independent checkpoints for the consumed managed metrics path followed by the client lifecycle/comparison path. Root requested T1's aggregate raw-distance field be named/qualified without premature metre authority. Await explicit source GO after T1 review.

This design-only pass ran no tests, game requests, dependency installation or source edits. The only deliverable written is this handoff document outside the repository.

## Accepted T1 assessment handoff

T1 supplies per-slot `structuralAssessment: knownFault|noKnownFault|unassessed`.
M4 consumes this production classification rather than duplicating its predicates
or counting the non-null reference lists. Exact fault totals require complete
buffer traversal and every relevant slot assessed. Null required references can
be known faults without a reusable EntityRef. Missing PathInformation/distance is
unassessed, while optional position/waiting metadata is separate from reference
faults. Any unassessed relevant slot makes the exact aggregate unavailable, not
zero. Structural assessment remains distinct from native route validity.
