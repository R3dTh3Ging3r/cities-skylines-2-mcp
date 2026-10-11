# Bridge API: release 1 capability contract

This release adds `GET /capabilities` and the MCP tool `cs2_capabilities`. It reports
`protocolVersion: 1` and `schemaVersion: 1`. Existing route and success field names
remain available; unavailable ping evidence becomes nullable as documented below.
Protocol 2, operation receipts, admission guards and new method
enforcement are future work; this manifest does not advertise them.

## Discovery and exposure

`/capabilities` returns `identity`, `runtime`, `endpoints`, `capabilities`,
`nativeSupport` and `degradedReasons`. `EndpointCatalog.All` in
[EndpointCatalog.cs](../CS2MCP.Bridge/EndpointCatalog.cs) is the authoritative
classification of the 66 HTTP routes. There are 66 MCP tools, including the new
discovery tool. Each endpoint names its corresponding MCP capability.

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
`completionMechanism`. `legacyHttpVerbs: ["GET"]` documents observed legacy MCP
usage, including writer routes. Other accidental method acceptance is not a
supported contract. This release adds no method enforcement.

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
| `sync-set` | Setter/buffer write returned; some routes read back, others echo input. Native downstream effects may follow. |
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
