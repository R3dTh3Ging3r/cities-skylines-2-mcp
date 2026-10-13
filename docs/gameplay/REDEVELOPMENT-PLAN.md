# Ezra City — Beech freeway and residential redevelopment

Design recommendation, October 9, 2026. Requested by the user while the city continues earning. **Planning only; no construction, demolition, purchase, save, tax change or simulation control performed.** The user subsequently clarified that clearing the required footprint and building the replacement infrastructure should happen in one paused pass. That is the construction sequence below.

## Recommended design

Extend the divided freeway south on the Beech corridor. Place a compact four-ramp diamond at Arborview, with Arborview remaining at ground level and a short freeway flyover above it. Keep Juniper as a separate local crossing without ramps. Retain the northern industry, recycling, treatment and crematorium. Reuse the elementary school and fire house where their access remains viable. Reserve a western high-school site and a larger future college site. Develop mostly low-density housing west of the freeway, and medium-density housing with selected taller clusters east of it.

This is a surveyed concept, not a native-tool construction preview. The proposed dimensions below are design reservations and starting geometry. Final road snaps, clearances, lane paths, demolition list and prices must be checked in the game before committing. The current bridge does not quote a complete interchange or report all school capacity/upgrade data.

## Survey and confidence

Read-only snapshots were taken while the user's city ran at speed 8; they are not simultaneous. The road-only query returned all 212 matching segments. An initial all-network query was truncated and was not used as a complete utility inventory. Building queries around the corridor were split until each returned every match. Terrain and pollution grids have 56 m cells: useful for site selection, insufficient for exact shorelines, pier clearances or property boundaries.

| Measurement | Survey result | Planning implication |
| --- | --- | --- |
| Owned tiles | 11; southern edge approximately z=-311.65 | Core concept fits existing ownership; later continuation needs another survey/purchase decision |
| Starting cash/population | 413,796 / 5,327 | Historical planning baseline; user continued playing afterward |
| Monthly income/expense/balance | 794,948 / 724,751 / +70,197 | One hourly-updated sample, not a guaranteed savings rate |
| Power consumption/production | 69,072 / 85,200, fully supplied | About 81% utilization; growth needs a capacity check |
| Freshwater consumption/capacity | 22,464 / 28,034 | About 80%; include expansion funding before a large influx |
| Sewage consumption/capacity | 22,464 / 212,000 | Substantial aggregate spare capacity; still verify local pipes |
| Garbage accumulation | 188,864 in bridge rate units | This endpoint does not establish recycling processing capacity or collection health |
| Residential building demand | Low/medium/high each 100 on the 0–255 scale | Do not read this as 100% or guarantee immediate refill |
| Commercial/industrial/office building demand | 21 / 0 / 0 | Avoid oversupplying retail or deleting job space without replacement |
| Terrain along Beech | Roughly 511.8–513.0 m over sampled southern corridor | Nearly level terrain favors a regular alignment |
| Southwest starter/high-school sites | Sampled ground and air pollution zero | Prefer these for initial western housing and school reservations |
| Former northwest industry | Ground-pollution cells remain positive, maximum 15,321 raw units | Delay sensitive infill until a fresh overlay supports it; no invented safety threshold |

The traffic sample showed 54% city flow across 208 roads, while a separate live vehicle snapshot near Beech showed 83 vehicles, two stopped by choice and no deadlock. These are different measures and not a matched peak test. Retain segment-based comparisons during construction validation.

## Corridor geometry and two compared options

Coordinates use world meters: x increases east, z increases north. Existing Beech is approximately x=-1138; Juniper z=846, Highland z=646, Arborview z=446. The concept preserves those recognizable axes where practical.

| Feature | A: compact diamond — recommended | B: roundabout terminals — alternative |
| --- | --- | --- |
| Crossroad | Arborview, z≈446 | Same crossroad for a fair comparison |
| Terminal centers | x≈-1268 and -1008; 260 m apart | x≈-1308 and -968; 340 m apart |
| Ramp noses, initial reservation | z≈766 and 126 | z≈806 and 86 |
| Conservative screening box | x[-1308,-968], z[126,766]: 340×640 m | x[-1348,-928], z[86,806]: 420×720 m |
| Box area | 21.76 ha | 30.24 ha; approximately 39% larger |
| Strength | Smaller reservation and a simple legible arrangement | Potentially useful terminal control if turning flows suit it |
| Main risk | Turning queues and terminal control need testing | Greater land consumption; circulating traffic can obstruct an approach |
| Reason to select | Best initial fit for the narrow eastern district | Choose only if native fitting and traffic testing justify the extra footprint |

