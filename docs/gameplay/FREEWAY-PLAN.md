# Ezra northern freeway and utilities: design brief

Prepared October 9, 2026. Research is outside the next one-hour gameplay challenge. No city construction or simulation advancement occurred during this preparation. The user wants the incoming freeway to curve west north of industry, continue toward the western owned boundary, and have attractive, efficient access for later expansion.

The user subsequently added a wastewater treatment facility with full expansion space and a durable location, plus replacement of the landfill with a recycling center **only if affordable**. Plan the industrial district, freeway and utilities together. Reserve space now without purchasing every facility upgrade immediately.

## Evidence and limits

Read-only bridge inspection confirmed Ezra City remains paused at frame 9462047, the previous challenge's final frame. Nine owned tiles form a square approximately bounded by x=-2181.565 to -311.652 and z=-311.652 to 1558.261. The incoming highway is around x=-1120, giving approximately 1.06 km to the western owned boundary. Its two carriageways continue north beyond owned land; preserve those outside connections. Raw graph and tile data are in ignored `.local/s4-research-*` files.

The user observed vehicles slowing almost to a stop at the sharp industrial exit. This is valuable direct feedback, not an independently measured before/after speed result. The last challenge established connectivity, but final city flow declined from 71% to 66%. A functional graph and an attractive screenshot do not establish good road geometry or throughput.

Exact construction coordinates remain provisional until terrain, buildings, utilities, bridge clearance and lane connections are surveyed. The existing corridor screenshot is historical. Do not treat it as a current terrain survey.

## What the research changes

