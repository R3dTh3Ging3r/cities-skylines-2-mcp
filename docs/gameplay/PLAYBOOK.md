# Cities Skylines II playbook

Research date: October 8, 2026. Installed baseline: **1.6.2f1**, confirmed in the local Player.log. This playbook combines linked developer explanations with proposed tactics. Older feature diaries explain systems but do not establish current prices, balance, or optimal thresholds; use the running game's values.

## Current patch context

The developer's Autumn Breeze preview describes changes to garbage collection priorities, company selection based on needed resources, residential vacancies in demand calculations, and income-based household decisions. These are reasons to distrust old recipes that assume permanently high demand or the old household behavior. The preview is an announcement, not by itself proof that every detail shipped. The installed version matches the [1.6.2f1 release announcement](https://store.steampowered.com/news/app/949230/view/686390624621953962); verify outcomes in the city. [Developer preview](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/autumn-breeze).

## The opening strategy

The following is our starting policy, not a claim that there is one optimal city layout:

1. Pause and survey the map, owned tiles, outside connections, terrain, water, pollution, available prefabs, money, and recurring expenses.
2. Establish a compact connected road network with room for a later through-route. Prefer inexpensive local streets for initial development; reserve arterial corridors without immediately building oversized roads.
3. Provide connected electricity, clean water, and sewage disposal. Compare the actual purchase and operating costs of feasible local generation and imports.
4. Add small amounts of housing, employment, and nearby shopping based on demand factors and available workers. Keep polluting activity away from homes and clean-water sources.
5. Run a short interval, inspect results, and expand the bottleneck that actually limits growth. Avoid filling large areas merely to empty demand bars.
6. Add services as capacity, access, and the budget justify them. Unlocking a building is not a reason to buy it immediately.

## Money and growth

**Documented mechanics:** Economy 2.0 removed city-budget government subsidies, increased service upkeep, and introduced a cost for importing city services. Demand also depends on household needs, housing preferences, workers, and company activity. Schools and suitable jobs matter to the workforce. [Economy 2.0 Part 1](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-one).

**Our policy:** Before construction, account for both the purchase and recurring cost. Distinguish treasury cash, recurring balance, loans, and one-time milestone rewards. Never call a city profitable solely because its treasury rose after a reward or loan.

Use a provisional reserve of two observed months of operating loss plus the next necessary utility intervention. This is a planning heuristic, not a game rule; adjust it to the user's deadline and objective. If monthly loss is positive and reasonably stable, estimated runway is `cash / monthly loss`. Otherwise report runway as unknown or not currently declining. Do not confuse monthly rates with elapsed game hours.

Change taxes or service budgets incrementally and observe the tradeoff. Diagnose expenses before cutting every service to its minimum. Cheap service budgets can become expensive if they cause shortages, missed collections, or lower efficiency. Borrow only for a stated purpose with a plausible repayment path; loans do not repair structural losses.

## Demand, housing, and jobs

**Documented mechanics:** Housing, employment, shops, and production interact. Commercial areas need customers and workers; vacancies can reduce housing demand; different densities serve different households. [Zones and demand](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings).

**Our policy:** Read the factor behind a demand bar before zoning. Add one manageable block and observe occupancy. Match jobs to the available education levels; a large number of vacant jobs can coexist with unemployment if qualifications or access do not match.

For persistent high-rent warnings, inspect employment, household income, vacancies, and housing alternatives before demolishing homes or cutting taxes. The revised rent explanation ties warnings to income and describes cheaper housing or moving away as possible responses. [Rent and household adjustment](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-two).

Do not promise that a new school fixes a labor shortage immediately. Education and migration require simulation time. Keep a mix of suitable jobs and reassess after several observation windows.

## Utilities and pollution

**Documented mechanics:** Most roads carry utility infrastructure, but network connectivity still matters. Electricity distribution can bottleneck despite adequate production. Surface-water flow determines where sewage travels; groundwater can be polluted or depleted by excessive extraction. Temperature can change electricity demand. [Electricity and water](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/electricity-water).

