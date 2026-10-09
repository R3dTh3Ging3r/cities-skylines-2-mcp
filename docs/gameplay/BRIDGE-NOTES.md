# Live bridge findings — October 8, 2026

## B001 — Spaces in zoning and service names

Observed `cs2_zone_area` with `NA Residential Low` rejected as unknown `NA+Residential+Low`. The MCP server's URLSearchParams form encoding uses plus for spaces; the bridge path does not decode plus as a space. Calling the same documented route with percent-20 succeeded and painted 1,210 cells. Other older name-based routes may share this issue. Newer road routes already have a bridgeQueryString helper. Candidate patch: use one query serializer consistently, with regression tests for spaces, ampersands and literal plus. Temporary gameplay workaround: correctly percent-encoded localhost requests through ignored helper script.

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