These rectangles are conservative study boundaries, not paved areas or automatic bulldoze selections. Excluding the user's removed cemetery, a rotated-building bounding-box screen found 208 buildings intersecting the mainline-plus-A reservation and 301 for B. Those counts include buildings between ramps that might survive and exclude impacts outside the rectangles, such as crossroad modifications. They are **not exact demolition quantities, household counts or relocation costs**. Generate an exact affected-entity list from the accepted native preview.

Start with two through lanes each way. The queried highway prefab is 12 m wide; proposed carriageway centers near x=-1154 and -1122 give a 32 m separation and roughly 44 m total pavement envelope. Reserve a broader approximately 100 m corridor for margins, utility continuity and possible later changes, without clearing that entire width automatically. Use an auxiliary lane where appropriate, preserving the through lanes.

The northern junction has merge/diverge features near z≈1075–1230. Option A's northern ramp nose at z≈766 leaves approximately 309 m to the nearest of those sampled features; B leaves about 269 m. Those are longitudinal separations, not certified weaving lengths. Inspect final noses, auxiliary lanes and lane paths before accepting either arrangement. Reject a forced fit that recreates our earlier closely spaced maneuver problem.

Ramp curves should leave and rejoin parallel to the mainline and meet the crossroad cleanly. The diagram's smooth curves are intended geometry, not commands for the bridge's single quadratic control point. Use the native curve/preset tools or appropriately joined curve sections. Verify the built result visually and by lane connectivity.

## Vertical layout and local access

Keep Arborview at surface level to avoid consuming the narrow eastern district with a long rising approach. Lift the freeway over the crossing, with the ramp terminals remaining at ground level. Initial study allowance: an 80 m crossing zone and approaches roughly 200–240 m each, depending on the clearance the native tool requires. A nominal 10–12 m rise over those lengths is a geometric starting point around 5%, not a proven required deck height or an approved final slope. If the native structure cannot fit with smooth transitions and clear supports, revise the length or alignment before clearing buildings.

Juniper becomes a separate grade-separated local crossing. Compare a local-road overpass there with the actual northern freeway profile; reserve approach land and pedestrian continuity. Do not assume the existing roundabout and taper can remain once Beech becomes a through freeway.

Highland remains useful on both sides but should not become an at-grade freeway junction or acquire extra ramps inside the diamond. Reconnect its local ends through district collectors. Close or reroute incompatible crossings around z=334 and z=222 rather than permitting accidental intersections with ramps or rising freeway approaches.

Arborview's first full neighborhood junctions should be set away from the ramp terminals. Initial candidates are the western collector around x=-1514 and an eastern collector around x=-850 to -820. These provide roughly 246 m west and 158–188 m east from terminal to collector. Preserve queue storage and keep school gates, retail driveways and bus stops out of that space. The eastern spacing is a constraint to test, not a guarantee of adequate capacity.

Provide a **temporary southern boulevard terminus**, feeding both local districts, until the freeway can continue south. Study a paired-road taper finishing around z=-100, with the next surface junction near z=-240, inside the owned boundary. Keep the taper free of an unnecessary crossing signal and maintain separation from the terminal junction. The exact geometry is unresolved until native preview; if the remaining distance cannot accommodate an orderly transition after the south ramps, shorten/revise the first construction scope rather than squeeze it in. Keep the future freeway centerline clear. Do not add an outside connection at an internal tile boundary.

Reserve a later southern local crossing near z=0, but do not build it redundantly beside the temporary terminal. Review it when extending the freeway and removing the temporary taper.

## Neighborhood and service reservations

