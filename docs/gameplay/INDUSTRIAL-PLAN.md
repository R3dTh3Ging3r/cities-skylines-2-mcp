# Northern industrial district — plan and construction record

## Latest verified update — October 10, special factories and zoning

This section supersedes older construction-status statements below. The original reservations remain historical design guidance, not current empty plots. The user has since completed/revised rail and other facilities; resurvey before changing those networks.

Six available signature factories now occupy C/D/E: Vehicle Factory01 at(-3150,3152), Chemical Plant05 at(-3650,3210), signatures08/02/03 at(-4436,3088),(-4280,3048),(-4128,3056), and Oil Refinery09 at(-3890,3228). Prefix is IndustrialManufacturingSignature. A470 m local closure along Z3100, X-3570…-4040, links D to E; all six frontages connect bidirectionally to the collector. Signatures04/06/07 are still locked. Keep additional unlocked-factory siting subject to a fresh footprint survey.

P already has its local grid and now has343 additional industrial cells on local frontage. O's restored local loop now has76 low-office and77 high-office cells south of its northern street. Avenues remain excluded. N was already occupied industry. Preserve the western O service/queue reserve, freight tracks, airport approaches and utility expansion plots. Road and park maintenance depots are east of C on Castlewood Street at(-2844,3076) and(-2852,3226).

