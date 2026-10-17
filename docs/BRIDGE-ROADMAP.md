# CS2 bridge improvement roadmap

October 17: [the PR4–8 reconciliation](handoff/ROADMAP-RECONCILIATION-2026-10-16.md)
records the current accepted source stack and remaining dependencies. It includes
bounded geometry/relationships, city/transit observations, typed metrics and
collected comparisons; operation completion, all-writer control and native
G1–G4 gates remain open. Computed grade/curvature inspection of existing network curves through C1/O7
is now independently accepted as a bounded read-only source slice; see its
[acceptance and limitations](handoff/NETWORK-GEOMETRY-2026-10-17.md).
The earlier curve-readback native smoke does not validate the newer source stack.
The current source also passively observes the existing save call's Task<bool>
through bounded lifecycle history. It preserves immediate legacy save behavior;
task outcome is not native saveSuccess, durable checkpoint or full O4 completion.
See [the passive task contract](bridge-api.md#passive-save-task-outcomes).

October 13 execution status: see [the authoritative handoff](HANDOFF.md). F1–F6 are accepted offline; O1 awaits independent review; other tasks remain pending and native gates remain notRun. The dated proposal below is retained as planning history.

October 11, 2026 · Proposed priorities · Planning only

The [implementation package](superpowers/plans/2026-10-11-bridge-implementation.md) now translates this roadmap into five focused plans, shared contracts, native investigation gates and release checks. It is a proposed execution plan, not evidence of implemented features.

## Outcome

Make the bridge fast enough for substantial city work, precise enough for deliberate layouts, and dependable enough to recover when the player takes over or a request times out. The biggest practical wins are **reliable action chaining, exact zoning, bicycle/bus road upgrades, cargo routes, and previews that show the entire layout before construction**.

This consolidates every numbered finding B001–B019 and the unnumbered additions in [BRIDGE-NOTES](gameplay/BRIDGE-NOTES.md), lessons L001–L054, the archived gameplay sessions, the interrupted cargo session, and a read-only source audit. Newly recorded gaps are B020–B029. Historical city measurements are evidence of those sessions, not current city state. No game calls, deployment, feature implementation, or upstream contact were made for this roadmap.

This is a delivery roadmap, not an approved implementation specification. The [original adaptation design](superpowers/specs/2026-10-08-cs2-bridge-design.md) covered setup and a supervised construction test; the larger features below need their own focused designs and implementation plans. Keep the existing C# mod → loopback HTTP → Node MCP architecture and normal game validation. Preserve upstream history and attribution in the personal fork.

## Recommendation and alternatives

**Recommended: repair the shared controls, then deliver the highest-use features in small releases.** This addresses the workarounds we repeatedly needed without making every feature wait for a complete redesign.

Feature-first development could deliver cargo or bike lanes sooner, but would leave retries, stale entities and ambiguous completion underneath those new actions. A broad rewrite could unify everything, but would discard working native-tool integration and delay practical improvements. Neither is the recommended starting point.

“Faster” means fewer round trips and unnecessary observations, bounded main-thread work, and fewer repairs—not concurrent city mutations. Batch predetermined actions sequentially and inspect meaningful boundaries. Geometry, occupancy, ownership, costs and the player's intervening edits remain reasons to revalidate.

## What already works

| Existing foundation | Extend it with |
| --- | --- |
| Connected roads/tracks, segment splitting, quadratic curves, free-end elevation | Full curve readback, explicit 3D snap targets, better alignment controls and native previews |
| Whole-road replacement, including a bounded multi-segment input | Native bike/bus overlays, additive/remove semantics and lane continuity checks |
| Eight cosmetic/side upgrades; lights, no lights, all-way stop and default controls | Preservation tests, roundabout/preset tools and turn/lane diagnostics |
| Radius zoning and aggregate occupied/empty counts | Exact native cells, frontage ownership, protected selections and bulk apply |
| Prefab dimensions, building inventory, roadside/shoreline alignment dry runs | Actual native validation, upgrades, exterior gates, underground and approach envelopes |
| Passenger route create/list/delete; fares, fleet policies and vehicle models | Cargo/air creation, existing-route editing, stop lifecycle and typed freight metrics |
| Cargo/air stop discovery and readback of native cargo routes | Creation and operation diagnostics; discovery is not creation support |
| Budget, taxes, fees, policies, loans, labor, statistics and basic utility aggregates | Consistent units, harmless loan quotes, detailed services and resource flows |
| District polygons, tile purchases, terrain/pollution/groundwater grids | Owner-bound landfill/extractor editing and better spatial queries |
| Timed simulation with frame-based auto-pause and cancellation; asynchronous saving | Verified completion, wall-clock deadlines, world fencing and takeover controls |

The current session exposes **10 direct CS2 tools**, while the historical full MCP catalog has **65**. A feature can exist in source/server registration but be absent from the client's allowed tools. Audit exposure before building a duplicate or changing configuration. This roadmap does not expand the allowlist. A server cannot independently know what the client chose to expose; diagnostics need both sides.

## Delivery sequence

Priority reflects impact; wave reflects dependencies. Work within a wave can proceed independently where the files and native tool state allow it. Effort is relative scope, not an hour estimate: S = focused contract fix; M = bounded feature; L = native pipeline/integration work.

| Wave | Deliverable | Priority / scope | Exit condition |
| --- | --- | --- | --- |
| 0 — Tell the truth | R01 contracts, capability and compatibility baseline | P0 / S–M | Strings round-trip; units/errors are accurate; installed capabilities are distinguishable from exposed tools |
| 1 — Make chaining dependable | R02 operation/save lifecycle; R03 guarded batches and player handoff | P0 / L | An interrupted or timed-out batch can be reconciled without repeating committed work |
| 2 — Remove repetitive manual work | R04 exact zoning; R05A bike/bus upgrades; R06A cargo rail/air routes | P1 / M–L | Precise blocks, verified upgraded corridors, and freight moving on normally validated routes |
| 3 — Plan complete layouts | R05B road/rail geometry; R07 facilities/previews; R09 progression; R10 areas/objects; R06B route editing | P1–P2 / M–L | Reviewable complete plans, correct connectors, normal upgrades/unlocks, editable existing assets |
| 4 — Diagnose growth | R08 service/economy detail; R11 traffic/resources/utility trends | P2 / M–L | Decisions use measured capacity, qualified costs and comparable operating observations |

R07A (asset/connector inventory and preview primitives) and R08A (minimum service operating readbacks) are early shared deliverables: their read-only pieces can ship from wave 1, and the required native previews must precede dependent mutations. R07B is full layout planning and facility mutation; R08B is the complete service/forecast model. This avoids making road overlays wait for the whole facility suite or creating a dependency cycle. R12 validation and release discipline applies to **every** wave.

## Work packages

### R01 — Contracts, capabilities and compatibility

**Evidence:** B001, B003, B004, B006, B008, B009, B014, B020, B022, B025. Start here; no new construction behavior is needed for most fixes.

- Standardize query encoding across old and new MCP handlers: spaces, literal plus, ampersand, percent and Unicode must reach the same name. Preserve compatibility with the HTTP parser.
- Correct demand's 0–255 scale, budget aggregate/breakdown signs, curve-control wording, cargo load labels and invalid route-length reporting. Resolve the calendar conversion against native UI across month/year boundaries; B004's cause remains unconfirmed.
- Return stable error codes, retryability and an operation reference when available. Preserve HTTP/native details instead of collapsing busy, unsupported, unavailable and rejected into one text error. Timeout means unknown outcome until reconciled.
- Add a capability manifest with protocol/schema version, mod/game build, supported operations, native dependencies and degraded/unsupported reasons. Match this against client tool discovery. A post-load stale unlock should not look like a permanently locked asset.
- Fix default bus-stop discovery to exclude placeholders/integrated-only choices; report actual unmet prerequisites. Investigate service counts that returned zero despite existing buildings before changing their definition.
- Audit slider ranges/steps and read back the effective value after the appropriate native update. Fee and policy setters use different mechanisms; acknowledgement alone is not applied-value verification.

**Done when:** focused regression fixtures round-trip names and units; malformed/range/locked/busy cases have distinct machine-readable results; native budget/date/service checks agree or explicitly report unresolved fields. Existing clients retain documented compatibility for renamed fields.

**Code areas:** [MCP adapter](../mcp-server/src/index.ts), [HTTP parser](../CS2MCP.Bridge/HttpBridgeServer.cs), [state/demand](../CS2MCP.Bridge/RequestHandlers.cs), [economy](../CS2MCP.Bridge/RequestHandlers.Economy.cs), [transit](../CS2MCP.Bridge/RequestHandlers.Transit.cs), [management](../CS2MCP.Bridge/RequestHandlers.Management.cs).

### R02 — Operation tracking, saves and recovery

**Evidence:** B002, B010, B012, B018, B024; asynchronous-save notes. Depends on R01's error contract.

- Establish one lifecycle across native building, road, transit, area and demolition operations: queued, validating, applying, verifying, then a terminal result. Use operation IDs and idempotency keys scoped to the current world. Every mutation, including an individual request outside a batch, must check expected world identity before applying.
- Distinguish timeout from cancellation. Query status after client disconnection; do not retry committed native work. Cancellation can stop queued/future work, but must disclose an already applied change.
- Return immutable before/after receipts with created, changed, reused and removed entities, final snapped geometry, costs and native warnings. An empty construction receipt is ambiguous until verified.
- Track save completion/failure and checkpoint identity using the native save result. Expose save name, world identity, completion time and artifact verification status. Never claim reload-tested recovery merely because writing succeeded.
- Reconcile pending tools and timers when loading another save or destroying a world. Prevent old requests from committing into the new world. Recover a stranded builder only after checking whether native apply is still active.

**Already partly present:** road completion waits until Finish and handles tool interruption; transit has pathfinding timeout/commit checks. Generic construction responds earlier. Reuse those safeguards rather than replacing them with arbitrary sleeps.

**Done when:** timeout → status resolves to exactly one outcome; repeated keys cannot spend twice; tool takeover/load/failure-after-apply leave a usable builder and a truthful receipt; a save failure never returns completed. Verify in paused and running disposable cities.

**Code areas:** [requests](../CS2MCP.Bridge/BridgeRequest.cs), [queue/timer](../CS2MCP.Bridge/BridgeSystem.cs), [generic tool](../CS2MCP.Bridge/BridgeToolSystem.cs), [road tool](../CS2MCP.Bridge/BridgeRoadToolSystem.cs), [transit tool](../CS2MCP.Bridge/BridgeTransitToolSystem.cs), [save/run handlers](../CS2MCP.Bridge/RequestHandlers.Meta.cs).

### R03 — Guarded batches, consistent observations and player handoff

**Evidence:** unnumbered observation/deadline notes, L033/L041/L043/L054, B023/B024. Depends on R02.

- Accept bounded, predetermined action lists with per-step IDs, expected world/revision, explicit prerequisites and protected regions. Run mutations sequentially; stop at the first failure or changed precondition. Resume from receipts, never by resending the entire list.
- No promise of whole-batch rollback: report partial completion and offer only validated compensating actions or a separately verified checkpoint restore.
- Add batched observations with frame/time/freshness metadata and a bounded workload. If a snapshot spans frames, expose that fact rather than calling it atomic.
- Budget queue work per update so a large read/zoning batch cannot monopolize the game thread. Benchmark limits before publishing them.
- Add player-visible pause/stop/takeover control and a mutation lease covering all writers, including generic builds, zoning, management controls and saves. Taking over invalidates pending agent steps and previews. Already applied work remains visible in its receipt. Manual inputs must not be silently replayed or overridden.
- Keep wall-clock deadlines distinct from frame-based game hours. Report blocking milestone/signature dialogs, interrupted runs and timer ownership; dismiss only the identified permitted dialog. Provide a known neutral selection/view action.

**Done when:** a batch partially succeeds, is interrupted, and resumes without duplicates; a user road edit invalidates affected targets; loading a different save cancels old work; a deadline reports the actual pause result. If the game/UI thread hangs, report the limit of the stop mechanism rather than guaranteeing an instantaneous halt.

### R04 — Exact cells and frontage-aware zoning

**Evidence:** B015, L041, L054. Depends on R02/R03 and scoped spatial readback.

- Read native block/cell identity, center/corners, zone, occupied/blocked state, owning frontage/road and side. Empty and unzoned are different properties.
- Select exact cells, road-aligned rectangles, polygons or frontages; preview the chosen cells. Exclude avenues by actual frontage ownership, not only a distance buffer.
- Support explicit only-empty, only-unzoned, preserve-existing-zone/building, protected-parcel and maximum-change rules. Revalidate versioned blocks after road reconstruction.
- Apply a bounded cell set in one operation; return every changed/skipped cell and reason. Conditional undo restores zone values only while the world/geometry still match; it cannot resurrect demolished buildings after growth.

**Done when:** one-cell selection changes exactly one cell; angled blocks fill without circular spill; mixed vacant/occupied lots and adjacent avenues remain protected; replay is a no-op; stale selections fail without repainting new blocks. Compare a fixed selection with the current step workaround for calls, wall time, final diff and errors—not just raw speed.

**Code areas:** [zoning](../CS2MCP.Bridge/RequestHandlers.Zoning.cs), [cell summaries](../CS2MCP.Bridge/RequestHandlers.Perception.cs). The current radius-8 brush is not an exact-cell tool and has no explicit occupied-cell guard.

### R05 — Road upgrades, road/rail geometry and whole junctions

**Evidence:** B009/B010/B012/B013/B019/B026; L019/L028/L048/L049/L052.

**R05A, first:** native bicycle/public-transport overlay add/remove by side, composition compatibility and cost/parking preview, preservation of unrelated upgrades, and corridor batching. Read actual lane permissions/directions, stop compatibility and junction continuity. Whole-road replacement already exists and is a separate operation. The possible existing-upgrade overwrite is an audit risk to test, not observed data loss.

**R05B, next:** full Bezier readback and explicit tangent/cubic controls; consistent parallel road/track offsets; radius, slope, clearances, joining angles and portal/support diagnostics. Add explicit endpoint/split selection with height-aware snapping. A surface road at the same XZ must not silently capture an intended tunnel connection. Keep existing quadratic semantics compatible and report supported elevation limits rather than unexplained clamping.

Add native roundabout and intersection-preset discovery with rotated whole-layout preview, plus normal apply, roundabout resize/style change and removal. Include footprint, cost, all ramps/supports, affected lots, resulting signals, lane movements and merge spacing. Check directed depot/platform/return movements, not only undirected graph connectivity. Geometry diagnostics help design; they do not certify beauty or future traffic capacity.

**Done when:** an upgraded corridor preserves existing decoration/opposite-side lanes and valid stops; a stacked-network fixture joins the selected level; a rail ladder supports both terminal platforms and return travel; a preset preview matches its complete built footprint and collateral changes; roundabout resize/style/removal preserves intended approaches and reports changed controls. Use actual vehicles to validate movement after construction.

**Dependencies/code:** R05A uses R02/R03 plus the early R07A native overlay validation/readback primitives; R05B adds whole-layout preview and full spatial survey. Neither waits for R07B facility mutations. [Road handlers](../CS2MCP.Bridge/RequestHandlers.Roads.cs), [upgrades](../CS2MCP.Bridge/RequestHandlers.Build.cs), [junctions](../CS2MCP.Bridge/RequestHandlers.Junctions.cs), [network graph](../CS2MCP.Bridge/RequestHandlers.Vehicles.cs), native road/definition systems.

### R06 — Cargo routes and existing transit editing

**Evidence:** B008/B014/B020/B021/B022; latest interrupted cargo session.

**R06A:** explicitly select transport mode and passenger/cargo purpose, exact compatible platform/gate and outside connection. Extend the existing native route/pathfinding pipeline for cargo rail, cargo air and passenger air, validating each mode independently. Include the return leg and mode-specific dispatch prerequisites; distinguish outside-supplied freight vehicles from local-depot requirements. Check airport taxiway/runway/gate readiness. Prevent duplicate creation after timeout. Shoreline placement does not supply ship routing; ship/harbor lines are a later extension only if wanted.

**R06B:** edit an existing route's stop order, insert/remove stops and metadata while retaining its identity, fare, schedule and compatible fleet. Provide a nonmutating route trace before replacing its path. Include explicit platform/depth selection, route health, stale stops and potentially redundant cyclic routes. Do not automatically delete a route merely because two stop lists look reversed.

Fares, fleet policies and passenger vehicle-model selection already work. Extend their readback/validation as needed; don't rebuild them. Cargo metrics need separate load/capacity fields and verified mass units; current passengersOnBoard is misleading for freight. An active line with zero vehicles is not proof of service operation.

**Done when:** native validation accepts cargo rail/air lines using normal unlocks; trains/aircraft dispatch, visit the intended terminals and move freight during an observed run. A separate passenger-air fixture validates compatible passenger gates and passenger movement. Invalid gates/unreachable legs fail without a broken line. Editing/renaming retains line identity and settings. Passenger and cargo load labels agree with their native panels.

**Dependencies/code:** R01/R02; operation diagnostics can be added incrementally. [Transit handlers](../CS2MCP.Bridge/RequestHandlers.Transit.cs), [route tool](../CS2MCP.Bridge/BridgeTransitToolSystem.cs), [rename support](../CS2MCP.Bridge/BridgeTransitRenameSystem.cs), [line policies](../CS2MCP.Bridge/RequestHandlers.LinePolicies.cs), [models](../CS2MCP.Bridge/RequestHandlers.LineVehicles.cs).

### R07 — Facility families, native previews and persistent reservations

**Evidence:** B010/B013/B017/B027; campus, rail and service placement lessons.

- Query base/inline/detached upgrades, compatibility, installed state, slots/counts, capacity effects, costs and prerequisites. Distinguish exterior gates from internal lanes; include underground platforms, upgrade envelopes and airport approach restrictions.
- Add true noncommitting native preview: final oriented footprint, supports, terrain/ownership conflicts, existing assets/zone loss, native errors and cost. Existing roadside/shoreline dryRun is only computed alignment. Label computed, native-validated and committed outcomes separately.
- Expose owner-bound native upgrade, removal, activation and relocation/rotation, retaining compatible upgrades and storage. Relocation must not conceal demolish-and-rebuy behavior.
- Keep world-scoped protected polygons for campus grounds, future civic buildings, utilities, ramps, rail approaches and expansion. Return selected/excluded regions in a reviewable map/overlay with IDs linked to the plan. Invalidate previews after relevant edits.
- Spatial queries must include intersecting curves/footprints even when centers lie outside the query, and disclose truncation/resolution. A denser sample of the same coarse terrain grid is not more detailed terrain.

**Done when:** previews leave treasury, committed entities and zones unchanged; temporary native preview entities are cleaned up on cancel/failure. Unchanged valid previews match build results; intervening edits trigger revalidation; a fully upgraded campus fits its stated envelope; a moved cargo terminal preserves supported upgrades; a small subway entrance reveals its long underground platforms. A user can review the complete arrangement before construction.

**Dependencies/code:** R07A read-only inventory/spatial queries need R01 contracts; its native preview needs R02. R07B mutations additionally require R08A operating readbacks, which build on R07A inventory and do not depend on R07B. [Placement](../CS2MCP.Bridge/RequestHandlers.Placement.cs), [perception](../CS2MCP.Bridge/RequestHandlers.Perception.cs), [native definitions](../CS2MCP.Bridge/CreateDefinitions.cs). Existing internal relocation/upgrade plumbing is reusable, not proof of a public working API.

### R08 — Service capacity, staffing and affordable operation

**Evidence:** B006/B010/B011/B025 and diagnostic follow-ups; L016/L023/L050/L053.

Provide per-building and city summaries for education enrollment/capacity/eligible population; medical patients/beds/ambulances; fire/police incidents, risk, vehicles and jail/prison occupancy/capacity; deaths versus processing, cemetery storage and hearses; garbage stored/processing/disposal and trucks; parks/leisure and maintenance workload. Distinguish deployed, available and in-maintenance vehicles; expose road connection, inactive/disconnected reasons and operational warnings. Read current/required staff and efficiency factors rather than treating an employee buffer as staffed capacity.

Separate purchase cost, base maintenance, wages, actual staffed upkeep and estimated future costs. Rates need periods; storage is a stock. Add a read-only loan quote that cannot change principal or treasury. Show assumptions in forecasts and keep speculative recycling/export revenue separate.

**Done when:** paused native panels match ordinary, upgraded, inactive and understaffed examples; parent/extension counts are not doubled; observed costs reconcile with budgets; loan quote leaves debt/cash unchanged. Service accessibility must use supported travel/coverage data or be explicitly labeled an estimate.

**Dependencies/code:** R01/R02, asset inventory; [services](../CS2MCP.Bridge/RequestHandlers.CityData.cs), [economy](../CS2MCP.Bridge/RequestHandlers.Economy.cs), [inspection](../CS2MCP.Bridge/RequestHandlers.Perception.cs), [loans/fees](../CS2MCP.Bridge/RequestHandlers.Management.cs).

### R09 — Progression and legitimate unlocks

**Evidence:** development-point notes, B005/B008/B017.

Expose current/next milestone, XP, available development points, stable tree nodes, costs, prerequisites and purchased/available state. Include signature-building unlock requirements and post-load freshness. Add relevant read-only gameplay settings and visual time/day-night state so planning can distinguish simulation conditions from presentation. This does not add settings writes. Add normal point spending with verified remaining points and unlocked assets; never substitute force placement for a purchase.

**Done when:** insufficient points/unmet prerequisites reject without cost; successful and retried purchases debit once; ordinary depot/stop prerequisites explain why a line is locked; milestone/signature dialogs report why a timed run stopped. Depends on R01/R02 and R03 modal handling.

### R10 — Roadside objects and owner-bound areas

**Evidence:** B010/B014/B016/B021/B028.

Add supported net-object discovery, placement, inventory, relocation and removal for bus/tram poles and mailboxes. Report attached road, owner, current fill/capacity and collection where available. Protect outside connections and station-owned objects from generic cleanup.

Separately, query/edit an existing landfill or supported extractor polygon through its native owner-aware tool. Preview legal vertices, affected neighbors and capacity/production effects; preserve stored material, ownership and upgrades. Existing district creation is not a landfill editor. General decorative props and terraforming are later scope, not prerequisites.

**Done when:** a mailbox and bus stop can be moved/removed without orphaning unrelated routes; rebuilt roads produce fresh attachments; an approved landfill strip extension changes verified capacity without changing neighbors; invalid/self-intersecting/unowned polygons leave the original intact. Depends on R02/R07.

### R11 — Comparable traffic, utility and production diagnostics

**Evidence:** B007/B017/B028; utility, happiness and notification notes.

- Traffic: fixed road cohorts, used-versus-unused segments, node/lane bottlenecks, queue heads and matched-period comparisons; retain geographic reconciliation when entity IDs change. Distinguish peak improvements from night traffic disappearing.
- Transit: collected headway, wait/load, travel-time and dispatch history; label newly collected history rather than implying the game has a native per-line history API.
- Utilities: per-network connection/fulfilled flow, endpoint warnings and distribution bottlenecks; treatment utilization/quality; power and battery trends with daylight/weather context. Aggregate supply alone is insufficient.
- Resources: bounded native ore/oil/fertility maps with honest resolution, company inputs/outputs/storage/workforce/import-export and terminal inventories/loading. Separate resource stocks, flows and rates.
- City diagnostics: severity-aware notifications, structured happiness factors and typed statistic catalog. Positive events must not be counted as service failures.

**Done when:** disconnected service is identified despite ample aggregate capacity; battery trend distinguishes temporary support from sustainable production; fixed-cohort traffic excludes false gains from new empty roads; freight/resource totals reconcile with native panels and known sampling periods. Depends on R01, spatial identity and family-specific readbacks.

### R12 — Validation, release and contribution discipline

**Evidence:** B029; original adaptation constraints. Cross-cutting, not a final cleanup phase.

- Add focused offline contract tests using a fake bridge and deterministic fixtures for serializers, schemas, units, errors, operation transitions and plan selectors. The Node package currently has build/start scripts but no test script; compilation is not behavioral coverage.
- Use a disposable city for native integration fixtures: occupied/vacant angled zones; decorated roads; stacked tracks; upgraded facility; outside cargo connections; blocked modal; interrupted build and save/load transition.
- For construction, require separate evidence of placement, current graph/lanes, appearance and running behavior. Passing one does not imply the others. Keep observed facts distinct from hypotheses and user aesthetic acceptance.
- Compare sequential individual calls with bounded batches on the same restored fixture. Record request count, wall time, game-frame time, errors, collateral changes and exact result diff. Establish a measured baseline before setting a speed target.
- Compile with SkipDeploy=true, deploy only a validated build with the game closed, verify installed build identity and retain rollback. Keep compact artifact paths, proprietary game libraries and raw city data outside commits.
- Document the local trust boundary: loopback binding already exists; a client tool allowlist does not authorize or restrict raw HTTP requests. A separate scoped review may add mutation-method/session-token checks while retaining local-only operation; remote hosting is outside this roadmap.
- Release focused patches on codex/ branches in the personal fork. Good early contribution candidates are encoding, contract corrections, placeholder/prerequisite handling and orphan-route telemetry. Do not bundle personal city history or a broad feature suite into one upstream patch; send upstream issues/PRs only on user instruction.

## Complete traceability

| Original note or addition | Roadmap coverage |
| --- | --- |
| B001 query spaces | R01 |
| B002 premature readiness | R02, R03 |
| B003 signed budget; B004 calendar | R01 |
| B005 blocking milestones/signatures | R03, R09 |
| B006 service counts | R01 investigation, R08 |
| B007 changing traffic baseline | R11 |
| B008 placeholder stops/prerequisites | R01, R09 |
| B009 Bezier wording/control | R01, R05B |
| B010 preflight, upgrades, activation, landfill, paths | R02, R05B, R07, R08, R10 |
| B011 loan quote and staffed costs | R08 |
| B012 reconstructed geometry/receipts | R02, R05 |
| B013 full interchange/preset previews | R05B, R07 |
| B014 orphan routes/negative length | R01, R06B, R10 |
| B015 exact-cell zoning | R04 |
| B016 mailboxes | R10 |
| B017 upgrades/connectors/resources/terrain | R07, R09, R11 |
| B018 persistent busy after timeout | R02 |
| B019 bike/bus road overlays | R05A |
| Development points, signature prerequisites and read-only settings | R09 |
| Save completion/idempotency | R02 |
| Batched observations, typed units, freshness | R01, R03 |
| Rectangles/preserve zones | R04 |
| Neutral view/selection, safe modal dismissal, wall-clock stop | R03 |
| Utility fulfillment/endpoint diagnostics | R11 |
| Notifications/happiness/crime risk semantics | R01, R11 |
| Roundabout style/size/removal | R05B |
| Service efficiency versus staffing; dispatch/maintenance coverage | R08 |
| B020 cargo and air creation/metrics | R01, R06A |
| B021 route editing/stop lifecycle | R06B, R10 |
| B022 capability/schema/field mismatch | R01 |
| B023 bounded chaining | R03, R04 |
| B024 takeover/world identity/timer ownership | R02, R03 |
| B025 service staffing semantics | R08 |
| B026 full geometry/3D snapping | R05B, R07 |
| B027 reviewable plans/protected reservations | R04, R07 |
| B028 production/resource/area data | R10, R11 |
| B029 validation and stale release documentation | R12 |

## First release and unresolved design questions

The first release should contain **consistent encoding, truthful units/errors, capability reporting, and focused regression tests**. Follow it with operation/save tracking and guarded sequential batches. Then exact zoning, bike/bus overlays and cargo creation become useful independently reviewable feature releases.

Before implementing their respective packages, resolve: which native preview systems can run without committing; which native save completion signal is reliable; what world/revision token survives normal reconstruction while rejecting a reload; which pending operations can actually be cancelled; exact cargo mass units; and native road-overlay preservation rules. These are bounded technical investigations, not reasons to delay the roadmap or present unverified fixes as complete.

No calendar schedule is assigned yet. Native API compatibility and integration tests dominate the larger items; estimates should follow the first focused design/probe for each package.
