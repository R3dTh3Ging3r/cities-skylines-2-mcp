# Bridge findings — October 8–11, 2026

Delivery priorities, dependencies and acceptance criteria are consolidated in the [bridge roadmap](../BRIDGE-ROADMAP.md). Entries below preserve historical observations; a proposed remedy is not an implemented fix. The October 11 review added B020–B029 after checking source and earlier gameplay evidence without touching the running game.

## B001 — Spaces in zoning and service names

Observed `cs2_zone_area` with `NA Residential Low` rejected as unknown `NA+Residential+Low`. The MCP server's URLSearchParams form encoding uses plus for spaces; the bridge path does not decode plus as a space. Calling the same documented route with percent-20 succeeded and painted 1,210 cells. Other older name-based routes may share this issue. Newer road routes already have a bridgeQueryString helper. Candidate patch: use one query serializer consistently, with regression tests for spaces, ampersands and literal plus. Temporary gameplay workaround: correctly percent-encoded localhost requests through ignored helper script.

B001 October9 infill follow-up: NA Residential Medium Row again arrived as NA+Residential+Medium+Row and was rejected before painting. The same documented endpoint with percent-20 succeeded; zoning readback confirmed55 row and62 low-density cells across three requests. This is repeated evidence that the serializer bug remains unfixed. Raw .local/r3-zone-row.json and r3-zone-*-direct.json.

## B002 — Construction acknowledgment precedes readiness

Successful WindTurbine01 placement followed immediately by WaterTower01 was rejected as another build operation in progress. Building confirmed once; delayed tower retry succeeded. Calls are sequential but game pipeline continues across frames. Candidate addition: operation ID, explicit busy/retryable state, and completion polling. Temporary pacing: 650–900 ms between mutations; inspect after uncertainty.

## B003 — Budget sign description is incomplete

Live budget: totalIncome 1918, totalExpenses -33324, balance -31406. Expense breakdown ServiceUpkeep is positive 33324. Tool note says expenses positive but totalExpenses is signed. Treat balance as authoritative; don't subtract a negative aggregate. Candidate patch: clarify aggregate vs breakdown signs or normalize contract with tests.

## B004 — Calendar output disagrees with UI

At the start, UI showed June 2026, API gameDateTime said 2026-01-06. Likely month/day construction or formatting issue; cause unconfirmed. Candidate patch: use game calendar conversion consistently. Until fixed use frameIndex and verified elapsed simulation hours, plus screenshot date.

## Useful additions

- Dedicated progression endpoint: current milestone, next threshold, XP, unlock points and available development choices.
- Development-point spending: the user noticed unspent points after the first challenge. The current MCP catalog and server source expose no tool to purchase development-tree unlocks. Add a progression/tree query with point balance, stable node IDs, costs, prerequisites and purchased/available states, plus a purchase action that uses the game's normal rules and returns the verified unlock and remaining points. Reject insufficient points or unmet prerequisites; retrying an already purchased node must not spend twice. Existing placement `force` options bypass locks and are not a substitute for legitimate point spending. Before choosing an unlock, inspect the current tree and compare its benefit and any subsequent building upkeep with the city's needs. This is a feature request, not implemented functionality.
- Completed-save status including safe checkpoint identity (current tool only acknowledges asynchronous start).
- Utility network connectivity and fulfilled water/sewage amounts, not only aggregate capacities.
- Batched observations with one frame/timestamp and typed units.
- Build completion receipts and idempotency keys, particularly for timeout recovery.
- Rectangular/block zoning and preserve-existing-zones mode for more precise urban design.
- Tool to dismiss construction panels and restore normal view for screenshots.

No upstream messages sent; preserve concrete evidence for a later contribution.

## B005 — Milestone dialogs hold simulation paused

Tiny Village popup visibly appeared at XP760. A new timed-run command reported running, but state re-paused after8frames at13:33. Screenshot confirmed blocking congratulations modal. User dismissed it. Candidate additions: blocking-dialog state, safe dismiss-current-milestone action, progression endpoint, timed-run interrupted reason. Do not repeatedly send resume while a blocking popup remains.

## B006 — Service building counts are zero despite real buildings

Service-budget response reports buildings:0 for every category even after wind turbine, water tower, outlet, cemetery, clinic, school and landfill are verified. Upkeep is nonzero. Candidate fix: correct service-entity association query; test against actual placed structures.

## Diagnostic additions confirmed useful during play