Construction/readback was paused; the user resumed during the final save. **Ezra-Northern-Factories-Zoned** is verified. See [SESSION.md](SESSION.md) and [L054](LEARNING.md#l054-predetermined-zoning-batches-need-spatial-exclusions-and-resumable-receipts). Factory operation, post-growth traffic and utility margin still require observation.

## Historical initial road phase and concept

October 10, 2026. Status: approved road phase built and saved as **Ezra-Industrial-Roads**, except the short freeway connection the user offered to finish. The user selected a regular airport, approved the supporting reservations, and wants solar/batteries reserved until power is needed. They requested no separate pollution analysis, reporting northward wind. This is the current industrial brief; older freeway/campus plans do not authorize unrelated construction.

Construction result: northern collector, airport avenue, four production compounds, first manufacturing grid N, Factory E frontage loop, and utility access loop are built. A native medium roundabout sits at (-2655,3520). Its southern avenue stops at (-2655,3285), about40m short of the existing divided freeway ends; the user explicitly offered to complete that connection. P remains the planned later manufacturing phase. Airport/cargo/rail/service/power assets, exact entrance streets, zoning and facility upgrades remain future work. See [L046](LEARNING.md#l046-industrial-road-construction-needs-native-validation-and-a-recoverable-tool-state).

As-built corrections: the collector uses a shallow smooth rise to Z3410 around the pylon near (-4310,3365), inside the owned tile ending at Z3428.174. Factory E's loop moved north/east and has a chamfered northeast corner, avoiding an existing ramp, highway and high-voltage line. Its revised clear reservation is370×200m; C retains the largest-factory space. E has one external entrance; the production grid, manufacturing grid and utility loop have alternate accesses. The separately named final save is verified; construction remained paused except brief automatic running after reload/restart. Traffic performance has not been tested.

Local review diagram: `.local/ip-road-concept.png`. Geometry and checks: `.local/ip-concept.json`, `.local/ip-concept-screen.json`. Those local files are supporting evidence; the portable decisions are recorded below.

## Agreed scope

- Finish the existing north-pointing freeway as an industrial/logistics gateway.
- Build coherent freight collectors and local industrial streets, with large rectangular compounds for stone and livestock. Preserve the current oil fields and operating services.
- Keep ore and coal secondary, with exact resource siting checked before building them.
- Reserve a regular airport with cargo capability, cargo railway terminal, railway approaches, and a maintenance yard.
- Provide ordinary industrial zoning blocks and protect larger plots for signature factories.
- Reserve solar generation, battery storage, high-voltage connections, another recycling facility and another treatment facility, with upgrade room.
- Include truck queue space, alternate road access, worker transit and walking access, fire/service plots, parking, utility corridors and later expansion.
- Roads are the construction priority. The user permits placing a necessary building first when that is needed to establish its entrance or appearance. This is not a request to buy every reserved asset now.

## Recommended arrangement

Use the existing oil fields as the district's centre. Four new production compounds sit to their west: two stone sites to the north and two livestock sites to the south. A central east-west collector serves the group; local perimeter streets provide building frontages and return routes. Main building entrances should face those local streets, away from the collector intersections.

Extend the north-pointing freeway roughly 270 m to a deliberate terminal south of the lake. The diagram shows a roundabout as the first candidate, with a west-east freight collector connecting the western avenue, existing northern highway spurs and airport avenue. Preview a smooth divided-road transition, junction controls and lane continuity before selecting its final geometry. The circle on the map is a concept symbol, not a ready-to-build junction or a capacity claim.

Place the freight terminal west of this gateway, the regular airport northeast with its long axis north-south, and ordinary manufacturing blocks east of the gateway. Keep substantial factory plots north of the existing east-west freeway. Unassigned land east of the airport remains available for further industrial/factory expansion; its exact subdivision is deferred until the airport approach restrictions are visible.

The power reserve sits along the western avenue. Additional recycling and treatment sites sit north of the current service area. Existing facilities remain in use.

## Protected parcels

Coordinates are world X/Z at the southwest corner; dimensions are east-west × north-south metres. Rectangles include planning allowances, not verified native placement envelopes. Do not subdivide a reserved facility parcel before checking its actual entrances and upgrades.

| Map | Use | X | Z | Reservation |
| --- | --- | ---: | ---: | --- |
| A | Regular airport, cargo upgrade and access | -1770 | 3060 | 640 × 1400 |
| B | Cargo terminal and storage | -3500 | 3450 | 400 × 320 |
| C | Largest signature factory plus smaller plots | -3390 | 3010 | 520 × 350 |
| D | Further signature factory plot | -3870 | 3060 | 340 × 280 |
| E | Smaller signature factories / flexible manufacturing | -4490 | 3020 | 370 × 200 |
| F | Livestock compound 1 | -4610 | 1660 | 490 × 425 |
| G | Livestock compound 2 | -4080 | 1660 | 510 × 425 |
| H | Stone compound 1 | -4610 | 2130 | 490 × 490 |
| I | Stone compound 2 | -4080 | 2130 | 510 × 490 |
| J | Solar, two possible battery stations and expansion | -5220 | 1560 | 460 × 1100 |
| K | Secondary ore/coal candidate | -5220 | 2870 | 430 × 380 |
| L | Additional recycling and upgrades | -2450 | 2210 | 400 × 320 |
| M | Additional wastewater treatment and upgrades | -1850 | 2210 | 400 × 320 |
| N | First ordinary-industry blocks | -2470 | 3070 | 410 × 340 |
| O | Worker/service hub | -3050 | 3540 | 260 × 150 |
| P | Later ordinary-industry blocks | -2470 | 3580 | 410 × 270 |
| Q | Compact rail yard and extension | -4000 | 3710 | 400 × 340 |

Terrain screening sampled all 17 parcels as dry and owned. Source terrain resolution is 56 m; repeating samples at 28 m is not a higher-resolution survey. Airport ground heights ranged about 509.4–514.1 m. Native grading, shorelines, collisions, height restrictions and connectors still require a closer check. Parcel Q intentionally includes part of the railway approach area; track and yard geometry must be designed together.

## Roads and access

The hierarchy is freeway → freight collector → local frontage → facility entrance. Keep ordinary zoning off the main freight approach and leave its junctions uncluttered. Use medium roads for the principal collectors, with wider-road space protected where a later capacity review warrants it. Use small roads for compounds and local frontage; extra lanes everywhere would consume land without proving better operation.

- The northern freight collector links the existing western avenue near X=-4725, the highway spur near X=-3447, the new terminal near (-2655,3520), and the airport avenue near X=-1945. Highway spurs require deliberate transitions before local accesses.
- The four stone/livestock compounds share the cross-collector near Z=2105. Their perimeter closes at approximately X=-4630/-3550 and Z=1640/2640, with a central local street at X=-4100. Keep field boundaries and building gates clear of these roads.
- Factory E receives its own local loop. C and D retain broad frontage; their short dotted access marks are placeholders until native factory previews establish the necessary streets.
- Airport access remains on a separate parallel avenue, with room between it and the airport for later passenger/cargo entrance roads. The southern dogleg is only a circulation reservation, not a verified gate position. Keep passenger stopping/parking out of the truck queue.
- The terminal's approximately 550 m parallel frontage is protected for future truck circulation. Its short end connections are not themselves sufficient queue-storage lanes. Gate positions, one-way direction and usable queue length must be established with the native building selected.
- N is the first manufacturing street phase; dashed streets around P are a later extension. The north edge stops near Z=3870 after the coarse terrain survey found water close to the earlier Z=3930 proposal.
- For L/M, reuse the existing southern avenue and central north-south road. The proposed small-road outer loop uses a western dogleg from (-2360,2105.2) through (-2360,2180), (-2470,2180), (-2470,2560), then east to (-1390,2560). Its east leg returns through (-1390,2180), (-1476.207,2180) to the existing intersection near (-1476.207,2114). This avoids the ramp conflict in the first draft and preserves the surveyed turbines. Smooth the corners and snap to freshly measured street geometry during construction.

Provide bus stops on the service/local streets, safe crossings and walking links toward the worker hub and existing city. Reserve small fire/service and parking sites in O; it is not a claim that every large service building fits in that one parcel. Additional service sites can use nearby unassigned frontage when actual coverage warrants them. Carry water/sewage and electricity corridors through the collectors; preserve a high-voltage route to the power reserve.

## Freight railway is a separate expansion

The global network query returned 64 existing train-track segments, all south/west of this district. The nearest sampled endpoint to the proposed freight area is near (-2213,-1719), roughly 5.4 km away in a straight line. That network connects to both outside rail endpoints. There is no adjacent railway to attach with a short spur.

Reserve an approximately 80 m corridor along the terminal's north side and toward the west/southwest. The concept follows approximately (-2810,3740) → (-3750,3740) → (-3920,3510) → (-3930,3290) → (-5260,3300). These are corridor markers, not rail curve control points. The long outside connection needs a separate terrain, ownership, bridge and cost survey. Preserve space for grade separation where the corridor crosses the new collector. Yard service-road crossing and turnout geometry are also unresolved.

No rail yard exists in the current inventory. Q reserves a compact yard plus expansion; do not consume its approaches with factories. Cargo terminals were locked in the survey, while rail-yard prefabs were unlocked. Check development unlocks again before buying anything.

Official mechanics: cargo terminals generate truck traffic and require cargo lines to receive trains; city-operated trains also use a rail yard. A regular airport needs its cargo upgrade for air freight, and native placement displays restricted-height approach zones. See [Public & Cargo Transportation](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/public-cargo-transportation). Build road access now when authorized; commission freight systems later as complete networks.

## Asset checks behind the reservations

| Asset | Surveyed base footprint | Planning implication |
| --- | --- | --- |
| Airport01 | 1136 × 376 m | Reserve the long parcel first; cargo-upgrade footprint is not exposed by the current query |
| CargoTrainTerminal01 | 304 × 136 m | Add storage, entrance circulation and track approaches; warehouse is 128 × 96 m |
| RailYard02 | 272 × 120 m | Extra Rails is 272 × 96 m; native owner-side placement still needs checking |
| SolarPowerStation01 | 288 × 552 m | Leave upgrade, transformer and battery room |
| EmergencyBatteryStation01 | 176 × 80 m | Two possible stations reserved; quantity ultimately depends on measured overnight load |
| RecyclingCenter01 | 176 × 144 m | Keep 64 × 80 hazardous-waste and 128 × 56 storage extensions in mind |
| WastewaterTreatmentPlant01 | 96 × 80 m | Known processing/filter extensions are each 96 × 80 m |
| Largest surveyed signature factory | 288 × 288 m | C protects a plot exceeding 320 × 320 m before roads divide it |

Nine industrial signature prefabs were surveyed. Three broad reservations are flexible land banks, not proof that all nine factories and their entrances have already been packed into a final design. Keep northeast expansion land available until unlock requirements and actual selected factories are known. Exact signature unlock conditions were not exposed by the bridge.

Stone and livestock do not require a resource deposit; coal and ore use ore deposits. Western tile totals identify a useful ore-search area but do not locate a precise extraction footprint. See [Economy & Production](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/economy-production). A small hub footprint does not represent the whole production field; the large compound reservations are intentional.

## Power timing and construction sequence

The late-session reading showed consumption 345,123 against production 520,000 in bridge units, about 66%. Consumption was about 70.5% in the earlier sample. These are instantaneous readings, not a worst-weather or daily-load study. Reserve J now and reassess generation at the next major expansion or when repeated peak readings approach capacity. Do not wait for a blackout, but do not buy solar solely because its site is available.

Solar output varies with daylight/weather; batteries store surplus and cover deficits rather than producing energy. Size the eventual installation against both peak demand and overnight energy, and verify the high-voltage outside connection before counting export income. See [Electricity & Water](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/electricity-water).

Proposed order after the user reviews this layout:

1. Refresh the city survey and verify a separately named checkpoint. Pause for substantial road work, consistent with the user's construction preference.
2. Preview the airport, freight terminal/yard and largest factory footprints without purchasing them. Confirm fronts, approach zones and extension positions. Place a necessary base asset first only where the user's authorization applies and the actual layout requires it.
3. Build the main freight collector, smooth freeway terminal and airport approach avenue. Retain existing industrial access throughout.
4. Build the stone/livestock road compounds, first manufacturing streets, factory frontage and utility loop. Keep future facility interiors uncut.
5. Verify current road graph, directions, elevations and appearance; then save a separate completed-road checkpoint. Do not call the future rail/airport operational from road completion alone.
6. Add facility-specific access, rail, airport, power and utility assets in demand-funded phases. Check queues after growth before widening roads or removing junction controls.

Latest planning sample: population 40,872, cash 9,632,574, simulation running at selected speed 4. The earlier monthly-budget sample showed a 2,340,747 surplus; it was not refreshed alongside that treasury reading. No money was spent by the agent. Native road prices and full facility-upgrade costs remain to be quoted during construction; no unsupported total is presented here.

## October10 facilities and second road phase — as built, tracks pending

A airport, Q compact yard, and B cargo terminal are placed with dedicated road access; details/current checkpoint in [SESSION.md](SESSION.md). P's four blocks, C's perimeter U, K's north-shifted loop, and D's east frontage were built. D has a pylon obstruction; do not rely on its entire original rectangle. C preserves a400×320m screened core. No factory/resource zoning or industrial service buildings were added.

The earlier railway corridor is superseded: B requires its EAST external connector, so O shifts east to preserve a broad rail approach. O roads are still pending. The user purchased additional western land, requests curved rail along its outside edge with switches/holding provisions at the southern connection, and reserves the open middle for future personal planning. Both rail/O layouts remain proposed. No track or transport line is operational from this work. J remains a power reserve; latest consumption344,444 of520,000 production. See [L047](LEARNING.md#l047-facility-connectors-can-invalidate-a-previously-reserved-approach).

## Railway visual review supersedes the first rail layout

See [RAIL-REDESIGN.md](RAIL-REDESIGN.md) for the user's latest pink/green/orange/blue direction. Cargo was rotated west while preserving its upgrade, O's original plot and loop were restored, and B/Q roads were reconfigured. The outer mainline and two sidings are built, but the user rejected their local geometry and requested a larger industrial yard and underground departure. Q's fan is unfinished. Resolve the complete large-yard, power and portal layout before further rail construction; the old S-bend conflicts with the new yard candidate.