**Our policy:** A city-wide surplus is not proof that every neighborhood has service. Inspect the affected connection before buying another plant. Place sewage away from clean-water intake routes using the actual flow overlay; protect groundwater deposits from polluted land. Verify the chosen road or network supports the needed connection.

Keep a modest capacity margin and expand ahead of demonstrated shortages. Avoid universal claims that one generator or water source is always cheapest: terrain, unlocks, output, import prices, and upkeep decide that.

## Roads, freight, and transit

**Documented mechanics:** Route choice includes time, cost, comfort, and behavior. Freight distance affects company costs; outside through-traffic can use city roads if they offer an attractive route. [Traffic AI](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/traffic-ai).

**Our policy:** Give industrial freight a direct route to the outside connection without routing it through residential streets. Keep local access streets, collectors, and through-routes serving distinct purposes. Use a few well-spaced arterial junctions; add alternate routes where a single connection concentrates trips. Check where the queue begins before adding lanes or a roundabout.

Treat a road as connected only when the graph confirms it. Similar coordinates or a convincing screenshot are insufficient. After changing a junction, confirm that vehicles can reach both directions of travel.

Public transport needs operational infrastructure and lines, not merely stops. Land-based modes have depots or yards; vehicle counts, service hours, and ticket prices affect operation. [Public and cargo transportation](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/public-cargo-transportation).

Start with a short useful route between real trip generators when affordable. Check vehicles and waiting passengers, then adjust service. Defer expensive rail or metro unless demand and the user's objectives justify it. Make walking routes useful where the available tools support them.

## Progression

Milestones use XP, with progress from development and a functioning city; milestones also unlock options and development points. [Game progression](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/game-progression).

Spend development points on the current constraint or an explicit objective. Avoid building expensive assets just to obtain XP unless the resulting cost is justified by the challenge. Use the actual unlock tree rather than memorized population thresholds from the first Cities: Skylines.

## Diagnostic order

| Observation | Check first | Candidate response |
| --- | --- | --- |
| Treasury falling | Recurring expenses, rewards/loans, utility trade | Delay optional assets; fix the largest avoidable expense |
| Housing demand stalled | Vacancies, jobs, affordability, negative demand factors | Correct the constraint before adding more empty zones |
| Businesses lack customers | Occupancy, access, household income, excess commercial space | Slow commercial expansion and improve catchment access |
| Jobs and unemployment coexist | Education mix and travel access | Match job types; improve access; allow workforce change time |
| A neighborhood lacks power or water | Local network and fulfilled demand | Repair connectivity or distribution before adding production |
| Garbage piles up | Vehicle access, processing, queues, collection behavior | Resolve the affected route or capacity constraint |
| Traffic worsens after growth | Queue origin, freight route, intersection capacity | Test one targeted network improvement |

These are diagnostic hypotheses, not automatic fixes. Record the actual cause when known.

## Operating the bridge

The initial allowlist exposes inspection and simulation tools. Construction, labor, detailed traffic, and statistics tools may exist upstream without being enabled here; check the live tool catalog before relying on them.

Important source-verified details:

- `cs2_demand` returns building-demand values on **0–255**, not the 0–100 stated in its tool description. Company counters can exceed 255. Demand refreshes while simulation runs. Use returned notes and raw units. [Handler](../../CS2MCP.Bridge/RequestHandlers.cs).
- `cs2_budget` reports **monthly rates refreshed hourly**. Expense breakdown entries are positive costs, but the observed `totalExpenses` aggregate is negative; use `balance` directly or add the signed aggregate to income (L005 / BRIDGE-NOTES B003). Do not treat adjacent reads as independent economic evidence. [Budget handler](../../CS2MCP.Bridge/RequestHandlers.Economy.cs).
- The services response does not provide full education/health coverage or garbage-processing capacity. Seek statistics, inspections, or UI evidence rather than inventing absent metrics. [Service data](../../CS2MCP.Bridge/RequestHandlers.CityData.cs).
- Timed runs return immediately and auto-pause later. Saves are asynchronous. Read back state and verify completed saves. [Simulation and save handlers](../../CS2MCP.Bridge/RequestHandlers.Meta.cs).
- A timeout may leave an action queued. Inspect before retrying to avoid duplicate construction. [Request handling](../../CS2MCP.Bridge/BridgeRequest.cs).