Prefab info needs capacity, base operating expenses and likely wages. Building inspection needs school enrollment, clinical treatment capacity, cemetery usage, landfill storage/processing and vehicles. Current inspect returned only building flag and28employees for landfill. UI inspection showed73%efficiency,30/30employees,2.67/151t garbage,10.83/100t monthly processing,39825/month cost,6vehicles in maintenance and0/14in use. Stored garbage demonstrates collection occurred; avoid treating decorative garbage geometry as proof.

## Further testing

- The sewage connection has an endpoint warning, although no household sewage warning appeared during the challenge. Inspect network attachment and default pipe elevation before treating it as fully resolved. The unused high-voltage outside connection also remains flagged.
- Native notifications include positive events (Happy Face, Leveling Building, Selected), so they are not exclusively warnings. Add severity/category filters and counts that exclude informational events.
- Add a read-only milestone/settings endpoint and a safe selection-clear command. Placement tools repeatedly reopen their construction panels, and closing one can reveal a previously selected building's panel.
- Distinguish service budget efficiency from actual building efficiency, which also depends on staffing and other conditions.

- Add a real-time stop deadline independent of agent polling. The requested 30-minute stop was detected a few seconds late, and pause/evidence tools finished within the next minute. A bridge-side UTC deadline should pause the game even while the agent is waiting on a tool or writing notes.

## Second-session follow-ups

Signature-building unlock dialogs also pause simulation (Rock Musician Mansion and Baltar Pines observed); extend B005 modal detection/dismissal beyond milestones. Add structured happiness-factor telemetry: the UI identified crime risk -13 while bridge notifications had no crime warning and CrimeCount was zero. CrimeRate corresponds to risk/probability rather than a count of completed crimes; expose units and distinguish these measures.

The UI-built small tree roundabout is confirmed by road graph junction164643v1, roundabout:true. Add a bridge action for roundabout size/style and removal, with normal unlock/cost checks; current junction control offers only lights/nolights/stop/default. Surface default pipe placement is visible at the sewage corridor. The outlet itself showed16% utilization, so the existing warning should not be presented as proof that sewage service failed. Separate the network endpoint channels and expose actual treatment/fulfilled flow.

## B007 — New roads change the traffic comparison baseline

Third challenge: cityRoadsAveraged increased from52 to80 after adding the gateway/bypass. The first city flow reading rose71to78%, while the original shopping street fell35to34% and many new segments hadzero recordedvolume andnear100%flow. This is an analytics limitation, not proof of an implementation defect. Track a fixed cohort of existing roads and per-route utilization alongside the aggregate. Add a comparison endpoint that preserves road identities/positions, reports sample counts and excludes unused roads when requested. Nighttime queue reductions also need a comparable daytime sample.

## B008 — Default bus stop can be a placeholder; lock error hides prerequisites

Third challenge, Grand Village/Tiny Town transition. Before the bus depot was ready, cs2_list_transit_stops reported ordinary NA/EU stops locked but `BusStop02 Placeholder` unlocked. Default cs2_place_transit_stop selected the placeholder and placed seven stops. After the depot was operational, normal line creation still returned "bus lines are locked (milestone not reached)". UI showed Grand Village and Bus Depots 1/1 satisfied, but Bus Stops 0/1. This identifies an unmet stop-count prerequisite, not a missing milestone.

After the depot became ready, explicitly placing the normally unlocked `NA_BusStop01` succeeded. Normal line creation then succeeded without force; the original placeholder stops were usable by the route, with passengers subsequently waiting and boarding. Eight-stop Ezra Town Loop dispatched six buses and had 90 passengers aboard in an early observation. Do not infer placeholder prefabs are suitable defaults merely because they are unlocked.

Candidate fixes: exclude placeholder/integrated-only assets from default roadside-stop selection; return actual unmet unlock conditions; refresh available prefabs after prerequisite construction. A default stop name appeared as `Assets.NAME[BusStop02 Placeholder]`, so also filter assets lacking player-facing names. Preserve ordinary game unlock rules; do not recommend force as the routine recovery.

## B009 — Curve description suggests a pass-through point

Research before challenge four: `cs2_connect_road` describes a curve "through cx/cz", but [RequestHandlers.Roads.cs](../../CS2MCP.Bridge/RequestHandlers.Roads.cs) constructs a quadratic Bezier converted to cubic form. The supplied point controls tangent directions and is generally not on the curve. Source inspection confirms the mismatch; no code fix was made. Clarify the schema and comment, give a tangent-aligned example, and consider separate cubic controls or explicit start/end headings for highway geometry. Any extension must preserve compatibility and ordinary game validation.