| Reservation | Proposed bounds or existing position | Intent and constraints |
| --- | --- | --- |
| Southwest first housing | Approximately x[-2130,-1500], z[-230,200], excluding school and road reservations | Clean sampled land; mostly low density, row/medium near useful local centers |
| High-school campus | x[-1930,-1690], z[-20,180], 240×200 m | Vacant in survey; allows comparison of small/larger school footprints, paths and expansion space |
| Future college | x[-2110,-1830], z[230,470], 280×240 m | Clean sampled ground; two office centers currently inside, so reserve through a later deliberate job-space adjustment |
| Western civic pocket | x[-1700,-1500], z[250,410], 200×160 m | Candidate clinic/police relocation area; some occupied property, exact plots and access must be fitted |
| Existing elementary | Center approximately (-1402,546), 72×48 m | Retain if access/expansion space remains viable; preserve school frontage off the ramp approach |
| Existing fire house | Center approximately (-1382,705), 40×40 m | Retain initially; test response paths to both sides before duplicating it |
| Eastern taller cluster | Approximately x[-940,-650], z[140,370], clipped to dry land and buffers | Wider southern waterfront area; medium density surrounding selected high-density buildings |
| Eastern local collector | Generally near existing x=-850, with local adjustment near Arborview | Maintain access north/south, preserve walking links and avoid continuous frontage-road sprawl |

Campus rectangles are land reservations, not verified full-upgrade envelopes. Native attachment previews must confirm expansions, parking/access and setbacks. The southwest housing outline includes the school reservation: it must be excluded from zoning.

Keep low density the majority of western residential land. Use row housing and medium density around local shops, schools and the collector, with connected small streets rather than every street feeding the freeway. Preserve profitable office/commercial pockets outside the required construction footprint until replacement demand warrants their redevelopment.

On the east, use medium-density blocks as the base, with high density clustered near Arborview and the wider southern shoreline. Maintain public walking access along the waterfront and short paths to useful bus stops. Start commercial/mixed-use at a few accessible nodes; current commercial demand and a No Customers warning do not support blanket retail zoning. Reuse and reroute the current bus service around the new road network before adding an expensive new mode.

The shoreline estimate is coarse: the eastern land narrows around Highland, with the last sampled dry positions near x=-736, and is wider near z=222–334. Keep the denser cluster in the wider area and verify the actual shoreline before drawing lots or paths.

## Service conflicts and priced assets

The user demolished Cemetery02 during planning. A refresh confirmed it absent and a Crematorium01 at approximately (-1885,1556) in the northern service area. A remote old-cemetery ruin in the search results is not an operating replacement. Remove cemetery relocation from this plan; still check crematorium operation and vehicle access before resuming after construction.

The clinic near (-1166,466), police station near (-1110,760), and water tower near (-1088,422) intersect or closely constrain the intended corridor. Prefer normal relocation where supported and economical. Keep the clinic and police on connected surface streets; the civic pocket is a candidate, not validated placement. Find a clean, connected utility site for the water tower and verify its supply after moving. Do not destroy utility capacity just because the citywide meter currently has spare capacity.

| Asset | Live construction price | Base footprint | Current status |
| --- | ---: | --- | --- |
| ElementarySchool02 | 40,000 | 72×48 m | Existing; unlocked; reuse first |
| ElementarySchool01 | 100,000 | 144×64 m | Unlocked alternative |
| ElementarySchool03 | 150,000 | 48×64 m | Unlocked compact-footprint alternative |
| HighSchool02 | 150,000 | 96×64 m | Unlocked first purchase candidate |
| HighSchool01 | 300,000 | 176×128 m | Unlocked alternative |
| HighSchool03 | 300,000 | 96×48 m | Unlocked alternative |
| College01 | 750,000 | 176×128 m | Locked; reserve land, defer purchase |

Price and footprint do not establish student capacity, staffed upkeep or educational suitability. The inspected existing elementary had 15 employees; the bridge did not expose enrollment/capacity. EducationCount is an education-level statistic, not a count of available school seats. Before choosing the high school, read eligible students, capacity, efficiency and staffed cost in the UI. Keep room for future east-side service provision without buying it until actual need justifies it.

## Paused build sequence

