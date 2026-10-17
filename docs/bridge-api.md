# Bridge API

The current source candidate has **76 native endpoint descriptors and 79 registered MCP
tools**, verified by the catalog and offline MCP discovery tests. The legacy
manifest retains `protocolVersion:1` / `schemaVersion:1` and advertises managed
`operationProtocol.version:2`. Strict HTTP method/framing checks are implemented;
the three [operation endpoints](operations.md) remain disabled pending completion
and lease guards. [Partial C1 inspection](#bounded-explicit-construction-inspection-partial-c1)
is a bounded read-only addition. [Observation groups](observations.md) add a fixed
read-only JSON POST catalog with shared scheduling and transport detachment. Native G1/G2/CON-01 remain `notRun`.

## Historical release 1 baseline

Release 1 introduced `GET /capabilities` / `cs2_capabilities`, with 66 endpoint
descriptors/tools. At that baseline, protocol 2 and method enforcement were future
work. Subsequent O2 and partial C1 source changes are described above. Existing
legacy route and success field names remain available; unavailable ping evidence
becomes nullable as documented below.

## Discovery and exposure

`/capabilities` returns `identity`, `runtime`, `endpoints`, `capabilities`,
`nativeSupport`, `degradedReasons`, `nativeValidation` `operationProtocol` and `observationProtocol`.
`EndpointCatalog.All` in
[EndpointCatalog.cs](../CS2MCP.Bridge/EndpointCatalog.cs) is the authoritative
classification of the current 76 native endpoint descriptors. There are 79 registered
MCP tools:75 endpoint consumers plus four bounded process-local history tools. The additional fixed lifecycle-history GET shares the existing O7 consumer. These include discovery, three operation tools, partial C1 inspection,
selected transit, typed diagnostic metrics and bounded observation groups.
Most native endpoints name their corresponding MCP capability; the fixed lifecycle-history GET is consumed by the existing O7 tool. The four
[M5 collection/comparison tools](observations.md#collected-diagnostic-history--bounded-m5-slice)
appear separately as MCP `clientOnlyCapabilities`; they add no native endpoint or
writer authority.

`capabilities[].support: supported` means the bridge implements that endpoint.
It does not establish a loaded city, native correctness, or client visibility.
`nativeSupport` separately marks existing native endpoints `unverified` with
`nativeFixtureNotRun`. The legacy transit handlers explicitly reject airplane
stop/line creation; `transitPassengerAirCreation` is therefore `unsupported`.
This says nothing about listing airplane connections or future cargo features.
No native fixture results have been supplied by this offline release.

`cs2_capabilities` adds `identity.mcpVersion` and an `exposure` report. Its optional
`clientTools: string[]` argument supplies the tool names actually visible in the
calling client. No client list means `clientTools: null` and
`clientExposure: unknown`. An empty supplied list means no tools are exposed.
An implemented capability absent from the list has `clientExposure: notExposed`;
its bridge support remains unchanged. A listed name has `clientExposure: exposed`.
`registered: true|false` is independent evidence from successful MCP registration
calls, including tools registered through the JSON helper. `registeredTools`
contains those observed names. The client list is evaluated in Node and is not
sent to the bridge. Discovery never edits a client allowlist.

Example MCP arguments: `{ "clientTools": ["cs2_ping", "cs2_budget"] }`.
Omit the argument when client visibility is unavailable.

## Identity and runtime evidence

`identity.assemblyVersion` comes from the loaded bridge assembly (normally
`0.4.0.0` for package version `0.4.0`); `modVersion` is the separate mod product
version `0.9.0`. The Node package/advertised MCP version is `0.8.0`. These values
are separate identities and are never substituted for a game version.

`buildIdentity` is the assembly's informational version when embedded by the
build. `sourceRevision` comes from embedded `SourceRevisionId` metadata or an
exact hexadecimal 40/64-character revision suffix in that informational version.
The running bridge does not read the checkout or guess the deployed commit.
An embedded revision is the build's recorded source revision; it does not attest
that the source tree was clean when compiled.
Missing build/source evidence is null with `buildIdentityReason` or
`sourceRevisionReason`. `gameVersion` is currently null with
`gameVersionReason: gameVersionNotObserved`; native handoff must obtain reliable
game-version evidence before making a compatibility claim.

`runtime` contains `observedAtUtc`, `ageMilliseconds`, `gameMode`, nullable
`isLoading`, nullable `cityLoaded` and `reason`. BridgeSystem publishes an
immutable managed snapshot on the main thread during creation, UI updates
(including paused updates), and load completion. Listener threads atomically
read this snapshot; they never access GameManager, Unity or ECS.

Before publication, native fields are null with `noMainThreadObservation`. If no
update arrives within 5,000 wall-clock milliseconds, or the UTC clock moves
backwards, native fields become null with `mainThreadObservationStale`; the last
observation time/age remain visible. Native observation failures also return
null fields with a reason. A fresh observation remains cached evidence and may
precede a transition that has started since its timestamp. Loading-start and
paused/main-menu behavior still require F6 native verification.

`degradedReasons` reports missing native/build/game evidence and transient
`gameLoading` or `gameNotLoaded` when observed. Main menu and loading do not
change bridge support to `unsupported`. Listener liveness remains independent
of main-thread availability.

`/ping` retains `ok`, `mod`, legacy `version` (the mod product version),
`gameMode` and `isLoading`, and adds `identity` and `runtime`. The two legacy
native fields now use the cached observation and can be null when unavailable
or stale. Consumers should migrate to `runtime` freshness/reason checks;
`ok: true` establishes listener liveness only. `/capabilities` also replies on
the listener thread without waiting for native dispatch.

## Endpoint classifications and completion boundaries

Each descriptor contains `path`, `legacyHttpVerbs`, `access`, `capability` and
`completionMechanism`. Despite its retained name, `legacyHttpVerbs` is enforced
by the current strict ingress before dispatch: legacy routes and partial C1
inventory accept GET; command submission, operation cancellation and bounded observation groups accept POST.
Other methods reject. The historical release1 manifest only documented legacy
MCP usage, including GET writer routes, without enforcing it.

| Access | Meaning and examples |
|---|---|
| `read` | Observations, screenshots and discovery; native reads still require their existing main-thread dispatch. |
| `cityWrite` | Persistent city writes, including loan, policy UI queues, tile purchases and asynchronous saves. |
| `uiWrite` | Camera movement (`/camera/set`). |
| `control` | Simulation pause/speed and scheduled runs. |
| `nativePreview` | Reserved; no existing route performs native preview validation. |

Roadside and shoreline placement are conservatively `cityWrite`, even though
their dry-run branch returns calculated geometry synchronously. That geometry
does not validate native preview entities. Internal transit rename and tile
refresh work are dependencies of their originating route, not additional HTTP
routes.

| Completion mechanism | What the current reply establishes |
|---|---|
| `off-thread-liveness` | Listener answered; native evidence is cached. |
| `off-thread-snapshot` | Managed manifest/snapshot serialized without a native-thread wait. |
| `sync-read` | Handler built an observation; freshness/native parity are separate. |
| `explicit-entity-read` | Partial C1 sampled the bounded explicit refs with before/after world validation; root bounds and native verification limits remain explicit. |
| `sync-set` | Setter/buffer write returned; some routes read back, others echo input. Native downstream effects may follow. |
| `loan-action-queued-ack; pending` | `/city/loan/set` queued a LoanAction; effective principal remains pending until a later observation. Zero requests treasury-limited repayment and does not establish full repayment. |
| `end-frame-ack` | UI/barrier work queued; barrier playback and downstream work are later. |
| `tool-apply-ack` | Generic tool selected Apply; commit/tool restoration are not independently verified. Placement dry runs instead return calculated geometry. |
| `tool-finish-readback` | Road Finish performed spatial result matching, without exact identity/preservation proof. |
| `transit-finish-readback` | Resulting entity exists and is neither Temp nor Deleted. |
| `transit-finish+rename-ack` | Committed line checked; optional rename setter acknowledged, without rename barrier verification. |
| `purchase-readback+refresh-queued` | Tile ownership/money read back; border refresh queued for a later ToolUpdate. |
| `run-scheduled` | Target frame/speed installed; timed-run completion is later. Cancellation clears the target and pauses synchronously. |
| `save-started` | Native asynchronous save invoked; `saving: true` is not a completed save or asset identity check. |
| `end-frame-capture` | Screenshot coroutine produced actual PNG bytes. |

The catalog describes existing boundaries; it does not upgrade acknowledgments
to verified completion. Always reconcile uncertain outcomes before retrying.
Errors retain the legacy `error` string plus F2 `errorData` with code, message,
retryability, outcome and available status/native/operation details.
Generic errors and old-server untyped errors retain `outcome: unknown` and
`retryable: false`, including HTTP 409 interruptions or uncommitted result
readbacks after possible Apply. HTTP status alone never proves no effects.
Only explicit known pre-apply rejections carry `notApplied`; reconcile uncertain
outcomes against city observations before retrying.

## Metric contracts and legacy fields

F4 adds metadata objects with their own `schemaVersion: 1`; the manifest remains
protocol 1/schema 1. Legacy numeric values are preserved without rescaling,
absolute-value conversion, date correction or cargo-unit guesses.

| Endpoint / legacy raw fields | Additive metadata and interpretation |
|---|---|
| `/city/demand`: `*.buildingDemand`, `*.companyDemand`, residential `householdDemand`, signed factors | `metricMetadata` declares `buildingDemandMin: 0`, `buildingDemandMax: 255`, `buildingDemandUnit: nativeDemand`, `companyDemandUnit: nativeCounter`. Company counters may exceed 255; they are not percentages. Demand refreshes during simulation updates and can remain stale while paused. |
| `/city/budget`: `totalIncome`, `totalExpenses`, `balance`, `moneyDelta`, `income`, `expenses` | `metricMetadata.ratePeriod: month`, `refreshPeriod: gameHour`. `totalExpensesConvention: signedNegativeCosts` identifies the native signed aggregate; `expenseComponentsConvention: positiveCosts` describes `expenses` components. `balanceConvention: totalIncome + totalExpenses`. `moneyDelta` remains a raw native value. Native panel parity is `unverified` with `nativeBudgetParityNotRun`. |
| `/transit/lines`: each row's `lengthMeters` and `passengersOnBoard` | Both are **deprecated raw aliases** retained unchanged. Row `metricMetadata.rawLength` comes from `UITransportLineData.length`; `validatedLengthMeters` is that value only when finite and nonnegative, otherwise null with `lengthStatus: invalid` and `lengthWarning`. `lengthValidation: finiteNonnegativeOnly` describes this numeric check, not native path correctness. |
| `/transit/lines`: `isCargo`, deprecated `passengersOnBoard`, `usage` | Row `metricMetadata.rawLoad` preserves `UITransportLineData.cargo`; `loadKind` is `passengers` or `cargo`. Passenger `loadUnit: passengers` has `loadUnitStatus: sourceDefined` and the source reason. Cargo `loadUnit: unknown`, `loadUnitStatus: unverified`, `loadUnitReason: nativeCargoAmountUnitNotVerified`; cargo 12 is not 12 passengers or a claim of tonnes. `usage` remains the raw panel helper value. Native route parity is `unverified`. |
| `/state`: `simulation.gameDateTime`; `/city/overview`: `gameDateTime`, `gameYear` | `calendarMetadata` (under `simulation` in `/state`) preserves `rawNormalizedDate`, `rawNormalizedTime`, `rawYear`, `rawDaysPerYear` directly from `TimeSystem`. Legacy formatting still uses `GetCurrentDateTime()`. `conversionStatus: unverified`, `reason: nativeCalendarParityNotRun`; no calendar conversion is asserted to match the displayed panel. |
| `/city/service-budgets`: `services[].buildings` | Each row's `buildingCountMetadata.rawCount` preserves the same getter value; `source: CityServiceBudgetSystem.GetNumberOfServiceBuildings`, `status: unverified`, `reason: nativeServiceBuildingCountParityNotRun`. Count inclusion/aggregation semantics remain unresolved. |

For example, monthly income 100, signed aggregate expenses −40, positive
component costs 40 and balance 60 describe the same budget. No arbitrary raw
field is normalized with `abs`. Invalid native route length −5 remains −5 in
the deprecated alias and `rawLength`, while `validatedLengthMeters` is null.
Non-finite raw floating values follow the existing serializer's representation;
the qualified length is always null for them.

`/city/taxes.allowedRange` is the allowed setter range.
`taxRates.*.effectiveRateSpan` describes the span of current effective rates
across resources/job levels and must not be used as the setter range. Existing
unlock/staleness warnings remain applicable. The read endpoints do not unpause
the city to refresh their measurements.

Road and connected-road `(cx, cz)` arguments are quadratic curve **control
points**, not through-points; a curve need not pass through that coordinate.

## Optional road-graph curve readback

`cs2_road_graph` accepts optional boolean `includeCurve` (default false). The
existing `GET /city/road-graph` endpoint receives `includeCurve=true|false` when
explicitly supplied; omission preserves the default. `allNets:true` still maps
to `nets=all` to include rail/other networks, and existing area, prefab query and
limit filters are unchanged.

When enabled, each `roads[]` row adds `curve: {a,b,c,d}`, where every point is
`{x,y,z}` copied directly from the segment's native `Game.Net.Curve.m_Bezier`.
Coordinates are world-space meters; `y` is world elevation, not a height offset
above terrain. `a` and `d` equal the existing `start` and `end`; `b` and `c` are
cubic Bezier control handles and need not lie on the segment. Coordinates are
not rounded or flattened. With the option omitted or false, `curve` is null.
Legacy `start`, `end`, rounded `length`, graph connections and other fields
retain their existing meanings.

This is raw diagnostic readback on the existing read endpoint. It does not infer
lane ownership, validate snapping/connectivity, or certify native geometry.
Native parity remains unverified; this addition does not complete the planned
C1 inspection or enable any new writer capability.

## Setter values and normal stop discovery

F5 adds `setterReadback` with `schemaVersion: 1`, `requested`, nullable
`effective`, nullable `range` (`min`, `max`, `source`), nullable `step`,
`verification` (`pending`, `verified`, `unsupported`), `mechanism`, `warning`
and `nativeParityStatus: notRun`. Unknown steps are null, not invented UI
increments. `verified` means the named getter or stored buffer was observed;
it does not prove every gameplay effect completed or native mutation parity.
A getter can observe a different value from the candidate. A missing/nonfinite
getter result leaves `effective: null` and `verification: pending`.

| Setter | Constraints and readback boundary |
|---|---|
| `/city/taxes/set` | Keeps the legacy clamp to loaded `m_TotalTaxLimits`; `appliedRate` is its candidate and `newRate`/readback use `GetTaxRate`. Step 1 describes integer representability; native UI step is unverified. `/city/taxes.constraints` reports its source. Invalid tax areas reject before calling the native setter. |
| `/city/service-budgets/set` | Keeps the legacy 50–150 clamp; native UI bounds and step are unverified. `appliedPercent` and `efficiencyPercent` describe the candidate. Readback uses `GetServiceBudget`, which can return a different value when the native system ignores an uncollected service. `/city/service-budgets.constraints` reports the bridge range and null step. MCP still requires an integer within 50–150. |
| `/city/loan/set` | Keeps the legacy bridge clamp to 0–creditworthiness. Native quote candidate is `effectiveCandidate`; the native lower principal bound depends on treasury cash, and `/city/loan.constraints` exposes that bound. `ChangeLoan` queues a `LoanAction`: readback stays pending with null effective. `currentLoan` and `observedPrincipal` are immediate observations, never completed application evidence. Read `/city/loan` later to reconcile. |
| `/city/fees/set` | Strict finite, adjustable-resource, loaded UI range validation: minimum 0, maximum `FeeParameters.m_Max`. Step is unknown; no synthetic 0–100/5 game range is asserted. `newFee` remains the legacy candidate; actual readback uses `TryGetFee`. Parking is excluded by the native service-fee UI. A missing parameter singleton returns `NOT_READY` before mutation. |
| `/transit/lines/policies/set` | Checks finite adjustment against loaded `PolicySliderData.m_Range` and positive `m_Step` before queuing; omitted adjustment retains legacy 0 and must satisfy the same constraints. Step 0 means unknown/no grid. `SetPolicy` queues through `EndFrameBarrier`: active/adjustment remain legacy request fields and readback is pending. Read the policy buffer through `/transit/lines/policies` later. |
| `/transit/lines/vehicles/set` | Existing transport type, purpose, primary/carriage roles and locks still apply. Readback rereads `VehicleModel`; it verifies the stored selection, not dispatch or changes to running vehicles. Numeric range/step do not apply. |

`/city/fees.fees.*.sliderRange` is a **deprecated, incorrectly named raw
alias**, preserved unchanged: min is internal income, max is export income,
defaultValue is raw signed imports. `financialRates` supplies correctly named
`incomeInternal`, `incomeExports`, and native UI `expenseImports` (negated raw
imports); its period and refresh timing remain unknown pending verification.
`constraints` comes from loaded `ServiceFeeParameterData.GetFeeParameters`
and includes the real default, maximum, adjustability and null step. This
does not silently reinterpret the old fields. Numeric Parking=12 was resolved
from installed assembly enum metadata; native UI `SetServiceFee` excludes it.
Static metadata is evidence of API structure, not live accepted-value parity.

Normal roadside prefab listing and resolution share the same predicate:
passenger bus/tram, native `PlaceableObjectData` role, and neither native
`PlaceholderObjectData` nor `PlaceholderObjectElement` roles. Internal or
localized name matching does not define placeability. Existing route-stop and
station/platform discovery keep their existing path. The prefab-instance
coverage of this role predicate remains unverified until F6 observes it.

`/transit/stops.prerequisites` and each normal prefab's prerequisites use
schema 1. A loaded city that has not ticked reports `freshness: warming` and
unknown unlock state. Warmed prefab lock observations are separated from
`unlockNodes: unknown`, observed depot count, observed stop count and
`dispatchReadiness: unknown`. Listing counts describe the requested filter;
they do not prove spacing or valid route construction. Depot absence does
not prevent line creation, and a depot count does not prove activation,
road/track connectivity or vehicle availability. Lock rejections report
`NOT_READY` while warming and `LOCKED` otherwise, without assuming a missing
milestone caused the lock. Discovery performs no simulation control call.

F5 read-only native prerequisite/range observations in F6 and disposable
setter mutation fixtures in O3 remain **notRun**. City/district policy,
camera, junction and simulation writers retain the existing F3 completion
boundaries; this slice does not claim to verify every writer.

## Offline validation and native handoff

Run `npm --prefix mcp-server test`, then build the core test runner and execute
its `contracts`, `endpoints`, `metrics` and `values` suites from the repository root. Tests use
explicit ephemeral fake bridge URLs and never import the executable MCP entry
point. The endpoint suite compares the catalog against dispatcher cases and
listener routes, and exercises runtime/identity/manifest contracts without game
assemblies. Compile the mod with `-p:SkipDeploy=true` and the configured GamePath.
The metric suite links the pure `MetricContracts.cs` source and checks demand
scale, budget conventions, invalid/valid route length, cargo units, and qualified
calendar/count evidence. `tests/contracts/metrics.json` supplies demand 0/255,
signed budget and cargo/negative-length fixtures to real MCP transport tests.
The values suite checks strict bounds/steps, finite values, explicit legacy
clamping, pending/observed readback and synthetic stop roles/prerequisites.
`tests/contracts/setters.json` supplies explicitly synthetic deferred/warming
responses to the real MCP adapter; it does not emulate or prove native setters.

F6 must read `/ping` and `/capabilities` at main menu, during loading and in a
loaded disposable city, including paused behavior. That native check is
`notRun` for F3. Compilation and offline fixtures do not prove native parity,
save completion or compatibility. No deployment is part of this task.
F6 budget/calendar/service-count and route panel comparisons also remain
`notRun` for F4. Cargo amount units need the later T1 native verification.

F6 adds a registered read-only `contracts` runner and a schema-1 evidence
validator. See [testing and release gates](testing.md) and the [native evidence
procedure](validation/contracts.md). Offline validation never contacts the
bridge. Required failed/notRun observations return incomplete evidence, and
unknown game/build/world identity cannot claim compatibility. Current loaded
module hash and WorldRef are unavailable, so automatic collection leaves native
identity/panel checks notRun. No existing endpoint or nativeSupport claim changes.

## Bounded explicit construction inspection (partial C1)

Observe `operationProtocol.world` and readiness from `GET /capabilities`, then
supply that exact identity and already discovered versioned entity refs. This
read-only surface does not discover IDs or establish native placement safety.
For this illustrative observed world/ref:

```json
{"world":{"bootId":"example-boot","epoch":7},"entities":[{"world":{"bootId":"example-boot","epoch":7},"index":123,"version":4}]}
```

call MCP `cs2_construction_inventory` with the object above, or issue the exact
compact GET equivalent (substitute live observed values):

```text
GET /v2/construction/inventory?bootId=example-boot&epoch=7&entities=123%3A4
```

Multiple refs use a percent-encoded comma-separated `index:version` list.
The typed client uses the shared query encoder, preserving spaces and literal
plus signs. Only `bootId`, `epoch`, `entities` and optional
`relationships=directDepth1` / `geometryDiagnostics=sampledCubicV1` or the three
`relationshipKind` / `relationshipOffset` / `relationshipLimit` page keys are accepted;
duplicate decoded keys, malformed escapes, unknown keys, spatial/cursor/family
expansion/limit options and bodies reject. Decoded boot ID is 1–128 characters
and nonblank; epoch is an integer from 0 through 9007199254740991. Index/version
are positive Int32 integers. Require 1–32 distinct indices; even two versions
of one index reject. The encoded query is at most 4096 characters and the compact
entity value at most 704 characters. World mismatch/loading rejects before any
entity lookup; identity/readiness is checked again before returning.

Response uses the shared `Observation<ConstructionSnapshot>` shape: `data`,
`world`, `sampledAtUtc`, `startFrame`, `endFrame`, `freshness`, and `warnings`.
The shared contract allows null world/frame evidence; this producer requires a
ready observed world and supplies both native frames and a UTC sample timestamp
at the end of reading. `freshness="current"` means sampled in that ready current
world, not native verification or complete geometry coverage. Warnings include
`partialExplicitEntityInventory`, `rootGeometryOnly` and
`nativeConstructionFixtureNotRun`. If start/end frames differ, it also reports
`multiFrameReadNotAtomic`; a multi-frame read is never atomic, and equal frames
do not establish an immutable ECS snapshot.

`data.entities` preserves request order including per-entity unavailable reasons;
`data.requestedEntities` states the exact selection. Finite network cubics retain XYZ and versioned ends. Root
OBBs retain rotated local centers and axes, labeled `rootGeometryOnly`. Unknown
measurements are null with reasons, never zero. `data.scope="explicitEntities"`,
`support="partial"`, `worldBounds=null`, `worldBoundsReason="notMeasured"`,
`nextCursor=null`, `truncated=false`, `nativeVerification="CON-01:notRun"`.

Optional `relationships:"directDepth1"` on the same MCP request (or GET flag)
adds fixed direct instance sources and related geometry. The fixed O7
`construction.inventory` arguments accept the identical flag and producer. It
reads Owner, Attached parent, SubObject, InstalledUpgrade, SubNet and SubArea;
these labels name stored references, not certified platform/family/connector roles.
Omit the flag for the original root-only native read pattern; the additive
`relationships` result is then null.

The expansion first checks all root liveness/source headers, reserving at most
128 total scalar/buffer slots before any root geometry or relationship value is
read. Exceeding this global count refuses with `RESULT_TOO_LARGE`, without a
prefix or additional-target read. Null and duplicate slots consume budget and
remain visible. Related targets are deduplicated by full world/index/version,
reusing explicit roots and reading at most128 additional targets. They provide
existing building root OBB/network cubic facts or explicit unsupported/missing
reasons. Target buffers and owner chains are never recursively traversed.

`relationships.sources` contains six ordered summaries per root; presence,
observedCount, visitedCount and coverageComplete describe source slots only.
Missing sources have unknown count; present empty buffers have complete zero
slot coverage. Links retain source/kind/slot and nullable target, unavailable
reason/raw malformed pointer evidence, and targetIndex into the compact target
table. `coverage="directSourceSlotsOnly"` never means complete facility coverage.
Expanded unknown installed-upgrade state remains null with
`fullUpgradeDescriptionNotMeasured`; direct upgrade links are separate evidence.

Direct HTTP and O7 items independently cap actual finite UTF8 JSON at256KiB;
O7 groups retain4MiB. The typed direct and O7 clients validate original identity,
ordered cardinality, geometry, source counts and link indices before returning.
The direct MCP tool separately caps its escaped text wrapper at256KiB. Oversize
complete results refuse rather than omit links. A maximum-count request need not
fit worst-case strings; select fewer roots. No endpoint or tool was added.

The capability is read-only / `degraded` (`explicitEntitiesAndDirectRelationshipsNativeNotRun`);
its native support is `unverified`. Full C1 and native G1/G2/CON-01 remain open.
See [supported fields and deferred evidence](native/construction-gates.md).
Existing `PrefabInfo` and `RoadGraph` payloads and midpoint-filter defaults retain
their current behavior.

### Recovering large relationship reads

The existing inventory tool and O7 `construction.inventory` item accept a raw
reference page for exactly one root:

```json
{
  "world": {"bootId": "example-boot", "epoch": 7},
  "entities": [{"world": {"bootId": "example-boot", "epoch": 7}, "index": 123, "version": 4}],
  "relationshipPage": {"kind": "subObject", "offset": 0, "limit": 32}
}
```

All three page fields are required. Kind is `owner`, `attached`, `subObject`,
`installedUpgrade`, `subNet` or `subArea`; offset is an integer0..2147483647 and
limit1..32. The corresponding GET uses `relationshipKind=subObject`,
`relationshipOffset=0` and `relationshipLimit=32`, all or none. Page mode cannot
combine with `relationships:directDepth1`; root geometry diagnostics may coexist.
There is no new tool, endpoint, global discovery or automatic retry.

`data.relationshipPage` returns all six source headers and only the selected
window's slots. Null, invalid and duplicate references keep their original slot
positions. A positive ref has status `referenceObserved`: target existence,
geometry, role and usability have not been checked. `additionalTargetReads=0`
and `targetResolution=notRequested`. Choose returned full refs and explicitly
call ordinary inventory on up to32 distinct indices for geometry; missing or
unsupported targets can still be returned there. Reading all automatic target
geometry is deliberately separate from reading attachment references.

`observedCount` and `endObservedCount` describe the selected source in this
observation; absent/unavailable sources keep null counts and reasons.
`returnedCount`, `visitedSlots`, `endExclusive` and `nextOffset` describe only
the selected kind. Continue by explicitly passing nextOffset as the next offset.
Offset equal to an available count returns an empty terminal window; a larger
offset rejects. Absent/unavailable sources require offset0. A count may exceed128
without causing unbounded allocation or scans: only the requested1..32 slots are
indexed. No skipped slots or target buffers are visited.

`sourceCoverage=complete` requires offset0 and coverage of the entire currently
observed source. A final page from a nonzero offset remains `partial`; it cannot
prove earlier pages saw the same contents. Nonselected headers are not marked
visited or complete. `windowComplete` concerns this one window only. The existing
root-list `truncated=false`/`nextCursor=null` does not assert relationship completeness.

Pages explicitly report `consistency=livePageNotSnapshot`,
`crossPageConsistency=unverified` and warning `relationshipPagesNotAtomicSnapshot`,
even when frames/counts match. Same-length replacement or reordering can change
membership between calls. A source presence/count/liveness change detected during
one page refuses with `RELATIONSHIP_SOURCE_CHANGED`; the caller decides whether
to restart manually. There is no stable cursor, saved snapshot or automatic
aggregate-completeness claim. Complete facility, platform and connector semantics
remain unknown.

Existing work and output caps remain. DirectDepth1 still reserves at most128
total slots before geometry or any slot dereference. A page reads one root,
two fixed six-header sets and at most32 guarded selected slots, with zero target
geometry reads. Headers/count checks are distinct from element visits and do not
promise native callback latency. An optional root profile adds at most17 pure
evaluations. Results still refuse whole on direct256KiB, O7 item256KiB/group4MiB
or escaped dedicated MCP256KiB pressure; they never silently reduce a page.

The dedicated MCP error preserves `RESULT_TOO_LARGE` and adds a finite `reason`:
`relationshipSlotLimit`, `inventoryByteLimit`, `observationItemByteLimit`,
`observationGroupByteBudget`, `transportResponseByteLimit` or `mcpResultByteLimit`
when that boundary establishes the cause. Known source messages are mapped
exactly; arbitrary bridge text is not echoed. Unknown older failures retain
`unknownLimit`/`unknownCause` and manual recovery guidance. Offset errors use
`relationshipOffsetOutOfRange`; detected source changes use
`relationshipSourceChanged`. O7 item errors retain canonical known producer
messages. These diagnostics do not identify which limit caused any earlier native
playtest refusal without its raw response. The new page workflow is offline
verified; future native validation remains required.

For O7 batches containing inventory, an outer HTTP refusal uses the same finite
diagnostics and a measured256KiB error-envelope cap, including text/structured
wrappers. It does not attribute an unknown group failure to any particular item.
Inventory item errors retain ordered IDs and safe canonical code/messages;
arbitrary legacy prose, nativeErrors and operationId fields are omitted. Other
item data remains unchanged. This error cap does not limit successful O7 groups
to256KiB: valid64-item success responses retain the4MiB transport bound.

### Computed network profiles (read-only C5 slice)

Add `geometryDiagnostics:"sampledCubicV1"` to the same inventory request or fixed
O7 item. The producer uses the already captured root cubics; it performs no
additional native getter or relationship-target profile read. Omission accepts
older responses without the additive nullable `data.geometryDiagnostics` field.
When requested, the field and its ordered root profiles are required.

Each available root curve supplies17 ordered samples at `t=i/16`, including
endpoints, with XYZ and individual `{value,support,reason}` for signed grade,
3D curvature and 3D osculating radius. Building, unsupported and missing roots
retain unavailable profiles with reasons. Degenerate curves still retain all17
samples; undefined quantities stay null. Summaries report supported/unavailable
counts and sampled min/max with t/location; equal extrema select the earliest
sample. `returnedSamplesOnly` and `betweenSamples:"unmeasured"` explicitly deny
continuous/global bounds between samples.

Grade is dimensionless rise/run along a→d, using Y vertical and the XZ horizontal
derivative. This is parameter orientation, not lane travel direction. Curvature
uses the full3D derivatives, not a horizontal road-turn-radius model. Curvature
and radius use inverse/native-world-coordinate units; physical metre conversion
and engineering suitability remain unverified. Native reported length stays in
the original network field; no chord/polyline length replaces it.

Zero horizontal derivative makes grade unavailable. Stationary derivatives make
curvature/radius unavailable. Supported zero curvature has null radius with
`zeroCurvatureUnboundedRadius`. Scaled arithmetic detects representability loss;
an overflow/underflow affects the quantities it prevents, with independent
reciprocal availability and explicit reasons. Unavailable positions retain their
own reason. No NaN/Infinity is published. Detected underflow/conditioning loss
is reported rather than clamped to supported flatness; arithmetic uses double precision.

At most32 selected roots yield544 pure sample evaluations. Rich profiles may
exceed the unchanged256KiB direct/item/MCP or4MiB O7 group budgets: select fewer
roots after whole `RESULT_TOO_LARGE` refusal. Samples are never silently dropped.
World/frame/freshness and partial/native-notRun warnings remain intact. This
computed observation supplies no design threshold, collision/clearance, terrain,
prefab acceptance, directed-lane, native-preview or improvement verdict.

## Observation groups (partial O7)

See [the exact request, fixed catalog, result semantics and bounds](observations.md).
`POST /v2/observations` / `cs2_observe_batch` are read-only. Capabilities advertise
`observationProtocol` limits and degraded `fixedBoundedReadersOnlyNativeNotRun`
support; native support remains unverified. This does not enable mutation batches
or change the three operation-route refusals.

## Typed diagnostic metrics (partial M4)

`cs2_metric_catalog` / `GET /v2/metrics/catalog` discover twenty-six fixed raw
metrics. Optional `family=all|city|labor|budget|transit` filters the compatibility
array. `envelope=true` is an additive GET option returning definitions, revision,
managed world/readiness/freshness, sample UTC and build identity; its freshness
reason is `managedReadinessOnlyNativeParityNotRun`. The MCP catalog tool and
`BridgeClient.metricCatalog(family)` consume the definition array. Native metric
and panel parity remain unverified.

`cs2_observe` accepts an observed `world`, explicit `scope`, and exactly one of
`keys` or `queries`. Keys translate to `{key,parameters:{}}`. The normalized
request is:

```json
{
  "world": { "bootId": "example-boot", "epoch": 7 },
  "scope": { "kind": "city" },
  "queries": [
    { "key": "city.population", "parameters": {} },
    { "key": "city.money", "parameters": {} },
    { "key": "budget.balance", "parameters": {} }
  ]
}
```

`GET /v2/metrics?query=<percent-encoded JSON>` carries that same request and
returns `Observation<Record<string,MetricValue>>`. Use the shared query encoder;
do not concatenate JSON into a URL. Require1–64 distinct known keys (the current
catalog contains26), exact fields, and empty parameter maps for these definitions.
Unknown keys, parameter semantics, areas, buildings, pagination and mixed scopes
reject before native dispatch. Selected-line metrics use
`scope={kind:"selectedLine",line:EntityRef}` with full observed world/index/version;
they accept only transit keys. City scope accepts only city/labor/budget keys.
The additive `diagnosticMetrics.v1` extension includes ten
`labor.jobs.totalByEducation.<tier>` / `labor.jobs.freeByEducation.<tier>` keys,
where tier is `uneducated`, `poorlyEducated`, `educated`, `wellEducated` or
`highlyEducated`. Each is a cached native `count` in `workplaceSlots`, with no
rate period. These are total/free job slots by the native education bucket;
vacancies do not measure unemployed workers, worker qualifications or a proven
labor bottleneck. Native totals remain separate; bucket sums are not forced to
match them, and no occupancy/shortage ratio is inferred. Missing counter objects
and negative counts yield null/unavailable; a present zero remains supported.

The updated client accepts valid older16-key catalogs and unchanged old-key
batches. A collection requesting a new key absent from its actual authoritative
catalog refuses before polling. Older clients that cap catalogs at16 are not
promised compatible with the expanded catalog. Selected definitions remain
pinned in each history; the revision and all previous metric meanings stay v1.
Actual MCP discovery exposes the strict typed metric inputs and all known keys;
exactly one of keys/queries remains required by handler validation. SDK input
errors are bounded independently of the existing response/history limits.
GET routes accept no body. The encoded metric query limit is12,000 characters,
within the existing16KiB whole-header limit. Larger future query forms can use
the existing bounded O7 POST; no new read POST or arbitrary URL dispatch exists.

`MetricValue` is `{value,unit,period,source,support,reason}`. Unavailable values are
null with a reason; no default zero, clamping or unit conversion occurs.
Definitions include kind, source, refresh description and parameter definitions.
Their Capability is `unverified`; a value's `support="supported"` only says that
the declared raw field was read. It does not certify native parity.

| Keys | Unit and period | Source and qualification |
| --- | --- | --- |
| `city.population`, `city.populationWithMoveIn` | `citizens`; no period | Native Population counters; the latter explicitly includes move-in. Absent Population makes these unavailable while preserving independent money/XP. Present zero population remains supported zero. |
| `city.money` | `nativeMoney`; no period | Signed raw CitySystem treasury; no currency conversion. |
| `city.xp` | `XP`; no period | Raw CitySystem XP. |
| `labor.employed` | `workers`; no period | Cached CityWorkerCount, not a resident employment ratio. |
| `labor.homelessCitizens` | `citizens`; no period | Cached HomelessCitizenCount. |
| `labor.jobs.total`, `labor.jobs.free` | `workplaceSlots`; no period | Cached native total/free workplace counters. |
| `budget.income`, `budget.expenses`, `budget.taxIncome`, `budget.balance` | `nativeMoney`; `month` | Native monthly rates refreshed hourly. Expenses retain signed negative costs; native balance is preserved rather than subtracting signed expenses. |
| `transit.waypointCount`, `transit.segmentCount` | `waypoints` / `segments`; no period | Complete traversal of the bounded selected-line buffers. Incomplete/oversized observations remain unavailable. |
| `transit.brokenWaypointCount`, `transit.invalidSegmentCount` | `waypoints` / `segments`; no period | Exact counts of T1 per-slot `knownFault` assessments; any `unassessed` slot makes the exact total unavailable. Nullable health-reference arrays are not counted. Optional positions/waiting metadata do not create broken references. |

Nonfinite values and invalid count values become unavailable. Missing
PathInformation/distance is unassessed, not zero invalid segments; sanitized
nonfinite distance remains a known fault. Native validity remains unknown.
There is no route-length, load/capacity/headway, utility-health, happiness-percent
or arbitrary-statistics metric in this catalog. T1's `rawLength` has unknown unit
and cannot borrow the legacy UI helper's metre meaning.

O7 adds the fixed item `{id,name:"metrics.read",args:<normalized request>}`.
It uses the same producer but returns `Observation<MetricBatch>` rather than a
dictionary. `MetricBatch` contains schemaVersion1, definitionRevision
`diagnosticMetrics.v1`, `values`, explicit scope, exact `members`, `perMember`
values, per-source observation metadata, nullable clock and member evidence.
Output city scope is explicitly `{kind:"city",line:null}` with empty member
arrays; input city scope is strictly `{kind:"city"}`. Empty city membership is
an explicit aggregate scope, never a wildcard route cohort.

Each required typed source is read once per request; clock comes from that
overview read or one state read. Source metadata retains original world, frames,
UTC, warming/availability, completeness and warnings; refreshTimeUtc is null.
Selected transit retains its original observation. Source failures disable only
affected metrics; world/readiness changes prevent publication. Multi-source reads
warn `multiSourceReadNotAtomic`; differing frames warn `multiFrameReadNotAtomic`.
Equal frames do not establish an atomic native snapshot.

Clock metadata retains raw normalized TimeSystem date/time/year/daysPerYear,
`sourceObservationId="city.overview"|"state"`, that source's frame bounds and
`frameIndex=endFrame`, explicitly qualified `frameEvidence="sourceObservationEnd"`.
It does not prove an exact clock-getter frame, verified hours or elapsed native
time. Invalid/unavailable clock evidence is null. Calendar conversion and native
budget parity remain unverified; cached-source refresh time remains unknown.

Selected-line member fingerprints cover only observed ordered waypoint/connection
refs and XYZ, segment refs/raw distances, mode/purpose, world and completeness.
They are `structuralSampleOnly`, unavailable when required evidence is missing,
and cannot prove complete geometry/topology, reconcile rebuilt identities or
authorize editing. No metric has an automatic improvement direction.

Direct results and catalog use the existing256KiB capped UTF-8 serializer; O7
retains its256KiB item,4MiB group and1MiB body limits. The client caps streamed
metric responses for all HTTP statuses. New endpoint capabilities are degraded
`fixedTypedMetricsOnlyNativeNotRun`; native support is unverified. No history,
comparison, persistence, writer/lease authority or gameplay controls are added.


## Passive construction lifecycle history

`GET /diagnostics/construction-lifecycle` and O7 `construction.lifecycleTrace`
consume the same bounded managed journal. The GET is available while loading;
O7 keeps ready-world admission. See [the lifecycle contract and coverage map](native/lifecycle.md#passive-source-instrumentation).
There is no additional MCP tool and no native completion, delivery, cleanup,
authority, cancellation or durability receipt.

Optional query fields are `bootId`, `afterSequence`, `limit`. The default returns
the latest16 events ascending; limit is1–32. `afterSequence` is exclusive,
0–9007199254740991, and requires this process's bootId. Wrong boot returns409;
a future cursor returns400. Unknown/duplicate decoded keys, bodies and wrong
methods reject. To continue, pass `bootId` plus returned `nextSequence` as
`afterSequence`. `hasMore`, `tailOmitted` and the precise `evictedFrom` /
`evictedThrough` prefix distinguish forward continuation, default-tail omissions
and eviction. Each page is an independent snapshot; loss counters do not invent
sequence intervals for failed observations. `retainedBytes` reports the sum of encoded event bytes; admission also reserves journal-array commas/delimiters.

Storage retains at most256 events,8KiB per event and2MiB serialized UTF8 (including journal-array framing), with
safe-integer sequence/counter saturation and no rollover. Pages and trace MCP
item/error wrappers are capped at256KiB; successful ordinary O7 groups retain
their4MiB transport cap. These caps describe serialized data, not exact managed
heap. No pagination, retry or polling is automatic. History survives world
changes until eviction. `readWorld/readReady/readAtUtc` qualify the read;
each event separately retains original `ingressWorld`, observed world/readiness,
nullable frame/reason, UTC, finite route/adapter/kind/phase, optional status and
ownership, and up to4 held raw versioned references with original worlds and
omission counts. No ECS lookup is performed to enrich trace references.

IDs are `legacy:<GuidN>` diagnostic identities, distinct from admitted operation
UUIDs. `replyAttempt/completionAccepted` describes the managed completion-source
attempt only; transport delivery remains unknown. `deactivateObserved` is a
method-entry observation, and `restorationAttempt` occurs at the existing native
assignment branch. `stopObserved` does not establish player takeover. Every
event retains `effects:unknown`, `cleanup:unverified`, `authority:unverified`.
O7's outer current frame/world span describes sampling this managed history,
not the age or native correctness of its events; warnings are
`managedHistoryNotNativeObservation` and `nativeLifecycleNotRun`.