Useful companion additions: read-only curve controls, tangent discontinuities, minimum horizontal radius, slope and lane-connection inspection; a road-alignment preview with footprint and conflicts before committing construction. These would help catch sharp ramps before a graph-connected but poor-looking design is accepted. The user reported near-stop braking at the industrial ramp; this is not yet a measured causal test of the control-point description.

## B010 — Construction preflight and operating diagnostics still need UI support

Fourth challenge: ramp construction sometimes snapped farther along an existing segment than requested, and overlapping trial pieces created undesirably close junctions. Add a normal-tool preflight that exposes proposed split/snap positions, node degree, nearby existing segments, lane connections, grade and expected removals. A transaction plan showing exact affected entity IDs plus a verified checkpoint would make optional live-road revisions more reviewable. Never interpret rejection or a timeout as permission to perform the same mutation indirectly.

Treatment inspection still required UI for actual utilization, reclaimed-water pollution, staffed wages, attachment placement, and deactivation. Add read-only upgrade attachment envelopes and maximum counts, normal upgrade dry-run/apply, building activation controls, and landfill area/storage/processing inspection. Distinguish an internal pollution-reduction upgrade from one that adds throughput. The existing prefab-name search did not list the landfill's live Waste Recycling Unit, although the in-game upgrade panel offered it.

B001 follow-up: `cs2_set_service_budget` again passed `Garbage+Management` and was rejected. The documented localhost route with `%20` succeeded and readback confirmed150%. No code patch during the timed gameplay run.

Landfill-area editing follow-up: at02:15 January2027 UI storage reached135/151t despite maximum funding. The user explicitly approved extending the narrow disposal strip about75m east at unchanged depth, on surveyed empty land and clear of treatment expansion. Native automated corner drags did not visibly change the shape or151t capacity, even after a short simulation advance. No successful expansion claimed. User offered to perform the two drags manually; leave input control alone until that action is complete, then verify capacity. Add polygon-read/edit with normal placement validation, current/proposed capacity, owner association and checkpoint support. Record this as an observed automation limitation, not a proven game bug.

Manual landfill edit follow-up: the user finished it, and a subsequent native UI read confirmed353t capacity with133t stored. Keep the earlier failed automation observations; do not attribute this successful polygon edit to the agent.

## B011 — Read-only loan quotes and staffed operating estimates

The current loan endpoint reports only existing debt. Source inspection found SetLoan calls RequestLoanOffer and then ChangeLoan in the same request, so it must not be used to obtain a harmless quote. The native Loans UI previews principal, interest and monthly cost separately from Accept. Add a read-only quote endpoint using RequestLoanOffer without ChangeLoan, with explicit time units matching the UI. At zero existing debt, returned zero interest is not an offer of interest-free borrowing.

The recycling purchase card exposed 120,000/month upkeep and 1,500 tonnes/month processing, but its future wage bill and budget-adjusted capacity/cost remain absent from the current prefab endpoint. Add read-only base maintenance, workplace mix, wage assumptions, service-budget effects and estimated versus actual labels. Profitability forecasts should exclude unmeasured recovered-material revenue or show it separately as a scenario.

## B012 — Road reconstruction can omit successful segments from receipts

Fifth challenge: extending the new southbound road by50m returned built:true, segments:[] and null junction data. A fresh road graph verified new segment69054v27 and movement of the previous intermediate node toz1106.68359. Add a post-operation topology diff including reused/changed entities, explicit completion state and final snapped coordinates. Avoid interpreting an empty segment list as cancellation.

The game also automatically added traffic lights at a paired one-way highway to two-way boulevard taper. Road preflight should disclose resulting controls and closely spaced junctions. Full lane-level route tests, curve radius/grade diagnostics and merge spacing would improve planning beyond graph connectivity. No code fix was made during gameplay.

## B013 — Interchange previews need whole-layout and lane information

October 9 research follow-up to the user's repeated geometry feedback. B009/B010/B012 already document curve-control ambiguity, snapping and incomplete construction receipts. A useful next bridge feature is a read-only native intersection catalog plus rotated whole-preset preview: affected roads/buildings, proposed connections and controls, elevation/clearance, total footprint and estimated cost. Include lane-level movement checks and distances to adjacent merges/terminals. These are proposed additions, not capabilities implemented or newly tested today. Their purpose is to prevent another graph-connected but visually poor interchange and to compare land consumption before placement.