1. **Readiness and checkpoint.** Refresh money, current loans, hourly budget, utility capacity, services, buildings, road graph and unlocked assets. Price the accepted native layout. Verify a separately named completed save preserving all of the user's latest work. Pause and confirm the frame no longer advances.
2. **Mark the actual footprint.** Confirm road alignment, ramp noses, supports and terminal geometry. Inventory affected occupied buildings and expected housing/job loss. The screening rectangles are not the demolition list. Dezone exactly the land being repurposed so old uses do not regrow after unpause.
3. **Move conflicts and clear.** Relocate the clinic, police and water tower as required; clear buildings/roads needed for the accepted plan. Preserve northern utilities/industry, the crematorium and reusable western services. No need to keep live traffic moving during the paused cutover, but every occupied/service block retained must have a route before resuming.
4. **Build the road structure in one paused pass.** Extend the freeway, construct the diamond and flyover, provide the Juniper crossing, reconnect both district collectors and build the viable temporary southern terminal. Build the planned local streets where funded. Preserve college/service sites and the next extension corridor.
5. **Restore networks and travel.** Trace water/sewage and electricity across every removed road. Highways must not be assumed to replace Beech's utility distribution. Provide explicit network connections as needed. Repair transit routes/stops and pedestrian links. Test north↔west, north↔east, south-terminal↔both neighborhoods, east↔west and northern freight/service routes in both directions.
6. **Provide replacement zoning and services.** Zone enough appropriate residential and employment frontage to replace the removed uses, adjusted for different densities. Do not equate equal zoned area with equal households. Reuse the elementary/fire house; build the chosen high school only with verified need and operating budget. Keep growth-only extensions, college and optional additional services staged.
7. **Pre-resume inspection.** Check lane paths, controls, slopes, junction spacing, remaining zoning, utilities and all service access; inspect from overhead and oblique views. Record actual spending and remaining reserve. Verify a new completed save while paused.
8. **Recovery after unpause.** Observe initial connectivity and utility service, then successive hourly budgets, occupancy, employment, garbage, deathcare and traffic. Compare peaks with peaks using a fixed road cohort. Build beyond the replacement scope only when occupancy, finances and services support it. Keep unexpected problems as measured notes rather than claiming the new design is proven immediately.

Construction while paused incurs capital costs but no simulation-time operating losses. Cash risk begins when the cleared city resumes. This corrects any implication that money is lost merely because the player spends a long time designing roads while paused.

## Funding and stopping rules

Keep **3 million** as the conservative provisional starting target for this scope; **5 million** remains the more comfortable allowance if clearing most occupied neighborhoods before regrowth. These are cash targets, not instructions to spend them all. The actual clearance list may reduce or increase the required reserve.

An illustrative 3 million allocation is 1 million capital, 1.5 million recovery reserve and 0.5 million contingency. Within the 1 million capital allowance, a 150,000 high school leaves 850,000 for roads, crossings, utility connections, normal service moves and other required works. The bridge exposes building prices but not a complete network construction quote; the 850,000 is unpriced headroom, **not a bill of quantities or affordability proof**. No loan, new tile or college purchase is assumed.

The prior live tax-loss stress test gives approximately 202,000/month loss if half of residential/commercial/office taxes disappear, or 479,000/month if all disappear while other costs/income stay fixed. A 1.5 million recovery fund covers approximately 7.4 or 3.1 game-months respectively. Those losses occur only after unpause, and actual recovery can be faster or slower. Industry and fee income may also change. Recalculate using the actual displaced population/jobs and current service spending.

Do not begin clearance if the priced capital plan plus the selected recovery allowance and contingency exceed available cash. Reduce optional streets/amenities or continue saving rather than quietly spending the recovery reserve. If a school or utility expansion makes recurring costs exceed the recovered city's plausible income, revise the operating plan before buying it.

During recovery, inspect the situation before discretionary spending if treasury reaches 1.5 million; pause and reassess if it approaches the 0.5 million contingency or essential services fail. These are planning decision points, not an automation or unattended monitoring commitment.

## Acceptance and unresolved checks

The plan is ready for layout review. Construction remains conditional on native preview and a fresh checkpoint. Specifically unresolved: exact native ramp/lane geometry, supported bridge clearances and piers, temporary southern taper fit, current school enrollment/eligible students, service move prices/attachment space, recycling and crematorium utilization, precise utility-network cutover, and the final construction quote.

Completion of the later build means all intended movements work, retained buildings have road/utility access, no forced stops occur on nominally free-flow ramp curves, terminal queues stay contained under observed traffic, and the city can recover without exhausting its reserve. A quiet sample does not prove future high-volume capacity.

Local review diagrams: `.local/p1/concept.png` and `.local/p1/options.png`. Raw evidence and the renderer remain ignored under `.local/`. This Markdown preserves the portable design decisions and dimensions. Source principles are linked in [ROAD-DESIGN-STUDY.md](ROAD-DESIGN-STUDY.md); later evidence belongs in [LEARNING.md](LEARNING.md).
