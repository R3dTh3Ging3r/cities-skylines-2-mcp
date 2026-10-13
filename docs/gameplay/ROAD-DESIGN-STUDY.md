# Road design study — Ezra City

Researched October 9, 2026. Research only: no city mutations or new traffic experiments. Read alongside the [current brief](EXPANSION-BRIEF.md) and [learning corrections](LEARNING.md#l028-connectivity-was-an-incomplete-design-standard).

## Evidence and limits

The last locally recorded game version is 1.6.2f1; it was not refreshed today. The developer's 2023 feature diaries document intended mechanics, not current balance or a guarantee of behavior in this save. The [1.6.2f1 Steam announcement](https://store.steampowered.com/news/app/949230/view/686390624621953962) was reachable, but its patch body was not readable in this research tool. No new patch-specific traffic claims are made here. Road-agency guidance supplies design principles, not CS2 capacity formulas or mandatory game dimensions. Recommendations below remain candidates until surveyed and tested.

## What CS2's tools actually support

The official road-tool guide describes parallel construction with adjustable separation, curves, snapping, slope guides, and elevated/cut roads. It also describes premade intersections and highway-to-street transitions. A merge can use a highway section widened by one lane, with a one-lane ramp joined to the extra lane. Signals, crossings, turn restrictions, roundabouts, sidewalks and sound barriers are available tools. These capabilities justify using a deliberate alignment and inspecting the native preview; they do not prove our bridge exposes every operation. [Developer road tools](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/road-tools).

In our bridge, `cx/cz` is a quadratic Bezier control point, not a point the road passes through. For endpoints A, B and control C, endpoint tangent directions are C−A and B−C. Select C from compatible tangent lines; if those directions cannot produce a suitable single curve, use another geometry approach. Native terrain adjustment and snapping can change the result. This follows the [local implementation](../../CS2MCP.Bridge/RequestHandlers.Roads.cs), not a claim about the manual curve tool. See [B009](BRIDGE-NOTES.md#b009--curve-description-suggests-a-pass-through-point).

## Choose the right kind of junction

A service interchange joins a freeway to surface streets; a system interchange joins freeways. A diamond uses four directional ramps and two surface terminals. Its terminals need storage and suitable controls. Compressing the footprint trades land savings against terminal interactions. A full cloverleaf adds loops and weaving. Selection should consider terrain, movements, neighboring ramps and developable land, not appearance alone. [FHWA interchange design guidance](https://highways.dot.gov/field-offices/missouri/interchange-design-promptlist).

Our provisional comparison for the Beech corridor:

| Candidate | Why test it here | What would disqualify it |
| --- | --- | --- |
| Compact conventional diamond | First candidate: one east–west collector serves both neighborhoods, with a legible four-ramp arrangement | Insufficient terminal storage, poor ramp spacing from the northern junction, or excessive waterfront displacement |
| Diamond with two roundabout terminals, often called a dumbbell | Alternative if the native preview fits and turning movements suit it | The circles consume useful blocks, queues reach the bridge/freeway, or dominant flow blocks an approach |
| Offset/asymmetric diamond | Candidate if moving a terminal protects an existing block or service | Kinked ramps, confusing street continuation, or unequal access that overloads one side |
| Looped or larger bespoke junction | Hold for a demonstrated constraint that simpler candidates cannot solve | Current small-city need does not justify sacrificing the district footprint |

This is a test order, not a selected interchange or a promised capacity ranking. A diverging diamond or single-point interchange should not be assumed practical without verifying lane connections and control support in the installed tools.

One full-access interchange can serve both sides: west district ↔ cross-town collector ↔ east district, with ramps to and from both freeway directions. It need not be two independent interchanges. A separate crossing without ramps can carry local traffic elsewhere along the corridor.

## Geometry before construction

Our earlier failures were visible even when reachability tests passed. Apply these project checks before calling a road finished:

1. **Fit the whole envelope.** Draw both carriageways, all ramps, terminal junctions, bridge approaches and neighboring blocks together. Account for buildings displaced, utility routes and land left usable. A thin centerline sketch is insufficient.
2. **Keep a coherent mainline.** Maintain visually consistent separation and a clear through path. Avoid a lane drop that forces through traffic into an exit. Leave future southern ends aligned; unfinished stubs must not be the only available route.
3. **Make joins tangent.** Reject visible elbows, hook-shaped recoveries and tiny reverse curves. Inspect from above and from an oblique driver's view. Measure or preview the entire curve, not just its endpoints.
4. **Separate decisions.** Mark the northern interchange's last merge, the proposed next exit, and the first surface-street junction beyond each terminal. Do not squeeze them together to make a preset fit. There is no verified universal minimum spacing for our CS2 save.
5. **Give queues somewhere to wait.** Keep driveways, bus stops, parking entrances and the first neighborhood junction outside the ramp-terminal storage area. Use observed peak queue tails to judge that space later.
6. **Check the vertical profile.** Preview clearance, grade and supports; reject a sudden hump or a support blocking a road. Compare surface freeway with local overpass against a short elevated freeway section. A long elevated corridor is not the default merely because it avoids one crossing.
7. **Inspect controls and lanes.** Verify turn paths, allowed directions and automatic lights. Prior success removing a light at a simple taper is not a reason to remove controls at a genuine crossing.
8. **Judge motion as well as shape.** Braking at a yield or signal can be correct. Repeated near-stops on a supposedly free-flowing curve need investigation. A screenshot cannot prove traffic capacity.

## Feeders, collectors and neighborhood streets

Frontage roads run alongside a freeway and can connect ramps to cross streets. Access near ramp connections needs protection. [TxDOT frontage-road guidance](https://www.txdot.gov/manuals/des/acm/chapter-2--access-management-standards/section-3--number--location--and-spacing-of-access/frontage-roads.html).

For this city, use that access-management principle without automatically adding continuous frontage roads on both sides. The waterfront strip is narrow. A useful north–south neighborhood collector set back from the ramps may distribute trips with less land consumption. Reserve frontage roads for places where they solve a measured access problem.

Proposed hierarchy: freeway → interchange → district collector → local street → property. Walking connections may take more direct routes. Keep truck access to the northern industrial district attractive; do not make the western housing district the freight shortcut. Preserve useful east–west local crossings so the freeway does not become the only way between schools, shops and homes.

## Density, services and travel

The developer describes route choice as a combination of time, comfort, money and behavior. Parking availability and price also affect travel choices. Roundabout entrants yield to circulating traffic. Therefore a roundabout, more lanes or a parallel street cannot be assumed to fix the bottleneck without observing usage. [Traffic AI](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/traffic-ai).

Mixed-use zoning combines medium residential and commercial demand. It is a useful option, not justification for a continuous retail wall. [Zones and signature buildings](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings).

For the east, propose connected medium-density blocks first, then selected high-density clusters near useful transit and services. Preserve a continuous waterfront walking route. Place neighborhood shops at accessible nodes and inspect occupancy before extending them. Keep parking access off ramp throats; reserve sensible parking sites without consuming the entire waterfront for cars. Density should be judged by access, occupancy, service load and queues, not how many tall buildings fit.

For the west, keep low density predominant and use row/medium housing around neighborhood centers and collectors. Put school entrances on calmer connected streets, with walking access from both sides of their catchment. Avoid using the school frontage as the primary freeway feeder.

Services have local and simulated effects; an apparent coverage area does not replace travel access. School demand depends on eligible students and education progression. Service upgrades can add capacity, and some sub-buildings require additional land. [City services](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies).

Reserve elementary, high-school and possible college sites with actual asset and upgrade footprints. A college reservation is not a purchase commitment. Compare existing police/fire/health capacity and response routes before deciding whether to relocate, expand or add buildings. Keep optional capital and future staffed costs separate.

## Next planning pass and acceptance test

Before selecting a location, refresh the road graph, elevation/terrain, owned boundaries, buildings, pollution, utilities, school demand, service capacity, traffic at comparable times, cash and hourly-updated monthly balance. Compare at least two complete footprint sketches on the same scale. Highland and Arborview are recognizable candidate cross-town corridors in the screenshot, not approved interchange locations; Juniper's proximity to the northern junction needs particular scrutiny.

For later authorized construction: verify a separately named checkpoint; preserve a working route during the cutover; build sequentially; inspect actual topology after each significant step. Test west/east to north/south and return movements, east↔west local travel, industrial freight access, service access, and walking routes. Keep final geometry screenshots and raw telemetry in `.local/`.

Test comparable peak and quieter periods using the same road cohort. Record queue origins/tails, repeated stops on free-flow ramps, lane use, route use and service trips. Repeat after neighborhood growth. Citywide flow alone, a quiet-night screenshot, and a graph path are each insufficient. Acceptance requires functional access, acceptable geometry, useful remaining building land and affordable operation; report untested growth capacity explicitly.