## B014 Orphan route telemetry after road clearance

Observed October9: Ezra Town Loop had zero stops/vehicles, eight stopList entries with isStop=false and stop=null, and lengthMeters=-8. Physical stop listing contained only outside connections. Normal line deletion resolved the obsolete markers. Improvement: expose an explicit broken/orphaned route flag and nullable/invalid length instead of a negative physical distance. Preserve outsideConnection classification in any bulk stop-cleanup helper. Evidence: .local/r2-lines.json, .local/r2-stops.json, .local/r2-lines-after.json. No code change in this session.

B012 October9 follow-up: four east-road receipt node IDs(68186,68067,56462,60172) were later absent, while all68 current eastern district road segments were reachable. Preserve current-node geometry in post-build verification rather than interpreting historical IDs as persistent.

B010 October9 follow-up: Pavement Path toward QuayLarge01 at(-690.4,788.4) was rejected InvalidShape despite Road-layer compatibility; nothing changed. Native endpoint/sidewalk diagnostics would help distinguish quay, geometry and elevation restrictions. No cause beyond the validation code was established.

## B015 — Exact-cell zoning and block selection

October 9: the user rejected the rounded patches and gaps left by the broad circular zoning pass, requested fuller street blocks, and explicitly proposed painting one cell at a time through the bridge. This is a requested bridge upgrade, not an implemented capability. The native rectangular-tool follow-up added 299 zoned cells in one western column; earlier fast drags mostly affected the endpoint until the cursor was first positioned at the drag start. That is one input-sequencing observation, not proof that native rectangle painting is generally unreliable. Evidence: `.local/r9-zoning-start.json`, `.local/r9-native-drag-test.json`, `.local/r9-zoning-after-native.json`, and `.local/r9-zoning-draghover.json`.

Source inspection of [the MCP zoning schema](../../mcp-server/src/index.ts) and [ZoneArea](../../CS2MCP.Bridge/RequestHandlers.Zoning.cs) confirms a circular brush with an 8 m minimum radius and 8 m zoning cells. A radius equal to one cell's width can still select several adjacent cell centers; the current action is not exact single-cell painting. [Zoning readback](../../CS2MCP.Bridge/RequestHandlers.Perception.cs) returns area totals by zone, not individual cell coordinates or identities. ZoneArea skips invisible, blocked and overridden cells but does not explicitly test `CellFlags.Occupied`; its response note about cells needing to be unoccupied must not be treated as a preserve-buildings guarantee. No demolition caused by this omission was established in this observation.

Proposed additions:

- Read individual zoning cells with a versioned block identity and cell index, world center and corners, current zone, occupied/blocked/overridden state, and road-access information. Distinguish an empty cell from an unzoned cell.
- Select one exact cell, a road-aligned rectangle, or a polygon. Show the selected cells and exclusions before applying; no circular spill onto the opposite street frontage. Support a bounded batch of explicitly selected cells to avoid one network round trip per cell.
- Offer explicit `onlyEmpty`, `onlyUnzoned`, preserve-existing-zone and preserve-building behavior, with protected polygons for campuses, parks, utilities and future civic plots. Check normal unlock and placement rules. Do not silently replace occupied properties or zone reserved land.
- Revalidate block versions, geometry, occupancy and exclusions immediately before applying. Reject or clearly report stale selections after road reconstruction rather than painting newly regenerated cells by old index.
- Return a change receipt listing each changed cell's before/after zone and each skipped cell's reason, followed by fresh readback. Support restoring the captured previous zones while their geometry is still valid; retain a separately verified named save checkpoint for broader recovery. An undo must not promise to reconstruct buildings lost after simulation advances.

Until implemented, use the existing small brush only as a local approximation after checking neighboring cells, or use native cell/rectangle controls. Verify the resulting block visually and with readback, and preserve the user's explicit pause until they approve resuming. This note records the requested upgrade; no bridge code was changed.

## B016 — Mailbox roadside objects are missing from the bridge

Observed October9: building-prefab search for mail returned only storage extensions; post search returned post office/sorting buildings. Native postal UI provides Mailbox (100 cost,1,000 capacity), and14 were placed normally. Building/tree and bus/tram-specific placement schemas cannot represent these roadside service objects, and current object listing only covers standalone trees/plants. Proposed: add supported net-object prefab discovery, normal native roadside placement with preview/cost/lock checks, an inventory of placed mailboxes with road attachment, and fill/collection telemetry where available. Preserve native validation and report actual entities. This is a capability request, not an implemented endpoint; see L042.