Use `observe -> hypothesis -> one change -> bounded simulation -> verify -> record`. For connection tests, 0.1 game-hours is enough to test advancement, not economic success. As an initial strategy, use 1–3 game-hour observation windows for short-term effects and longer windows for population, education, and profitability. Adjust using measured response times. These intervals are provisional and must fit the user's deadline.

## Objectives and time limits

Record targets, ranking, restrictions, starting save, and the meaning of the clock in [SESSION.md](SESSION.md) before the challenge. Distinguish real minutes from game hours or months. Do not infer that a real-time deadline stops while the game is paused.

For a real-time challenge, provisionally budget 10% for baseline/checkpoint work, 70% for useful interventions, and 20% for observation, final evidence, and saving. Change those proportions if the task demands it. Near the deadline, finish and verify existing work before starting a large project. Report success only from measured targets and a verified save.

Read [LEARNING.md](LEARNING.md) for corrections. Our first live session is documented in L004–L006. Starter utility budget reductions helped retain cash, but growth required raising capacity; monitor fulfilled consumption and leave headroom. These observations come from one city and are not universal settings.

## Lessons from the second challenge

- Inspect happiness-factor contributions before adding services. Crime risk can depress happiness even when recorded crimes are zero; a count alone misses that problem (L009).
- Compare job vacancies by education with unemployment. Newly unlocked offices may help an educated workforce, but add a modest cluster and observe hiring before expanding further (L010).
- Review development points at each milestone through the UI until the bridge exposes the tree and legitimate spending. Advanced Road Services and Roundabouts were purchased normally in session two.
- A locally verified utilization reading is stronger evidence of service operation than aggregate capacity; retain unexplained endpoint warnings for investigation (L011).

- Employment and traffic need repeated observations after growth. In session two, unemployment temporarily rebounded above target before recovering; report both the final result and volatility. A better individual road does not establish better citywide flow (L012).

## Lessons from the third challenge