**Separate freeway movement from property access.** A frontage road runs beside a freeway and receives ramps; cross streets distribute trips from there. Keep driveways and nearby intersections out of ramp connection areas. This is the feeder-road approach relevant to this corridor. [TxDOT frontage-road guidance](https://www.txdot.gov/manuals/des/acm/chapter-2--access-management-standards/section-3--number--location--and-spacing-of-access/frontage-roads.html).

**Use collectors inside town.** Collectors connect local streets with arterials. In Ezra, an industrial collector should gather factory/service traffic, while a separate town collector serves neighborhoods and shops. Their purpose should be visible in junction spacing and connections, not just lane count. [FHWA road hierarchy](https://www.fhwa.dot.gov/policyinformation/pubs/our_nations_highways_2026/roads.cfm).

**Provide room between maneuvers.** Closely spaced entrances and exits create crossing lane-change movements. Ramp spacing and auxiliary lanes must be considered together. Apply this principle to the game; do not transplant real-world distance requirements into a small game tile as if they were simulation rules. [TxDOT interchange design considerations](https://www.txdot.gov/manuals/des/rdw/chapter-15-grade-separations-and-interchanges-/15-6-general-design-considerations.html).

**Start with a simple service interchange.** Diamond layouts connect a freeway to a cross street through four ramps; their terminal intersections still need suitable control and queue storage. A cloverleaf consumes more land and introduces weaving between loops. A freeway-to-freeway junction serves a different purpose. [FHWA interchange guide](https://highways.dot.gov/field-offices/missouri/interchange-design-promptlist).

**Use the game's lane tools deliberately.** The developer describes constructing acceleration/deceleration lanes by widening a highway section by one lane and attaching a one-lane ramp to that extra lane. Parallel roads and curves support a consistent divided alignment. Bridge placement must still be verified in the running game. [Cities: Skylines II road tools](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools).

**Measure whether drivers actually benefit.** The developer describes routing costs involving time, comfort, money and behavior. A longer bypass may attract little traffic if it offers a poor trip. This is why route usage and queues matter alongside citywide flow. The 2023 diary explains intent, not a guarantee of the installed patch's exact behavior. [Cities: Skylines II traffic AI](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/traffic-ai).

## Recommended layout

1. Make the incoming pair of highway carriageways sweep west through a broad, continuous bend north of industry. Begin the bend early enough to avoid a kink. Preserve two through lanes in each direction initially, with consistent median spacing. The westbound carriageway should be north of the eastbound carriageway on the east-west section under right-hand traffic.
2. Continue the mainline toward x=-2181, stopping safely inside the owned boundary. Leave aligned extension ends and clear land for later continuation. This is an expansion provision, not a new outside connection at an internal tile boundary. Vehicles must have usable exits before unfinished ends.
3. Place one diamond-style service interchange on the straight western section, clear of the mainline bend. Favor a single crossroad bridge over two elevated freeway decks if terrain and clearance permit. Provide all four ramp movements, moderate ramp curves, gentle vertical transitions and straight merge/diverge approaches.
4. Use a short industrial-side frontage/distributor road where it helps distribute trips, connecting the ramp terminal area to the industrial collector and western town collector. Reserve the opposite-side frontage corridor for future development; build only the connections needed now. This is not a requirement to pave two full-length frontage roads immediately.
5. Keep industrial loading streets off the ramps. Give the main collector fewer, deliberate junctions, and place local accesses away from terminal queues. Keep walking routes connected across the corridor at the crossroad rather than routing pedestrians along highway ramps.
6. Retain a working city entrance during replacement. Reuse useful parts of the old western route as collectors where appropriate. Remove the sharp ramp and improvised loop only after the replacement is verified in both directions. Avoid redundant ramps crowded against the new bend.

The result should read visually as one coherent corridor: parallel carriageways, smooth approaches, a consistent median, one legible interchange and deliberate green buffers. Planting comes after geometry and operation are checked. Do not add ornamental loops to fill empty space.

## Integrated industrial and utility layout

At the start handoff, the user made treatment and recycling secondary to the freeway. Recycling remains conditional on affordability. Keep the industrial collector as the shared access route for factories and utility service roads, with a direct connection to the interchange. Large facility entrances belong on service streets set back from ramp terminals; do not put a queue-producing driveway at a merge. Keep residential through trips on the town collector.

Survey the industrial edge for a contiguous utility campus before fixing the interchange and collector alignment. Choose land outside the freeway widening/extension reserve, away from the intended town expansion and clean-water sources. Inspect terrain, pollution, groundwater and applicable wind direction. Neither treatment nor recycling should be assumed pollution-free. Preserve street connections through the industrial district so a utility parcel does not become a barrier to later expansion.

The treatment plant can be inland: the developer describes sewage purification with water returned to the freshwater network and pollutants collected as solid waste. This makes truck access and waste-processing headroom part of the treatment project. Verify actual output and added waste after commissioning. [Developer electricity and water explanation](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/electricity-water).

Reserve the plant's legal full upgrade envelope, access, pipe corridor and neighboring land for additional future capacity. Inspect attachment sides and upgrade limits in the UI before calling the reservation sufficient. A base building's dimensions alone do not establish its fully expanded footprint. Keep roads, zoning and other facilities out of that space. If an enduring site cannot fit, use a regular, accessible parcel suitable for later industrial/service reuse, preserving utility connections and accounting for any contamination before redevelopment. No promise that one facility will serve an arbitrarily large city.

### Live asset and affordability check

Read-only inspection on October 9, at paused frame 9462047, reported both base facilities unlocked:

| Asset | Base footprint | Construction cost |
| --- | --- | ---: |
| WastewaterTreatmentPlant01 | 96 x 80 m | 400,000 |
| RecyclingCenter01 | 176 x 144 m | 880,000 |

Available treatment upgrade prefabs: Extra Processing Unit (100,000) and Advanced Filtering System (50,000). Recycling upgrades: Storage Extension (150,000; reported 128 x 56 m) and Hazardous Waste Collection Point (200,000; 64 x 80 m). These are owner-attached assets, not independent facilities. Reported dimensions do not establish attachment layout, maximum number of upgrades or the combined footprint. Use normal building upgrade controls, not standalone placement of upgrade prefabs.

Both base facilities together cost 1,280,000, already exceeding the last verified 1,171,721 treasury by 108,279 before infrastructure or upkeep. With the 500,000 reserve, both would require at least 1,780,000 plus road/pipe costs and contingency. Therefore recycling is not presently affordable under the working guardrails. Reassess after construction and earned income; do not spend in anticipation of unearned recycling revenue or a milestone reward. The treatment plant alone would leave 771,721 before all other spending, so the whole project still needs a cost check.

The current prefab response does not expose full staffing costs, service output or upgrade limits. Inspect in-game upkeep/capacity, estimate wages, and check the settled budget after commissioning. Aim to retain the 100,000/month surplus; at minimum, avoid a structural loss and new debt. During landfill/recycling overlap, budget both facilities. The developer says recycling recovers manufacturing resources, but that is not a guaranteed profit estimate. [Developer garbage-service explanation](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies).

### Commission and retire in stages

1. Reserve utility parcels and upgrade envelopes before locking in the new freeway, interchange and collector geometry.
2. Build and connect treatment using normal unlocks and placement rules. Confirm road access, power, water/sewage connections, staffing, treatment operation and sufficient actual capacity before retiring the old sewage outlet. Inspect reclaimed-water output and the added garbage load.
3. If recycling becomes affordable, build it on the reserved parcel and verify collection, processing, storage trend, staffing and truck access. Confirm it can handle the city's waste stream, including treatment waste; assess whether the hazardous-waste upgrade is required instead of assuming it is or is not.
4. Keep the landfill operating until replacement service is established. Inspect the game's emptying/transfer controls and destination capacity; complete the normal retirement process before removal. Do not treat demolition of stored waste as successful replacement. If this cannot finish within the hour, report the transition as incomplete and keep safe collection capacity.
5. Reuse the old site only when its condition and the road plan permit. Leaving room for future industrial/service expansion is an acceptable fallback to a permanent utility site under the user's instruction.

## Alternatives considered

| Layout | Advantage | Limitation |
| --- | --- | --- |
| Westward mainline bend + one diamond + selective frontage access | Matches the user's alignment, reserves expansion, relatively simple to verify | Needs room for the bend and four ramps; crossroad queues must be checked |
| Continuous paired frontage roads with multiple ramp pairs | Distributes access along a developed corridor | Too much infrastructure and too little ramp spacing for the current roughly 1 km reach |
| Keep north-south mainline and add a western freeway branch | Preserves a potential future southern through route | More complex system junction; does not prioritize the requested mainline turn west |

The first option is the working recommendation. If the available land cannot fit it smoothly, simplify access or stage construction rather than squeezing another sharp loop into the footprint. Do not quietly purchase land to rescue a poorly fitted design; surface the measured constraint first.

## Bridge geometry rule

`cs2_connect_road` accepts one quadratic control point, then converts it to cubic form. In [RequestHandlers.Roads.cs](../../CS2MCP.Bridge/RequestHandlers.Roads.cs), the cubic controls are `A + 2/3(C-A)` and `D + 2/3(C-D)`. The curve generally does not pass through C. Its starting direction is C-A and ending direction D-C. The MCP wording "through" the control point is misleading (B009).

For a smooth join at P, align P-C_previous with C_next-P in the same direction. One quadratic cannot express every desirable ramp shape; split a compound curve into deliberately aligned pieces. Offset carriageways must be checked for a consistent gap through curves: copying coordinates with a fixed x/z shift is not a true constant-distance offset. Snapping can alter endpoints, and elevation at a snapped endpoint follows the joined network. Verify the built result, not just the requested coordinates.

## Challenge four: priorities and acceptance

Challenge four ran October 9, 2026, with the real-time window 15:24:25–16:24:25 UTC. The researched acceptance criteria below are preserved; see [SESSION.md](SESSION.md) for measured outcomes and limitations.

- **Primary:** coherent westward freeway corridor, smooth connections, four usable interchange movements and room for extension. Inspect both overhead and driver-level views; watch trucks negotiate ramps and check lane continuity.
- **Secondary objective:** commission wastewater treatment on a surveyed parcel reserved for its full upgrade envelope and future utility capacity; verify operation before retiring the old outlet.
- **Conditional objective:** replace landfill service with recycling only when capital, staffed upkeep, waste capacity and a safe transition are affordable. Reserve the site even if construction is deferred.
- **Secondary:** industrial and town collectors feed access without trapping local traffic at ramp mouths. Revalidate buses, service vehicles and utilities after road replacement.
- **Economic guardrails:** aim to retain at least 500,000 cash, no debt and positive recurring balance; carry forward the 100,000/month surplus target if affordable. Starting values must be refreshed. Avoid growth zoning while evaluating the road experiment.
- **Traffic evidence:** record comparable daytime observations of the original entrance, industrial collector and shopping spine, plus ramp volumes and queue heads. Where roads are replaced, map by position and role rather than assuming entity IDs persist. Target improvement in persistent queues and forced braking; 80% aggregate flow remains a secondary aspiration, not the sole pass condition.
- **Capacity guardrail:** inspect landfill storage before a long observation run. Last observed 121/151 tonnes and approximately 129 tonnes/month processing; these are historical, not a current guarantee.
- **Finish:** leave time to observe and correct the completed layout, pause at the real deadline, verify the final save and record both successes and remaining defects. Allocate time by progress rather than rigid blocks.

Short-term speed/queue improvement will remain a hypothesis until observed. Low current ramp usage cannot demonstrate high-volume capacity; future growth needs another check.

## Current utility parcel reservation

The built treatment plant is centered at(-1534,1200), facing its north-south access street atx=-1482. Keep the unzoned land west and south of it open for normal attached upgrades; no full attachment-envelope guarantee has been established. A provisional recycling parcel lies east of that street between roughlyx=-1470 and-1180,z=1080–1280. The base176x144m recycling footprint appears to fit with the road on its west side, but attachments, frontage, terrain and access must be validated before purchase. Do not fill this land with growable industry. Reuse of this service precinct for later industrial expansion remains the accepted fallback if much larger treatment needs eventually require relocation.

The current landfill and treatment share the industrial street network. Do not confuse that arrangement with unlimited processing capacity: treatment raised the solid-waste stream. The user manually enlarged the landfill after automated area dragging failed. Capacity was verified at 353 tonnes, up from 151; the resulting tapered polygon differs from the initially proposed narrow 75 m extension. This is an interim storage measure, not a processing upgrade. Recycling remains the preferred funded replacement, subject to actual staffed costs and a normal landfill emptying transition.