## B017 — Facility planning needs upgrade, connector and approach envelopes

October10 industrial planning observation. Prefab data supplied useful base dimensions and some detached extensions, but did not expose the regular airport cargo-upgrade footprint, full legal upgrade attachment geometry, runway approach zones, track/road gate positions or signature-factory unlock conditions. The global list_roads query can find Train prefabs despite its road-centric name; local road_graph queries alone missed the distant rail system. The terrain grid's56m resolution was too coarse to clear a road18m from a sampled shoreline.

Proposed read-only additions: an asset-family inventory including inline/detached upgrades and unlock requirements; an oriented native preview returning footprint, entrance/track connectors, attachment areas and airport approach restrictions; a map-wide typed-network/outside-connection summary; and a higher-resolution bounded terrain/resource query. Include native collision/placement reasons and estimated costs without purchasing the asset. Treat these as discoverability/capability gaps, not proven game bugs. No bridge code was changed. Evidence `.local/ip-*`, lesson L045.

## B018 — Build remains busy after a timed-out road operation

Observed October 10 during industrial road construction: request `09`, a `cs2_connect_road` for `Highway Oneway - 3 lanes` from (-2642.866, 3247.03247) to (-2655, 3285), timed out. The intended connection was about 40 m long. Fresh road graphs showed no connecting segment, and subsequent build requests repeatedly returned "another build operation is in progress, retry shortly". Closing the native construction panel, selecting zoning, and loading the verified **Ezra-Industrial-Roads-Recovery** save did not clear the busy response. A full game restart was planned at the time of this entry; successful recovery is not yet established. The underlying cause is unproven, and a timeout alone must not be interpreted as cancellation. Evidence: `.local/ib-taper-fix-req.json`, `.local/ib-built-09.json`, `.local/ib-built-09r.json` through `ib-built-09t.json`, `.local/ib-built-11.json`, `.local/ib-built-12.json`, and `.local/ib-terminal-live.json`.

Extend B002's completion tracking with an operation ID and read-only status exposing the current build phase, elapsed time and completion/error state. Add safe cancellation with a confirmed terminal result, plus a watchdog for stalled operations. On city load, reconcile pending operations with the new world and reset stale build ownership only after confirming that no native build can still commit. Return a final topology/change receipt before allowing a retry. These are proposed fixes; no bridge code was changed.

Follow-up, same session: a complete game-process restart cleared the busy state; normal road construction then succeeded. Reloading alone had not worked. The restart/reload automatically resumed the saved city, requiring an immediate fresh pause. The short taper was left to the user as offered, so its geometry is still untested. Final road graph verified63 retained operations,127 segments, and only the two intentional dead ends. See L046. No bridge implementation was changed.


### Service siting telemetry follow-up (October 10)

Current service inventory and prefab footprints helped choose empty sites, but /city/services still omits health, police and death-care usage. Native panels showed312deaths/month versus221cremation/month,1033/1500cemetery occupancy,37/40jail use, and no prison capacity. Expose those rates with units plus building patients/beds, deployed/available emergency vehicles, operational warnings, road connection and upgrade state. Distinguish roadside dryRun (geometry only) from native placement validation. Scope: bridge improvement note; no mod changes in this gameplay pass.


## B019 — Bicycle and bus-lane upgrades need native composition support

October10, requested by the user during the transit expansion. Current cs2_upgrade_road supports only grass, trees, wideSidewalk, soundBarrier, parking, lighting, medianGrass and medianTrees. HandleUpgradeRoad performs an exact alias lookup and rejects unknown values; native PrimaryLane/SecondaryLane strings are not a supported fallback. Generic connect/build emits new network definitions, not the existing-road overlay workflow. FencePrefab is not evidence of a decorative-only asset: the native bicycle overlay uses that family.

Installed native metadata maps bicycle lane requirements to CompositionFlags.Side.SecondaryLane and public-transport lanes to Side.PrimaryLane, separately for left/right. Native overlay placement reads PlaceableNetData set/unset masks and emits an original-road plus Upgraded definition. The current bridge needs explicit support; a bare enum bit is not enough to promise compatibility on all streets. Native UI applied a small bicycle-lane trial, but manual one-segment interaction is inefficient. User took over cycling. Dedicated bus lanes were not added.