- Track a fixed set of existing road segments and actual use of new routes alongside aggregate traffic flow. Adding unused roads changes the average ([L013](LEARNING.md#l013-traffic-expansion-needs-a-fixed-comparison-cohort)).
- Verify the complete transit dependency chain with fresh unlock data after construction. Default placeholder stops may not satisfy the game's prerequisites; choose normal named assets explicitly ([L014](LEARNING.md#l014-verify-transit-prerequisites-before-committing-the-depot-budget), [B008](BRIDGE-NOTES.md#b008--default-bus-stop-can-be-a-placeholder-lock-error-hides-prerequisites)).
- Include staffed recurring cost in transit decisions. A depot can consume a large share of a small town's surplus even before a useful line operates ([L014](LEARNING.md#l014-verify-transit-prerequisites-before-committing-the-depot-budget)).
- After a large housing influx, pause further zoning until hiring catches up. Resume with modest infill on verified vacant frontage; repeated employment readings matter more than a single favorable sample ([L015](LEARNING.md#l015-services-and-modest-infill-after-a-growth-surge)).

- Purchase cards can exclude wages: the radio mast listed 5,000/month but cost 24,500/month when staffed. Verify total operating cost after staffing, and budget an explicit wage allowance before construction ([L016](LEARNING.md#l016-building-card-upkeep-excludes-wages)).

- Check landfill storage and processing after significant growth, even without warning icons. Compare throughput with current accumulation and leave headroom; track storage trend after funding changes ([L017](LEARNING.md#l017-inspect-storage-before-a-warning-appears)).

## Freeway geometry and feeder-road preparation

The researched working brief is [FREEWAY-PLAN.md](FREEWAY-PLAN.md). Choose a coherent alignment and access hierarchy before placing individual ramps. Use frontage roads selectively beside freeways and collectors to distribute local trips; protect ramp approaches from closely spaced accesses. Real-world design guidance is inspiration, not a guarantee of the game's behavior.

Add visual geometry and vehicle-motion checks to graph verification: consistent carriageway spacing, smooth joining directions, usable merge lanes, no abrupt elevation changes and no persistent forced near-stops on free-flow portions of ramps. Slowing or stopping at a controlled surface-street ramp terminal can be intentional. The bridge's quadratic control point is not a waypoint; plan tangents explicitly and inspect the built result. These are revised decision rules prompted by [L019](LEARNING.md#l019-user-review-exposes-a-geometry-weakness), pending a measured replacement experiment.

For utility replacements, reserve the legal upgrade envelope and access/pipe corridors before setting adjacent roads or zoning. Compare the combined capital cost with the reserve, include staffed upkeep and temporary overlap, and verify replacement service before removing the original. Treat treatment-generated solid waste as part of the waste-capacity forecast. Owner-attached upgrade prefabs are not standalone buildings; base footprint data does not prove full expansion will fit ([L020](LEARNING.md#l020-utility-expansion-needs-a-combined-capital-and-site-check)).

## Fourth-challenge operating checks

- Inspect the built merge positions and node degrees after curve snapping; temporary construction receipts and requested coordinates are not final topology. Reserve extension stubs explicitly and report which movements depend on future land purchase ([L021](LEARNING.md#l021-compound-curves-still-need-post-build-graph-and-visual-checks)).
- Recheck queues across a morning peak and preserve a fixed original-road comparison. Removing a signal can move a queue before it clears; separate that evidence from later road changes ([L022](LEARNING.md#l022-a-morning-queue-and-a-later-clear-road-are-different-observations)).
- Commission treatment with both sewage utilization and reclaimed-water quality checks, then remeasure solid-waste generation. Maximum landfill funding is not proof of adequate throughput. A pollution-reduction upgrade is not a processing upgrade unless the live card says so ([L023](LEARNING.md#l023-treatment-trades-water-pollution-for-operating-expense-and-solid-waste)).

- Preview loan terms without accepting before recommending a principal. Compare the UI monthly interest with staffed facility costs, shared budget changes and temporary service overlap; zero current debt does not imply a zero-rate offer ([L025](LEARNING.md#l025-preview-loan-costs-before-recommending-a-principal)).

- At a divided-highway to local-road transition, inspect automatically created traffic controls before opening traffic. A taper can acquire a light despite having no crossing traffic. Retain reasonable junction spacing and recheck queues at comparable peak times after adjusting controls ([L026](LEARNING.md#l026-a-highway-terminal-needs-an-explicit-control-check)).
- Treat built:true with an empty segment receipt as an ambiguous report requiring a fresh graph query; native reconstruction may move nodes or reuse entities. Do not duplicate a successful command based on an empty receipt ([L026](LEARNING.md#l026-a-highway-terminal-needs-an-explicit-control-check)).

## Corridor design after the user's interchange review

Use the [road design study](ROAD-DESIGN-STUDY.md) and [current expansion brief](EXPANSION-BRIEF.md) before choosing the Beech alignment. Compare complete interchange footprints, including ramps, terminal storage, bridge approaches, local crossings and remaining buildable blocks. A service diamond is the first candidate to test for access to both neighborhoods; it is not selected until the survey supports it. Preview geometry before placing segments. Require graph, visual and observed-motion checks separately. This is a process correction from repeated user feedback, not measured proof of a replacement's capacity ([L028](LEARNING.md#l028-connectivity-was-an-incomplete-design-standard)).

Keep local east–west connections independent of freeway access where practical. Do not force frontage roads into the constrained waterfront strip merely to follow a design label. Preserve school/service upgrade sites and test both-side access before zoning the corridor. A compact footprint is unacceptable if queues spill onto the freeway; a large footprint is unacceptable if it needlessly consumes the intended neighborhoods.

New user-reported work supersedes stale construction tasks. Refresh the actual city before reusing entity IDs, cost assumptions or checkpoint names. Recycling construction and treatment relocation are no longer pending objectives ([L029](LEARNING.md#l029-user-progress-supersedes-the-utility-migration-backlog)).

## Redevelopment cutover rule

For the user's planned rebuild, perform demolition, service relocation, roads, utility reconnection and replacement zoning in one paused construction pass. Validate the completed network before resuming. Separate capital spending during pause from recurring deficits during post-resume recovery; do not budget operating loss against real minutes spent building ([L032](LEARNING.md#l032-paused-construction-separates-capital-spending-from-recovery-losses)). Use the [surveyed redevelopment plan](REDEVELOPMENT-PLAN.md) as a concept, and native previews for exact geometry/costs. Check old industrial soil before residential or school infill; verify full student capacity instead of treating EducationCount as available school seats ([L031](LEARNING.md#l031-a-live-survey-changes-the-redevelopment-recommendation)).

## Handoffs and transit cleanup

After a user takes over construction, resurvey buildings and networks before continuing old steps. At a frozen simulation frame, aggregate population and service consumption can reflect the pre-clearance city; verify actual facilities and topology before commissioning ([L033](LEARNING.md#l033-user-takeover-invalidated-the-pending-rebuild-sequence)).

For lingering bus-stop markers, inspect both physical stops and the route's ordered waypoints. Remove the specifically obsolete route when its stops are gone; preserve outside connections. A zero-stop route with negative length is broken telemetry, not a useful distance ([L034](LEARNING.md#l034-bus-stop-warning-markers-can-outlive-physical-stops)).

When a final graph disagrees with old construction receipts, first check whether the receipt nodes still exist. Later junction reconstruction can replace them. Verify connectivity using the current graph and geometry; do not rebuild a road just to recover an old entity ID ([L038](LEARNING.md#l038-rebuilt-junction-ids-can-produce-false-disconnected-road-reports)).

For a requested city/urban service version, confirm its localized asset name, footprint, price and capacity before placing it. Do not infer variant identity or capacity from size or prefab suffix. Distinguish listed base capacity/upkeep from actual staffed operation, and retain upgrade space without promising untested fit ([L039](LEARNING.md#l039-city-service-variants-have-distinct-capacity-and-footprints)).

Before continuing a surface street into an avenue near an overpass, inspect the avenue's road heights and touchdown location as well as its top-down alignment. Prefer an at-grade junction. Size civic reservations from the actual asset footprint, then retain additional grounds; a coordinate-only preview does not validate collisions or upgrades ([L040](LEARNING.md#l040-civic-reservations-need-asset-and-junction-elevation-checks)).

For a large campus or district with strong aesthetic requirements, make the proposed arrangement visually reviewable before committing major buildings and road relocation. Explain the grounds, entrances, extensions, housing and walking connections together; a footprint-feasible arrangement is not an accepted design. The user rejected the first university-campus layout despite successful native placements ([L043](LEARNING.md#l043-campus-footprint-fit-did-not-establish-an-acceptable-layout)).

When planning a residential district beside existing interchanges, screen each new street against current ramp geometry and overpass heights before building. Close local loops inside the available land and reuse existing surface junctions where practical. Count physical approach directions as well as graph edges: divided ramp pairs can give a normal four-direction intersection five edges. Reserve school/service plots before subdividing the remaining blocks, but treat native building and upgrade fit as a later check ([L044](LEARNING.md#l044-residential-road-planning-benefits-from-current-ramp-envelopes-and-reserved-service-plots)).

Before fixing a freight district layout, locate the global rail network and outside connections, measure the gap to the proposed terminal, and include a yard plus track approaches. A terminal rectangle alone is not a functioning railway plan. Reserve the largest airport/factory footprints before subdividing surrounding land; distinguish base size, detached upgrades, circulation and approach corridors. Coarse terrain samples screen candidate sites but do not clear shoreline roads or native asset collisions. Review roads against existing ramp envelopes and reserved rail strips before construction ([L045](LEARNING.md#l045-freight-planning-needs-global-connections-and-full-facility-reservations)).
Industrial construction checks: validate proposed road pavement against exact tile bounds, ramp nodes and pylons, independently of coarse parcel screening. Keep schematic facility access marks provisional until real gates and upgrades are previewed. After any save load or game restart, immediately read and restore the intended pause state. For a timed-out build, read current geometry before retrying; persistent busy responses after tool changes/reload are a recovery problem, not proof that another build is safe. Preserve a verified current save before a full restart. One successful restart recovery is recorded in [L046](LEARNING.md#l046-industrial-road-construction-needs-native-validation-and-a-recoverable-tool-state), with bridge improvements in B018.


### Verify external facility connectors before surrounding roads

For stations and yards, compare each track endpoint with the placed asset footprint and internal vehicle circulation. An internal dead end is not automatically an external connection. Keep approach space open until the actual exterior connector is identified; revise nearby reservations before building roads that block it. One cargo-terminal observation motivated this rule: [L047](LEARNING.md#l047-facility-connectors-can-invalidate-a-previously-reserved-approach).


For an industrial rail yard, plan both terminal tracks, the full siding ladder, depot approaches, power corridors and service roads together. Prefer consistent parallel tracks with ample turnout length; do not squeeze an additional platform into an early short merge or send a westbound depot lead south merely to meet an existing spur. Asset placement success and graph connectivity do not establish aesthetics or throughput. Use native relocation to preserve upgrades when changing orientation. Validate tunnel elevation separately from horizontal snapping ([L048](LEARNING.md#l048-a-technically-connected-railway-can-still-fail-the-industrial-yard-design-brief)).

At rail switches, use a finite joining angle with a long approach; an exactly tangent merge can fail native overlap validation. After demolition, upgrades, or tunnel construction, refresh segments and junctions before using IDs or checking connectivity. Native reconstruction can move a requested portal junction, and in-place upgrades can add new gates without adding a separate building inventory entry. Check forward depot-to-terminal movements separately from undirected connectivity; include a deliberate return connection where parallel approaches otherwise face away from the destination ([L049](LEARNING.md#l049-native-rail-turnouts-and-portals-reconstruct-geometry)).


For underground transit, size the plan from actual generated platform endpoints and elevations, not the entrance lot. Use exact platform IDs and deliberate return-stop order; validate route creation, then observe depot dispatch and passengers separately. Preserve straight track sections and terminal tails for future stations/extensions. Compare ridership/queues at similar in-game periods before attributing change to transit ([L051](LEARNING.md#l051-underground-station-surface-size-does-not-describe-its-track-footprint)).

Treat bicycle/public-transport road-service overlays as native existing-road upgrades. Prefab class alone does not establish behavior. Use verified composition support, preserve unrelated road upgrades, and read back actual lanes/junction continuity before claiming a complete corridor. An isolated native click or drag is insufficient ([L052](LEARNING.md#l052-bicycle-overlays-require-the-native-existing-road-upgrade-path), [B019](BRIDGE-NOTES.md#b019--bicycle-and-bus-lane-upgrades-need-native-composition-support)).

For parks across an existing city, compare current park inventory with actual occupied buildings before choosing sites. Empty road grids are future neighborhoods, not current leisure gaps; civic/office frontage can suit a garden or plaza while housing gaps suit playgrounds and neighborhood parks. Screen full footprints, then use native validation and inventory to verify. Confirm park-maintenance provision separately and distinguish placement from demonstrated upkeep/visitor use. See [L053](LEARNING.md#l053-developed-neighborhood-park-distribution-needs-an-occupied-building-survey).


Batch predetermined bridge work sequentially when its targets and preconditions are already checked; the user prefers this over an inspection between every small action. For stepped zoning, precompute bounded 8 m rows, screen the full brush reach against avenue/rail/building/reservation exclusions, refresh live geometry at batch boundaries, and record each mutation before advancing. Stop on ambiguity or changed state; never blindly replay after timeout. This preserves useful checkpoints without serializing every read around every brush point. One390-action batch sequence succeeded; no general timing advantage was measured ([L054](LEARNING.md#l054-predetermined-zoning-batches-need-spatial-exclusions-and-resumable-receipts)).

Check route capabilities separately for each transport mode and passenger/cargo purpose. Existing cargo stops and readable native routes do not establish bridge creation support. Use compatible exact gates/platforms and verify return paths, mode-specific dispatch and actual loading independently. An active route is not proof of operation; the current cargo passengersOnBoard field is a mislabeled freight quantity with unverified mass conversion. Preserve user-built routes and refresh after takeover before diagnosing redundancy ([L055](LEARNING.md#l055-cargo-discovery-does-not-imply-creation-or-operating-support)). Prioritized bridge changes are in the [roadmap](../BRIDGE-ROADMAP.md); they remain proposals until implemented and tested.