Proposed focused upgrade API:
- bicycleLane/publicTransportLane aliases; explicit side left/right/both, defined relative to start→end; add/remove actions.
- Read existing composition and apply native set/unset rules while preserving unrelated opposite-side lanes, parking, decorations and lighting.
- Expose native placement flags and supported compositions. Dry-run native validation should return cost, conflicts/displaced parking and exact tool errors without purchase.
- Batch existing segments sequentially through the native upgrade pipeline, preserving curve/nodes and returning replacement IDs plus individual outcomes.
- Read back composition masks, actual lane types/directions/permissions, stop compatibility and junction lane connections to prove a continuous corridor.

Related transit telemetry gaps: surface/underground asset footprint and connector preview; operation status beyond depot count; a route/platform direction preview. Evidence and source audit .local/tr-cap-review.md; RequestHandlers.Build.cs HandleUpgradeRoad and BridgeToolSystem upgrade definition handling. Scope is a recorded improvement request, not an implemented patch.

## B020 — Cargo and air creation are missing despite working discovery

October 10 cargo session, audited October 11. Stop listing exposes compatible cargo train platforms, airport freight gates and outside connections. Unfiltered line listing reads native cargo rail/air routes. Creation does not support them: TryResolveLinePrefab selects passenger and excludes cargo, IsValidLineStop requires passenger compatibility, and CreateTransitLine explicitly rejects airplanes. The error says airplane routes cannot be drawn and airports fly automatically; this must not be repeated as a game rule. Historical native readback contains two Cargo Airplane Route entries created while the user was controlling the UI. The user later confirmed takeover. No cargo routes were created by the agent, and none were operating in the last paused snapshot (zero assigned vehicles).

Extend the native route pipeline with explicit mode and passenger/cargo purpose, exact compatible platform/gate selection, outside destinations, return-path validation, ordinary unlocks and idempotent completion. Verify dispatch and freight movement separately from route existence. Also correct ListTransitLines: passengersOnBoard=data.cargo is misleading for freight; native cargo data sums resource amounts. Add distinct load/capacity/units rather than labeling freight as people or monthly throughput. Confirm mass conversion against native UI before choosing kg/tonnes. Sources: [Transit handler](../../CS2MCP.Bridge/RequestHandlers.Transit.cs), CreateTransitLine/TryResolveLinePrefab/IsValidLineStop/ListTransitLines; ignored .local/cg-current-lines.json and cg-review-*-il.txt. Roadmap R01/R06A. No implementation changed.

## B021 — Existing route and roadside-stop lifecycle is incomplete

October 11 source review. Passenger line creation supports name/color; list/delete, line policies (including fares/fleet), and vehicle-model tools already exist. There is no public existing-line stop insert/remove/reorder or standalone metadata update. BridgeTransitRenameSystem supplies reusable native rename handling for newly created routes, not a general edit endpoint. Bus/tram stop placement/listing exists, but generic demolition accepts buildings, road edges, flora and districts rather than standalone transit poles. Route deletion deliberately leaves physical stops.

Add native route editing with path preview and preservation of identity/settings; shared supported net-object move/remove for poles/mailboxes; orphan health and duplicate-candidate diagnostics. Protect station-integrated stops and outside connections. The historical cargo snapshot contained two Salford loops using the same terminal platform with reversed two-stop order, plus one Sibley loop. Flag that as potentially redundant; it is not permission to delete current user routes or evidence of a bridge-created duplicate. Sources: [Transit](../../CS2MCP.Bridge/RequestHandlers.Transit.cs), [rename system](../../CS2MCP.Bridge/BridgeTransitRenameSystem.cs), [demolition](../../CS2MCP.Bridge/RequestHandlers.Build.cs). Roadmap R06B/R10; extends B014/B016.

## B022 — Capability exposure, errors and metric contracts need a shared model

October 11 source and tool-metadata review, no live bridge query. The historical full MCP catalog has65 tools, but this session directly exposes10 CS2 tools. Implemented/registered and client-exposed are different states; an allowlist gap must not be described as missing source functionality. Server capability reporting should include protocol/mod/game build, schema and supported native operations. Client discovery supplies exposure status; the server cannot independently inspect a client's tool allowlist. Preserve the loopback-only connection and existing permission scope.

Confirmed contract mismatches include demand described as0–100 versus handler0–255, signed budget totals, freight load under a passenger label, negative orphan-route length, and dryRun meaning alignment-only rather than native collision validation. Ranges and setter receipts also need review: some native slider mutations return before end-of-frame application. MCP bridgeJson/errorResult discards HTTP status and structured native error detail, hindering reliable retry decisions. Return typed quantities/units/periods/frame/freshness, stable error codes and verified effective values. Missing/unsupported must not silently become zero. Sources: [MCP adapter](../../mcp-server/src/index.ts), [economy](../../CS2MCP.Bridge/RequestHandlers.Economy.cs), [line policies](../../CS2MCP.Bridge/RequestHandlers.LinePolicies.cs), [placement](../../CS2MCP.Bridge/RequestHandlers.Placement.cs). Roadmap R01; consolidates B003/B004/B014 and unnumbered unit notes.

## B023 — Predetermined action chains need supported batch receipts

October 10, the user explicitly asked for more chaining. The local step helper completed390 sequential zoning actions for496 newly painted cells, with per-call intent/result files and screened rectangles. That is one successful external helper run, not a native batch API, exact-cell capability or measured general speedup. Existing whole-road replacement already accepts multiple segments; extend batching deliberately rather than claiming all batching is absent.

Provide bounded action lists with step IDs, explicit dependencies, expected world/revision and pause/geometry preconditions; stop on first failure or changed state, and resume only uncommitted steps. Reconcile ambiguous responses before retrying. Add grouped observations with sampling metadata, bounded main-thread work and measurable performance. No concurrent city mutations, hidden auto-retry or promise of atomic rollback across native builds. The current queue drains pending work in one UI update; workload limits must keep the game responsive. Sources: [BridgeSystem](../../CS2MCP.Bridge/BridgeSystem.cs), [road replacement](../../CS2MCP.Bridge/RequestHandlers.Roads.cs), L041/L054. Roadmap R02/R03/R04.

## B024 — Player takeover and save loads need world-scoped control

October 11 source review of repeated gameplay interruptions. HTTP timeout only ends the wait; it does not cancel queued/native work. Entity index+version and a paused frame are not a durable loaded-world identity. AutoPauseTargetFrame is not cleared by ordinary SimControl pause or the current load-complete callback in this system lifecycle; /state does not expose its ownership/status. This is a source risk requiring a load/takeover test, not a newly observed late pause in a different city. Generic construction also responds at Apply before Finish, unlike road/transit completion checks.

Introduce operation/world-generation tokens, one mutation owner/lease, explicit player takeover/stop, queued-work cancellation and status for an already applying operation. Invalidate previews and pending actions after load or relevant manual edits; settle native tool ownership rather than blindly clearing a busy flag. Keep existing road/transit interruption, native error and pathfinding checks. A wall-clock deadline is separate from simulated hours, and a blocked UI thread limits any in-process stop guarantee. SaveGame currently discards the asynchronous save task; completion/error and checkpoint identity need tracking. Sources: [queue/timer](../../CS2MCP.Bridge/BridgeSystem.cs), [request timeout](../../CS2MCP.Bridge/BridgeRequest.cs), [save/run](../../CS2MCP.Bridge/RequestHandlers.Meta.cs), [simulation control](../../CS2MCP.Bridge/RequestHandlers.cs), [native tool](../../CS2MCP.Bridge/BridgeToolSystem.cs). Roadmap R02/R03; extends B002/B005/B018.

## B025 — Raw employee buffers are not verified service staffing

October 10 maintenance-depot inspection reported40/48 employees where earlier native panels showed30 positions. The samples differed in time and generic InspectEntity returns the raw Employee buffer length. This is unresolved metric semantics, not proof that the game's staffing is wrong. Earlier service work also showed the purchase card's maintenance excluded substantial wages.

Add current/required staffing with defined shifts/roles, efficiency factors, vehicles deployed/available, capacity/usage and actual upkeep. Cover school enrollment, patients/beds, emergency dispatch, jail/death processing/storage, garbage, parks and maintenance condition. Separate parent buildings from upgrades and stocked amounts from rates. Compare same-frame native panels before defining these fields. Sources: [InspectEntity](../../CS2MCP.Bridge/RequestHandlers.Perception.cs), [city services](../../CS2MCP.Bridge/RequestHandlers.CityData.cs), L016/L050/L053 and SESSION-21. Roadmap R08; promotes earlier unnumbered service diagnostics into a deliverable.

## B026 — Spatial readback and snapping omit important geometry

October 11 source audit confirms ResolveBuildEnd chooses by XZ distance; e1/e2 apply only to unsnapped ends. A requested depth is therefore not an explicit choice of a stacked network. Current graph scope filters by segment midpoint and reports endpoints without full curve controls; nearby-building center filters can similarly omit a footprint intersecting a query. These are source-confirmed limitations, not evidence that every old connection is incorrect.

Add explicit entity/split/height targets, 3D tolerance, full Bezier and lane readback, footprint-intersection queries, clear scope/truncation metadata and native reconstruction receipts. Validate directed switches and platform returns separately from undirected reachability. Road-overlay preservation also needs a native parity test: the upgrade path constructs supplied flags without an explicit merge of existing Upgraded flags; actual collateral loss has not been observed. Sources: [road snapping](../../CS2MCP.Bridge/RequestHandlers.Roads.cs), [graph](../../CS2MCP.Bridge/RequestHandlers.Vehicles.cs), [perception](../../CS2MCP.Bridge/RequestHandlers.Perception.cs), [upgrades](../../CS2MCP.Bridge/RequestHandlers.Build.cs). Roadmap R05/R07; complements B009/B012/B019.

## B027 — Whole layouts and protected parcels need a shared representation

Repeated user feedback rejected graph-connected interchanges, a campus and rail approaches on appearance/layout grounds. Local JSON coordinates and Markdown reservations helped planning, but no common bridge preview or protected-parcel API enforces them. A successful native placement does not prove the design is attractive or leaves usable development space.

Provide world-scoped plan objects containing named geometry, native asset/upgrade envelopes, intended road/rail gates, setbacks and protected future-use polygons. Show an overlay/map with exact affected assets and cost before committing; revalidate after user edits. Exact zoning, road previews and facility placement should consume the same reservations. This assists review rather than claiming an automatic aesthetic score. Keep planning metadata separate from game objects until apply. Evidence L019/L028/L043/L048/L054 and prior campus/industrial plans. Roadmap R04/R05/R07.

## B028 — Industry planning lacks local resource and production accounting

October 11 source review. Tile feature totals, native pollution/groundwater grids, labor and generic statistics already exist. They do not expose full local ore/oil/fertility geometry, owner-bound extractor areas, company input/output/storage flows, freight inventory/transfer performance or resource balances. The earlier industry plan needed large reserved compounds and manually positioned facilities; a small hub footprint did not describe its production area.

Add bounded native resource queries with actual resolution and units, company/terminal resource inventories and rates, and owner-aware extractor-area read/edit using normal rules. Include utilities' fulfilled per-network flows and trend/context when evaluating future industry; a battery-supported snapshot is not sustainable surplus. New forecasts must expose assumptions instead of promising unmeasured export or recycling income. Sources: [tile features](../../CS2MCP.Bridge/RequestHandlers.MapTiles.cs), [utility aggregates](../../CS2MCP.Bridge/RequestHandlers.CityData.cs), [grid/inspection](../../CS2MCP.Bridge/RequestHandlers.Perception.cs), L045/L047/L054. Roadmap R10/R11.

## B029 — Regression coverage and release documentation lag gameplay experience

October 11 repository review. The Node package exposes build/start scripts but no test script; no maintained test tree was found in the source inventory. This does not deny earlier one-off local validation. The setup/design records preserve October8 installation milestones and still say loaded-city play was unverified, while later sessions demonstrate it. README lists62 tools versus the historical65-tool catalog. Version strings differ between Node0.8.0, Mod.Version0.9.0 and C# project0.4.0; this is compatibility-reporting ambiguity, not proof of runtime incompatibility.

Add contract fixtures and focused native integration checks per feature, including timeouts, loads, takeover, invalid selections and supported upgrades. Maintain tested game/mod/server/protocol identities, capability-derived documentation and dated verification status. Compile with SkipDeploy=true, deploy with the game closed and retain rollback. Benchmark batch correctness and responsiveness before claiming speedups. Preserve proprietary libraries and raw saves/screenshots in ignored local paths. Sources: [package scripts](../../mcp-server/package.json), [mod identity](../../CS2MCP.Bridge/Mod.cs), [project](../../CS2MCP.Bridge/CS2MCP.Bridge.csproj), [setup guide](../codex-setup.md), [original plan](../superpowers/plans/2026-10-08-cs2-bridge.md). Roadmap R12. No tests, builds or deployment were run for this documentation-only review.
