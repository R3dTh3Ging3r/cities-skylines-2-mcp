# Learning journal

Use this journal to improve decisions from evidence. Append entries; mark superseded conclusions and point to their replacement. Do not turn a single successful outcome into a universal rule.

## How to learn from a session

1. Record a baseline with city, game/bridge version, settings, in-game time, wall time, and relevant metrics with units.
2. State the hypothesis and the expected result before the change. Choose a practical observation window.
3. Prefer one meaningful intervention at a time. If several changes are necessary, explicitly record the confounders.
4. Record what actually changed, including unexpected costs or failures. Separate construction success from the intended economic benefit.
5. Decide whether to keep, revise, or reject the hypothesis. Record confidence and circumstances where it may not apply.
6. Update the session state immediately after a meaningful outcome. Promote a useful decision rule to the playbook with this entry's ID, preferably after a repeat observation or strong independent support.

Do not run expensive experiments merely to improve the notes when the user's challenge has a deadline. Natural gameplay outcomes can provide evidence. Keep raw captures in ignored `.local/play/` paths and preserve a concise result here.

## Entry format

Use a short ID such as `L004`, followed by:

- **Context:** city, version, difficulty/mods, objective, and both clocks.
- **Baseline:** relevant values, units, collection time, and checkpoint.
- **Hypothesis:** the cause we suspect and expected result.
- **Action:** exact change and cost; entity IDs or coordinates if useful.
- **Observation window:** elapsed game time and wall time.
- **Outcome:** after-values and whether the expected result occurred.
- **Confounders:** migration, milestone rewards, weather, simultaneous changes, stale counters, or missing observations.
- **Conclusion and confidence:** source-confirmed, observed once, repeated, tentative, or contradicted.
- **Follow-up:** next check and any playbook change.

## L001 Bridge demand units differ from the tool description

**Date:** October 8, 2026. **Context:** bridge source at upstream base `f0894c1`; game 1.6.2f1.

**Evidence:** `mcp-server/src/index.ts` describes demand as 0–100. `CS2MCP.Bridge/RequestHandlers.cs`, in `GetDemand`, explicitly labels building demand 0–255 and warns that company counters can exceed 255 and values refresh only while simulation runs.

**Conclusion:** Source-confirmed mismatch. Do not call a raw value a percentage or diagnose frozen demand from paused readings. The playbook now specifies raw units and freshness checks. Live readings have not yet been compared with the UI.

## L002 Budget changes need an appropriate observation window

**Date:** October 8, 2026.

**Evidence:** `GetBudget` describes hourly-updated monthly rates with positive expense values. A quick successful request is not a new economic sample.

**Conclusion:** Source-confirmed reporting behavior. Record in-game timestamps and distinguish treasury change from recurring balance. The provisional 1–3 game-hour observation window is a strategy hypothesis, not a tested optimal interval.

## L003 Connection success is narrower than gameplay success

**Date:** October 8, 2026.

**Observed:** C# compilation completed with zero warnings/errors, installed hashes matched, and native Codex ping returned bridge 0.9.0 at MainMenu. State correctly showed no city loaded; city overview rejected the request.

**Conclusion:** Main-menu communication is verified. This does not yet establish save completion, placement, simulation controls, or city-management effectiveness. Keep those validation stages separate. No live city strategy experiments have been performed.
## L004 First neighborhood reaches a positive recurring reading

October 8, 2026, Ezra City, game 1.6.2f1 / bridge 0.9.0. Initial capital 1,000,000, no population. Connected street loop, wind turbine, water tower and remote sewage outlet; manufacturing separate from housing. Initial utility upkeep excessive relative to population. Reduced Electricity and Water & Sewage budgets from 100% to 50%; efficiency fell to 25%, but measured capacity remained sufficient. At API 13:33: population121, income21995/month, expenses17824/month, balance+4171/month, treasury898008. This is one positive recurring reading, not yet proven sustained. Treasury also includes milestone reward; it is not the profitability metric. Growth and zoning occurred alongside budget changes, so the effect is confounded. Follow-up: watch electricity margin and repeated budget updates.

## L005 Bridge execution lessons

See BRIDGE-NOTES.md for reproducible URL-encoding, construction-readiness, budget-sign and calendar issues. The 0.1-hour timed simulation test auto-paused exactly at frame7938010. A later long run was paused before its target, coincident with milestone progress; check actual state rather than assuming timed runs always finish uninterrupted. Separate save Ezra-Start.cok exists at23416990bytes. Pipe endpoint warning persists at sewage outlet; city-wide sewage capacity is nonzero and no household sewage warning observed, but aggregate capacity alone does not prove local connection.

## L006 Growing services while preserving recurring profit

At1,377 residents and4,218XP, monthly income210818, expenses122402, surplus88416; treasury798090. Added small cemetery, clinic, elementary school and landfill. Residential/industrial tax12%, others10%. Utility budgets stepped up as demand grew: electricity50->75->100%, water50->75%; health50%, garbage75%, education100%. A brief power shortage at consumption15777 versus15000 production was resolved by budget increase; later fulfilled consumption matched demand. Avoid keeping starter cuts after growth. Landfill UI confirmed stored garbage and151t capacity. City now has waterfront walking loops and one tree-lined road section. Industrial freight enters north, homes south/east, pollution-heavy landfill northwest.

Traffic baseline85%flow;20queued vehicles on main spine near civic/commercial junction. Added diagonal local connection fromindustrial node(-1310,846) to civic node(-1370,646); connectivity verified both ends. Effect not yet measured. No deadlocks in baseline. A rejected road link atz446 was blocked byOverlapExisting nearclinic and did not mutate; kept clinic and used other connected routes.

## L007 Costs, labor and final-stage restraint

At1,519 residents the latest budget was59284/month surplus after a second wind turbine and healthcare budget75%. Happiness60, health55; later62/56. More industrial growth reduced measured unemployment from14.26% to2.82% while uneducated vacancies persisted. Do not infer that all vacant jobs mean general unemployment. Restoring health efficiency coincided with higher health; this is one observation with migration/time confounders.

At1,564 residents the new firehouse was operationally budgeted at50%, with an updated monthly surplus29158. Its recurring cost is included in the budget, unlike the earlier62636reading. Defer police/transit and large parks until justified by need and margin. Big Town remainsunreached; actualmilestone3LargeVillage. Existing natural trees, street-tree upgrades, riverside paths and separation of industry/waste fromhomes provide the initial aesthetic structure; central reserved green space can become a formal park when unlocked.

After the diagonal road link, the flagged intersection bottleneck cleared on a subsequent reading; city-wideflow remained85%. Growth and different time-of-dayconfound attribution. Original highway approach still has the poorest flow; do not claim the connection solved alltraffic.


## L008 Final result and limits

Final paused state: population 1758, money 761670, income 167734/month, operating costs 150952/month, surplus 16782/month, no debt, XP 6133. Large Village achieved; Big Town not achieved. Happiness 52, health 56, unemployment 8.59%, no homelessness. Traffic flow ended at 79% despite the intermediate bottleneck clearing: do not generalize that intermediate success.

Late budget readings remained positive but varied from 29,158 to 7,271 before ending at 16,782. Industrial taxes declined, and staffing mismatch remained. The sustainable next step is diagnosis and consolidation, not more service commitments or indiscriminate industrial zoning. The next challenge should budget time for transport and economic stabilization. Deadline polling ran slightly past 30 real minutes; add an independent bridge-side real-time pause deadline in a future update.

## L009 Happiness diagnosis and targeted policing

Second challenge, October 8–9, 2026. Happiness fell to48 despite adequate aggregate utilities. UI factor panel identified crime risk -13, taxes -2, unreliable healthcare -2 and traffic -1. CrimeCount was0, while the police UI showed90% crime probability: risk and actual crime counts are different measures. Built Small Police Station (PoliceStation02) for100,000, initially75% budget then100%; staffed operating cost25,200/month at100%. UI risk later fell to19%, happiness rose to63, and surplus remained positive above59,000/month. Population growth, office jobs and time are confounders; this is one city experiment, not a universal optimal service threshold. Read the actual happiness causes before choosing a building.

## L010 Educated jobs reduce the observed mismatch

Before low-density office zoning, unemployed residents coexisted with mostly uneducated vacancies and no educated vacancies. After Grand Village unlocked Office Low, zoned183 cells along the new western loop near the school and shops. Employment readings subsequently showed unemployment4.93% and4.17%, down from roughly12%; office tax income appeared. Commercial expansion and changes in the labor pool also occurred, so the office effect is not isolated. Keep education-specific vacancy data in the decision loop rather than responding to industrial demand alone.

## L011 Verify local service operation and road outcomes

The sewage outlet UI showed 71,000 m3/month capacity and 16% usage at approximately 03:48 (UI September; API calendar remains incorrect), despite the lingering pipeline endpoint warning. This confirms observed operation, not that the pipe warning or its surface elevation is correct. Do not confuse aggregate capacity with utilization; inspect the building.

Traffic: entrance signal removal, northeast loop, a small tree roundabout and same-width asymmetric main-street replacements have not established an overall improvement. City flow readings79->81->79->78->77 while traffic volume rose. Original worst segment improved28->39%, but the next segment worsened after replacement. Road reconstruction can reset measurements; allow multiple refreshes and preserve uncertain or negative results. Do not claim the experiment succeeded from a nicer-looking junction.

## L012 Late observation changes the outcome

October 9, 2026, second challenge. Surplus exceeded 40,000/month over distinct hourly updates (83,508; 74,597; 114,677), ending at 142,444. Happiness ended at 64. However unemployment rebounded from 3.03% to 8.61% and finally 4.32%; total jobs fell from 1,047 to 965 between late samples while the adult population grew. Jobs recovered to 1,109 and unemployment ended below the target at 4.32%. The cause of the temporary job decline was not isolated. Treat office zoning as a promising intervention, not a demonstrated lasting fix. Final city traffic was 75%, below both the 85% goal and 79% baseline despite an improved original spine segment. Reserve observation time, stop residential growth when hiring falls behind, and report final values alongside intermediate improvements.

## L013 Traffic expansion needs a fixed comparison cohort

October 9, 2026, third challenge. Added a custom industrial highway gateway, outbound flyover and western bypass. New routes were graph-connected and later carried vehicles, but use was light: an observed inbound segment volume9, bypass6 and outbound span1. Aggregate roads averaged increased52->80->91. City flow initially71->78%, then declined to69% as measurements settled; the original shopping street remained near30%. New, lightly used roads confound the average, and new housing increased trips. Conclusion: construction and future expansion access succeeded; a citywide congestion improvement has not been established. Compare the original street cohort and direct route usage before claiming success (B007).

## L014 Verify transit prerequisites before committing the depot budget

Third challenge. Bus depot cost150000 construction and93000/month estimated upkeep at100% funding. Eight-stop line eventually created normally, with six buses and90 passengers aboard on an early check. Default placeholder stops failed to satisfy the game's stop-count prerequisite; one explicit NA_BusStop01 resolved normal line unlock (B008). Transportation budget reduced to75%, fare set to2. Whole-city surplus remained above100000/month in early subsequent readings, but staffing/tax trends still require observation. Conclusion: query fresh prerequisites after each dependency and inspect full operating costs before placing transit. One operational route does not yet prove congestion reduction or good route design; its6.36km length merits later optimization.

## L015 Services and modest infill after a growth surge

Third challenge. Initial western apartments/row houses helped grow population2578->over3200 but temporarily drove unemployment toabout12%. Additional office/commercial zoning and time coincided with recovery to2.05%,1.29%,1.30% and1.71% over later checks. Radio mast added after Tiny Town unlocked communications and the UI identified unreliable internet-2; cost25000 andlisted5000/month upkeep. Healthcare funding75->100 coincided with health56->59 and disappearance of the unreliable-healthcare penalty. Park/leisure and wealth changes also confound happiness. Added only151 further residential/mixed-use cells after the hiring recovery, on verified vacant frontage. Continue observing before further housing expansion.

## L016 Building-card upkeep excludes wages

Third challenge correction: the radio-mast construction card listed5000/month upkeep. Inspecting the operating building later showed5000 maintenance plus19500 wages =24500/month total,10/10 employees and115% efficiency. The full city budget already included the staffed cost and still showed216996/month surplus. Treat purchase-menu upkeep as maintenance unless wages are explicitly included; use the operating building or settled service-budget total to verify the actual commitment. This also reinforces the prefab-info request for both base upkeep and staffing estimates.

## L017 Inspect storage before a warning appears

Third challenge near its end, population about 3,680. No garbage warning was active, but the landfill UI showed 117/151 tonnes stored and 79 tonnes/month processing at 75% budget, against approximately 120 tonnes/month city accumulation. Raised Garbage Management to 125%. The operating building then showed 124% efficiency, 124 tonnes/month processing, 28/30 employees, and storage 116/151 tonnes. Its displayed cost was 63,000/month at that staffing; the city budget's first refreshed surplus remained 190,615/month. This is an early response, not proof of a permanently balanced waste system. Monitor storage trend and staffing; processing headroom is modest. Absence of a warning is weaker evidence than measured remaining storage and throughput.


L017 follow-up, same session: at UI 02:31 in December, landfill storage had risen again to 121/151 tonnes despite 129 tonnes/month processing, 30/30 staff and 66,375/month cost. This corrects any inference of a sustained decline from the first 116-tonne observation. Delivery timing or backlog is a hypothesis, not an established explanation. Continue monitoring before more growth.

## L018 One-hour result and remaining constraints

October 9, 2026. Independent helper paused at 14:32:41 UTC, the real one-hour deadline; bridge state confirmed paused at frame 9462047. Verified final save Ezra-60min. Population 2,578 -> 3,719; recurring surplus 130,139 -> 208,957/month; cash 842,957 -> 1,171,721; debt zero. Tiny Town reached and paid a one-time 125,000 reward. Happiness 64 -> 71, health 56 -> 59, unemployment 4.73% -> 0.91%. Final settings passed three separate hourly profitability checks of 190,615, 182,406 and 178,904/month before ending higher. This supports the challenge's profitability result, not unlimited growth.

City traffic ended 66%, down from 71% and below the 80% goal. A better individual entrance observation and connected new roads did not establish congestion relief. Final bus snapshot had six vehicles and 15 passengers aboard at API 05:18, versus 65–90 aboard in earlier daytime observations; do not compare time-of-day occupancy as a controlled experiment. Landfill storage and two MissingUneducatedWorkers warnings remain open constraints. Only one educated job vacancy remained despite 199 total vacancies. Further housing should wait for capacity and workforce diagnosis. Ten homeless citizens were recorded at the end.

## L019 User review exposes a geometry weakness

After challenge three, the user reported that the industrial ramp's sharp turn makes cars almost stop and criticized the gateway's appearance. Record this as a user observation; the agent has not independently quantified the speed loss. Prior connectivity checks missed this quality problem. Next experiment: a broad westward mainline bend, one planned service interchange and deliberate feeder/collector access, evaluated with truck movements and persistent queues as well as graph connectivity. Research and alternatives are in [FREEWAY-PLAN.md](FREEWAY-PLAN.md); no replacement was built during preparation.

Source inspection also established that the bridge uses a quadratic control point rather than a pass-through waypoint (B009). This mathematical finding is verified in code. It supports planning aligned curve tangents, but does not prove the cause of every existing sharp turn or the performance of an unbuilt replacement.

## L020 Utility expansion needs a combined capital and site check

Preparation only, October 9: the user added expandable wastewater treatment and recycling conditional on affordability. Live prefab inspection at the unchanged paused state reported treatment 400,000 (96 x 80 m base), recycling 880,000 (176 x 144 m base), both unlocked. Combined base cost 1,280,000 exceeds last verified cash 1,171,721 before infrastructure, wages or reserves. This rules out buying both immediately under the current reserve policy; it does not decide affordability later in the challenge. Upgrade prefabs exist, but attachment limits and total expansion footprint remain unverified.

Developer documentation links wastewater purification to solid-waste production. Hypothesis: shared industrial/service access and planned waste capacity will make treatment expansion easier. No facility has been built or tested in this preparation. Preserve the landfill until replacement collection and a normal stored-waste retirement process are verified. Detailed sources and next-run checks are in [FREEWAY-PLAN.md](FREEWAY-PLAN.md).

## L021 Compound curves still need post-build graph and visual checks

Fourth challenge, October 9, 2026. Built a divided westward freeway and a compact service interchange using tangent-aligned quadratic pieces. The first local-access trial produced bunched nodes and overlapping approaches; inspected construction receipts and rebuilt that trial before opening it. The surviving layout has separate ground ramp terminals and a grade-separated crossroad. Native snapping moved requested merge positions, so requested coordinates alone did not describe the final topology. Observation: active eastern ramps carried deliveries, buses and private cars. Western ramps remain future-facing because the mainline ends at the owned-tile edge. Do not equate four built ramp connections with four currently usable outside directions.

## L022 A morning queue and a later clear road are different observations

Fourth challenge. Early light traffic had no stopped vehicles near the new interchange. At08:18 the industrial approach had19 vehicles waiting at a signal and7 farther upstream. Removed the verified three-way junction's light; the next snapshot showed a29-vehicle queue shifted onto Arborview Street. A later sample cleared to2 stopped vehicles, then0 at midday and1 non-traffic work vehicle in the afternoon. The unused western stub was subsequently removed and the former highway feeder converted to a four-lane city collector; its redundant light disappeared. At20:32, Arborview's original segment79135 measured46% flow versus34% at baseline, but the shopping segment79130 was35% versus34%. Different times and several related changes prevent attributing the result to one change. Preserve morning-peak observations and original-road comparisons; do not sell a quiet evening as proof of rush-hour capacity.

## L023 Treatment trades water pollution for operating expense and solid waste

Fourth challenge. Normal placement of WastewaterTreatmentPlant01 cost400000 on the former highway approach. The live building reached50/50 staff and119625/month total cost at75% Water & Sewage funding, with45000 maintenance and74625 wages. It showed actual sewage usage and0% pollution in reclaimed water. Its50% purification figure is distinct from output pollution; do not describe it as50% contaminated output. The old outlet was disabled normally after replacement operation was observed; standby maintenance750/month versus7500 active.

After the outlet was disabled, sewage capacity remained312000–316000 against about13200 consumed, and garbage accumulation rose to146832–147936kg/month. The landfill at125% funding processed129t/month. Raising garbage funding to150% produced137t/month, costing79650/month, still below generation. Storage was115/151t at16:18. This is an unresolved capacity deficit, not a permanent solution. First city balances after the change remained positive at111334 and109218/month. Recycling at880000 remains unaffordable with the500000 reserve. The landfill's56000 waste-recycling upgrade advertises lower ground/air pollution and15000/month upkeep, not additional throughput; don't purchase it as a capacity fix.

Landfill extension verified after resuming: native UI showed133/353t at02:49 January2027, increased from151t capacity. The user performed the polygon edit. Processing display temporarily109t/month with137% efficiency; don't infer maximum throughput from one post-edit refresh. Extra storage buys time; it does not eliminate the need for a funded processing replacement.

L023 follow-up: after the user-assisted landfill extension, verified capacity353t. Water & Sewage funding reduced75%->65% to match the oversized treatment capacity. The first refreshed readings showed fresh capacity31722 vs consumption13044, sewage248000 vs13044, no import/export requirement and positive monthly surplus103688; a later hourly reading139111. Electricity remains75%, garbage150%. Do not treat the funding cut as a permanent maximum population limit; recheck during growth or weather changes.

L022 follow-up, second morning: at07:16 the same(-1730,1280),600m survey had38 driving vehicles and0 stopped; at08:26,93 vehicles and2 stopped (one yielding, one bus stopped by choice); at09:44,52 vehicles and1 car yielding at66079 with no queue behind it. Compare with the first post-construction morning's108 vehicles and29 stopped around08:18, not with the old-layout baseline at05:18. Rain, January versus December, and several local changes still limit causal attribution. Original industrial road79135 now51% flow vs34% initial; shopping79130 stayed34% vs34%. This supports a local access improvement under observed traffic, not proof of future high-volume capacity or a citywide cure.


## L024 Final verification can qualify an otherwise successful rebuild

Fourth challenge ended paused at frame 9846473, UI 16:30 January 2027, before the real 16:24:25 UTC deadline. Verified save Ezra-Freeway: 26,720,580 bytes, exclusive readable. Population 3,719 → 3,869; cash 1,171,721 → 966,413 after infrastructure and treatment; recurring surplus 208,957 → 225,175/month; debt zero. Treatment's late staffed cost was 103,675/month at 65% funding, with 0% reclaimed-water pollution and substantial spare sewage capacity. Final garbage generation was 150.5 tonnes/month, above earlier observed landfill processing around 137–138; the user's verified 353-tonne storage extension buys time rather than resolving throughput.

Final city flow stayed 66%, and the new inbound auxiliary segment reported 30% despite periods with no stopped vehicles in the nearby census. At final pause there were seven stopped vehicles among 52 in the area, including a four-vehicle eastern industrial queue; no deadlock. Earlier second-morning queue reductions remain useful repeated observations, but neither they nor the more coherent geometry establish an overall city traffic improvement. Next checks should inspect lane transitions and the shopping spine, then repeat matched-time observations before growth.

## L025 Preview loan costs before recommending a principal

Preparation for challenge five, October 9. The initial conditional suggestion of a 500,000–600,000 recycling loan was too optimistic before checking the live quote. The Loans UI preview, without accepting, showed monthly interest 50,909 on 500,000 and 70,909 on 600,000. Reset the preview to zero. Recycling's construction card listed 120,000/month upkeep, 880,000 purchase and 1,500 tonnes/month nominal processing. Wages and recycling income remain unknown. With current surplus 198,431, subtracting 50,909 interest and hypothetical 120,000 incremental upkeep leaves only 27,522 before wages or offsetting changes. This screening scenario does not prove insolvency at every service budget, but it rejects an assumed 100,000/month margin without a funded operating plan. Lower garbage funding and eventual landfill savings must be modeled explicitly, including temporary overlap. Fresh landfill UI showed 144/353 tonnes, generation 151 tonnes/month, processing 135. No city construction, loan acceptance or simulation advancement occurred in this investigation.

## L026 A highway terminal needs an explicit control check

Fifth challenge, October9. New north/south roads and flyovers passed directed graph connectivity tests for all six north/west/south movements. The west-to-south exit uses a separate local collector. Automatic lights appeared both at the highway-to-boulevard taper and at the local T-junction only54m farther south. At18:43 the250m terminal survey had107 vehicles,69 stopped;44 queued behind the collector signal. After a verified checkpoint and removal of both lights, the same survey at20:16 had6 vehicles, only1 stopped bus by choice and no deadlocks. This single post-change result is confounded by declining evening demand. Repeat at peak before claiming a capacity fix. The43m spacing to the existing roundabout and the73m highway merge-to-industrial-exit spacing remain compromises.

Construction receipts also sometimes reported built:true with no segments after a50m extension. A subsequent graph read showed the new road and a moved intermediate node. This is an observed receipt limitation, not evidence that the command did nothing. Rejected slope/overlap trials reported no construction. Preserve native validation; verify graph and visual geometry before retrying.

## L027 Cash accumulation succeeded; longer stability is unproven

Fifth challenge ended early after the user stopped Computer Use. Verified final save Ezra-South,26,948,325 bytes, paused by17:10:14UTC. Cash967032->983256 after18194 net road spending and34418 cash earned during simulation. Population3867->3856, happiness70, health59. Commercial/Office tax10->11; three distinct hourly balances164287,194469,208820 exceeded100000. Commercial demand56 and office93 remained positive at the first post-change demand check. Revenue fluctuated, especially industry, so the final increase cannot be assigned solely to taxes. No new zoning, borrowing, recycling purchase or tiles.

Only about4h06m of game time elapsed. Final garbage generation149808kg/month; landfill storage was not refreshed because the UI check was interrupted. Last144/353t is historical. The final69% city flow uses a changed153-road sample and cannot prove overall traffic improvement. A full daily cycle, current storage and staffed recycling economics remain necessary before a longer unattended earning period.

L027 continuation: the user clarified that the interruption was a meeting and returned control. An eight-minute observation window began17:23:39UTC, deadline17:31:39UTC. Fresh landfill UI at20:40 showed140/353t stored,105t/month processing,29/30 staff and77400/month cost; at23:08 it showed157/353t,118t/month,30/30 staff and79650/month. This observed rise corrects any inference from the initial lower140t reading that storage was sustainably falling. Three more hourly balances were220760 at21:22,206279 at22:46 and217843 at23:54; treasury passed one million without additional construction or policy changes. The22:46 terminal survey had6 vehicles,2 stopped briefly and no deadlock; still nighttime evidence.

L026 morning follow-up after the authorized continuation: at06:56 the terminal survey had8 vehicles,2 stopped by choice; at07:34 it had20 vehicles, only1 stopped bus by choice and no deadlock. The industrial survey had11/0 then8/0 vehicles/stopped. This repeats a clear early-morning observation, but does not cover the complete peak or match the earlier evening queue. Final aggregate city flow66% across153 roads; no citywide improvement claim.

L027 final continuation result: deadline guard paused at17:31:39.159UTC, frame10011035, UI07:34February2027 (API2027-01-02 calendar formatting remains unreliable). Final save Ezra-South-Final verified exclusive readable,26672290 bytes. Cash1082282, population3942, surplus210660/month, debt0, happiness70, health59, unemployment1.53%. Net cash gain115250 from the original start, after18194 net roads. Landfill storage rose to166t overnight then ended159/353t, processing118t/month against151216kg/month generation; no permanent balance. Additional hourly balances195985,217947,220344 and210660 remained above100000. A transient electronics-shop No Customers warning cleared by the finish. No further construction or policy changes during the continuation.

## L028 Connectivity was an incomplete design standard

October 9, research following repeated user criticism. Earlier roads passed directed reachability checks, yet the user identified abrupt ramps and an unattractive interchange, then manually placed the replacement. These are repeated observations of dissatisfaction with our geometry, not controlled measurements of traffic capacity. Credit the current northern preset placement to the user. The existing L019 geometry lesson was not enforced strongly enough during construction.

Correction: compare whole interchange footprints before construction, including lane continuity, tangent joins, grade, ramp-terminal storage, neighboring junctions, local crossings and remaining usable blocks. Apply separate topology, appearance and vehicle-motion checks. A suitable native preset is worth testing; a preset alone does not establish good placement. The [road study](ROAD-DESIGN-STUDY.md) links developer tool documentation and real-world design guidance. A compact service diamond serving both districts is a hypothesis to test against roundabout terminals and site constraints, not an observed improvement or guaranteed capacity result. No traffic experiment occurred in this research pass.

## L029 User progress supersedes the utility migration backlog

October 9, latest user report: recycling is operational, landfill emptied/deleted, treatment relocated, cash positive but below the earlier 500,000 reserve target, and income fluctuating without going negative. These are user-reported outcomes, not new bridge measurements. The archived migration record remains valid history but its next actions are obsolete. Do not rebuild facilities already completed or reuse its old cash/landfill figures as current.

The new objective is research before staged western low/row/medium housing, eastern medium/high waterfront development and a southern freeway corridor. Save the requirements independently from a final construction plan. The screenshot supports spatial intent but not exact dimensions, service capacity or interchange feasibility. Next evidence needed: current terrain, networks, buildings, pollution, finances and service demand; then compare full footprints before selecting a site.

## L030 Rebuild cash must cover lost taxes as well as construction

October9, one live read-only budget snapshot: surplus75,882/month, with555,098/month in residential/commercial/office taxes. Holding other income and expenses fixed, losing half that tax stream implies201,667/month deficit; losing all implies479,216/month deficit. These are calculated stress scenarios, not observed post-demolition results. Existing industry may also lose customers/workers, and services may change costs. This supports including an operating-loss reserve and phased demolition in the next budget; it does not establish a guaranteed recovery time or final construction price. Provisional3million staged and4–5million broad-clearance targets are detailed in [the brief](EXPANSION-BRIEF.md#preliminary-rebuild-reserve--live-budget-check-october-9).

## L031 A live survey changes the redevelopment recommendation

October9 read-only planning survey: all212 road-only graph matches returned; corridor building queries were split until untruncated. Terrain/pollution resolution56m. Arborview(z≈446) offers more separation from the northern junction than Highland(z≈646), making it the preferred candidate crossroad. A compact diamond screening box340×640m is smaller than the compared420×720m roundabout-terminal box; this is a geometric comparison, not a traffic-capacity result. Exact native placement, bridge clearances and southern taper remain untested.

Former northwest industry still has positive raw ground-pollution cells, while sampled southwest school/housing reservations have zero. Do not treat the old industry migration as immediate proof of clean residential land. Existing elementary and fire house may be reusable; bridge inspections do not expose their full capacity. HighSchool02 is unlocked at150,000 with96×64m base footprint. College01 is locked at750,000 and176×128m; reserve land instead of including immediate purchase. Evidence and dimensions are in [REDEVELOPMENT-PLAN.md](REDEVELOPMENT-PLAN.md).

## L032 Paused construction separates capital spending from recovery losses

The user clarified that demolition and the complete required infrastructure build should happen while paused. Agreed: no simulation-time operating losses accrue during that paused construction interval. The reserve stress tests in L030 apply after resuming, while population, jobs and tax receipts recover. Their assumed recovery duration is not construction time. Plan the road/service/utility cutover in one paused pass, verify it before unpause, then observe recovery. This is a sequencing clarification, not a new gameplay experiment.

During planning the user removed Cemetery02. A refreshed inventory confirmed Crematorium01 in the northern service area; a remote old-cemetery ruin is not a replacement service. Remove cemetery relocation from the current plan and exclude it from old building-conflict counts. Final running-city snapshot showed cash243,691 and surplus147,233/month, but user changes and continuing simulation prevent attributing the financial differences to the agent. No game mutations were made by the agent in this pass.

## L033 User takeover invalidated the pending rebuild sequence

October9, initial agent construction after the3million threshold included service relocation, native single-point interchange placement and northern freeway reconnection. Before those works were fully commissioned, the user cleared most southern buildings and constructed a different boundary-road framework. Fresh reads on return showed treasury2,492,489 and no southern service buildings, still at the original paused frame14928842. The earlier readiness note saying no construction had begun was therefore obsolete. This is one handoff observation: preserve attribution, inspect current graph and command receipts, and replace pending actions instead of completing an outdated sequence. Frozen population/service aggregates cannot establish post-demolition functionality.

## L034 Bus-stop warning markers can outlive physical stops

October9, one observed cleanup: cs2_list_transit_stops returned only four distant outside connections; Ezra Town Loop reported zero stops/vehicles but eight orphan waypoint entries and a nonsensical -8m length. The user had removed the roads/stops. Deleting that specific obsolete route through the normal route tool removed its markers; a refreshed line list returned zero bus lines. Do not demolish map outside connections just because the transit-stop query includes them. Inspect both stop objects and route waypoints before cleanup. Treat negative route length on a broken route as invalid telemetry, not a physical measurement.

## L035 Paused residential grid passed topology checks, not traffic testing

October9, one completed construction observation:29 successful calls created approximately8,890m of residential roads around the user's avenues for37,788, leaving2,454,701. All68 surveyed residential/avenue segments in the district connected to the existing network with no isolated or degree-one endpoints. Visual review showed connected north/south grids, preserved western education plots and freeway separation. New ground-pollution sampling was zero across both planned housing areas; this updates the older contaminated-soil observation only within those surveyed bounds. No simulation frames advanced, no zoning/services were added, and no operating/traffic success is claimed. Road spacing and connectivity are readiness evidence; future vehicle movement and populated recovery remain separate tests. Raw evidence: ignored .local/r2-*; verified save Ezra-Residential-Roads.

## L036 Small infill can combine service loops with intentional residential courts

October9, one paused construction observation: a two-access service loop and a curved residential loop fit the user's marked pockets, with two approximately92m courts occupying part of the narrower freeway-side strip. Seven normal road operations built961m for3,808; final graph confirmed all endpoints reachable and exactly the two intentional court terminals as new degree-one nodes. Those terminals use ordinary rounded road caps; no separate cul-de-sac bulbs were placed. Base fire/police footprints40×40m informed frontage, but no full upgrade-fit or emergency-response test occurred.

A zoning readback confirmed55 row-house and62 low-density cells, all empty. The streets passed graph/visual checks only; no vehicle turning, traffic capacity, housing occupancy or noise comfort result exists while paused. Sampled ground/air pollution was zero and noise nonzero. Keep a freeway setback without treating row houses as a proven noise barrier. The B001 name-encoding failure repeated and correctly encoded requests through the same endpoint succeeded. Verified checkpoint Ezra-Service-Residential-Infill; raw .local/r3-*.

## L037 Larger campus selection changed both footprint and budget

October9, one paused construction pass. Smaller schools were placed first, then the user requested larger versions. Normal bulldozing returned47,500 of190,000 paid: a142,500 replacement loss. The final ElementarySchool01(144×64m,100,000) and HighSchool01(176×128m,300,000) were placed on larger local-road sites with three nearby parks, tree-lined frontage and576.6m of connected walking paths. Total net spend including the replacement, parks and eastern roads was620,620, leaving1,611,945. Preserve this correction instead of reporting only final asset sticker prices.

Elementary native UI showed1,000 seats,0/15 staff and12,500/month maintenance; the game was paused and no students/staff arrived. Capacity on the card is not a demonstrated education outcome, and maintenance without wages is not staffed cost. Larger campus appearance was visually checked; all upgrades and future enrollment remain untested.

## L038 Rebuilt junction IDs can produce false disconnected-road reports

October9, one graph-verification observation. The21 eastern road operations built approximately6,233m, but four node IDs from early receipts were absent after later connections reconstructed roads. A check against historical IDs flagged them as unreachable; the fresh untruncated754-network graph showed all68 current eastern district road segments connected and no unintended new dead ends. Missing obsolete IDs are not evidence that roads are broken. Verify current geometry, nodes and components before rebuilding anything. This repeats the broader receipt limitations in L026/B012 without establishing traffic performance.

A separate optional path into the coastal QuayLarge01 was rejected InvalidShape with no change. The prefab is a24m RoadPrefab, but that alone does not guarantee every proposed pedestrian junction is valid. No direct quay path was built; ordinary school paths attached successfully to street junctions. Raw evidence .local/r4-*; verified save Ezra-Schools-Parks-East.

## L039 City service variants have distinct capacity and footprints

October 9, one paused construction observation. Native asset cards confirmed City Fire House = FireHouse02 (24×32m, 125,000, 6 engines), City Police Station = PoliceStation03 (48×48m, 220,000, 16 patrol cars and 40 jail spaces), City Elementary School = ElementarySchool03 (48×64m, 150,000, 1,500 students), and City High School = HighSchool03 (96×48m, 300,000, 800 students). City assets are specific variants; a numeric suffix or larger physical footprint alone does not establish the intended style or capacity. The compact city elementary card has more seats than the much wider ElementarySchool01 card observed in L037.

Four normal roadside placements cost 795,000, leaving 518,293. Schools face local streets at opposite ends of the eastern district; emergency services face the collector outside its immediate ramp approach. Inventory, entity inspection, screenshots and the current 68-segment eastern road graph verified existence, alignment and connected frontage. All remained unstaffed at the unchanged paused frame 14928842. Asset-card upkeep sums to 77,000/month, but actual staffed expense, enrollment and service response remain untested. Open surrounding space is a reservation, not a verified full upgrade fit. Final save Ezra-East-City-Services verified readable; raw evidence `.local/r5-*`.

## L040 Civic reservations need asset and junction elevation checks

October 9, one paused construction pass. The live CityHall01 base footprint is 128×160m; its cost is 1,600,000. Roads were arranged around a larger northeast civic reservation instead of extending the ordinary small-block grid through it. A coordinate-only preview places a potential south-facing Hall near (-575,1930), but no building was bought and no full native placement/upgrade test occurred. The reservation remains a geometric plan.

The fresh Coldwater road graph showed its freeway-overpass descent from y521 near x=-940 to y511 near x=-836. Extending the x=-854 collector straight north would meet that slope. Instead, the local Brightwater connection reached the existing flat junction near (-712,2118). This is a measured geometry correction before building, not evidence of avoided traffic failure.

Six normal operations built 1,226.94m, including a civic boulevard and one 203m cross-street farther south. Three boulevard segments received trees and median trees. Treasury changed from 451,808 to 446,060 (5,748 spent). All nine new segments were present and connected in the final untruncated 384-road graph, with no new degree-one endpoints. Native visual checks confirmed the layout and planting. Traffic and future building operations remain untested at paused frame14928842. User constraint: east-side housing is medium/high density only; large civic and future-building plots stay reserved. Verified save Ezra-Civic-Roads; raw `.local/r6-*`.

## L041 Minimum brush rows improve control, but are not exact-cell editing

October9, interrupted paused zoning correction. The user rejected the earlier broad circular patches and requested smallest-brush work through the bridge. Source confirms an8m minimum radius and8m cells; one brush can touch multiple cells. An8m-spaced world grid constrained to planned block rectangles filled western frontage sequentially. The first100 placements completed in2.007seconds and reported108 changed cells. A later whole-city read after3,529 placements showed13,905 zoned cells versus11,853 at the start; screenshots showed more continuous strips. These are one-session coverage and latency observations, not evidence of grown buildings, demand balance or user acceptance.

The user took over after6,029 placements. No eastern pass or final clearing pass occurred. Preserve that incompleteness, stop all mutations, and refresh after manual edits. Source also shows no occupied-cell exclusion: one existing low-density lot lies in a planned row-house strip, so this approach must not be described as only-empty or exact-single-cell editing. QuayLarge01 is a zonable frontage reference and must not be filtered out as if it were a highway. Keep the western red service loop and civic grounds reserved. Proposed bridge tools and protection requirements are in B015.

## L042 Mailboxes provide access before they provide a collection service

October9, one running-city placement pass: native Mailbox card showed100 cost and1,000-letter capacity.14 placements across west/east neighborhoods, bus hub and industry were verified by build notifications and persistent markers, for1,400 capital. Initial clicks just outside the road produced Road required/Overlapping items previews; selecting the road center let native snapping attach the asset. Some quick clicks only moved the valid preview and needed a subsequent observed click to apply. Count persistent placed markers, not previews.

The post overlay showed green access roads while processing remained0/month. That observation agrees with the card's statement that post-office vehicles collect mailbox contents; green roads alone cannot establish functioning collection. Official documentation: https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies . The current bridge cannot find/place/list mailbox net objects through its building/tree or bus/tram tools. Add generic supported roadside-service-object discovery, placement and inventory instead of pretending a bus-stop tool can place mailboxes. No new bridge implementation in this pass. Final verified save Ezra-Mailboxes; city left running as found.

## L043 Campus footprint fit did not establish an acceptable layout

October10, one interrupted campus construction pass. The user requested three fully upgraded universities plus housing, parks, paths and parking, and approved using earned cash in stages. University01 with Wing, Library and Park was built for2,175,000; native panel confirmed17,500 capacity and205,000/month maintenance before wages. Detached Library and Park were accepted across Woodland Street from the parent. TechnicalUniversity01 base was then built for1,600,000 after six empty surface-road segments were replaced with a boulevard/rear loop. No simulation commissioning occurred.

The user said the layout was being messed up and took over before technical upgrades, medical construction or support features. This is explicit negative design feedback; successful placement and sufficient geometric space are not evidence of an attractive or accepted campus. The precise objection was not specified, so do not invent one. For a future large campus layout, make the arrangement visually reviewable before committing its road relocation and major building placement; footprint calculations alone did not communicate the result adequately here. Stop immediately on takeover and preserve the partial state accurately.

Operational corrections: the known plus/space prefab encoding bug repeated for Small Road. The first direct road request succeeded, but immediate follow-ups encountered a busy response and one validation rejection. A PowerShell loop incorrectly retained the previous result after nonterminating HTTP errors, producing two unreliable receipt files. The later helper checked HTTP status, recorded each response separately and waited850ms between operations. Treat only corrected receipts and fresh graph/UI as evidence, not stale loop variables. Verified start checkpoint Ezra-University-Campus-Start; no final save before takeover.

## L044 Residential road planning benefits from current ramp envelopes and reserved service plots

October10, one road-only construction session in the user's red-outlined western/southern expansion. Current graph review caught a planned southern closure that would have overlapped a two-way highway and an existing ramp. Those streets were revised before construction: local loops close north/west of the ramps and connect to existing surface junctions. A northern junction with five graph edges has four physical approach directions because two opposite one-way ramp carriageways share the western approach. Edge count alone does not identify an awkward five-arm intersection.

37 native road operations built about12.64km for55,364 while paused. The final graph matched87 current segments, all connected in both travel directions, without unintended dead ends or new southern freeway joins. Six service/school rectangles remained free of road centerlines; actual building/upgrades were not placed, so full native fit is unverified. Nine quiet internal T-junction lights were removed and independently verified off, while avenue entries, ramp signals and four-way collector crossings remained. This is one topology/geometry observation, not measured traffic capacity or user acceptance of the aesthetics.

The user resumed during final checks and inspected the campus. The agent left their controls and simulation state alone; final save Ezra-West-Residential-Roads was verified readable. Compare construction spending at the unchanged paused frame, not against the later running treasury. Raw evidence `.local/wr-*`.

## L045 Freight planning needs global connections and full facility reservations

October10, one read-only industrial planning survey. A local train-network query found no tracks; a global generic-network query returned64 train segments, all south/west of the proposed district. The nearest sampled endpoint is about5.4km away in a straight line. Both outside rail connections are reachable from that existing rail network, but the industrial site has no short spur. Reserve terminal, yard and approaches without calling cargo rail ready; the long connection is a separate project. No rail yard existed in the building inventory.

Live asset data showed Airport01 at1136×376m, the largest industrial signature factory at288×288m, and compact RailYard02 at272×120m with a272×96m Extra Rails extension. Those dimensions changed the concept before road subdivision. All17 proposed parcels sampled dry/owned using the56m terrain grid; this is a coarse screen, not native placement verification. The exposed data did not establish airport cargo-upgrade geometry, all attachment locations or signature unlock conditions.

Review caught specific draft conflicts: a utility-loop corner lay within about5m of an existing ramp; the initial railway strip cut a worker reserve and grazed ore/factory space; the first northern manufacturing road was only18m from a wet terrain sample. The final concept reuses surface-avenue access, moves the conflicting reservations and pulls the northern street south. Rail-yard frontage, track turnouts, airport approaches and native upgrade fit remain unresolved before construction. These are planning observations, not built-road performance or aesthetic acceptance. User requested skipping a separate pollution analysis and selected power reservation rather than immediate solar construction. Evidence `.local/ip-*`; decisions in [INDUSTRIAL-PLAN.md](INDUSTRIAL-PLAN.md).
## L046 Industrial road construction needs native validation and a recoverable tool state

October10, one industrial construction session. The user approved the surveyed road plan.63 retained successful road operations were matched to127 current segments, approximately18,336m, after a game restart changed entity IDs. Independent graph checks found every new road reachable from the existing industrial network and only two new degree-one ends: the airport avenue at(-1945,4400), deliberately reserved for later development, and the freeway transition at(-2655,3285), which the user offered to finish. A native medium roundabout at(-2655,3520) was verified visually and by graph flags; local small-road junctions remained unsignalized and principal collector intersections retained signals. This is topology/placement evidence, not a traffic-capacity test.

Construction corrected schematic assumptions. The original Factory E loop conflicted with a ramp/highway corridor; its revised loop and northeast chamfer clear those roads and the power line, leaving a370×200m reserve at X-4490…-4120,Z3020…3220. The largest factory remains reserved in C. E has one external access, while the production, manufacturing and utility networks have alternatives. A straight northern collector was rejected OverlapExisting near a pylon. A first revised bend then exceeded the owned tile ending atZ3428.174; the accepted smooth bend peaks atZ3410. No tiles were bought. Coarse owned/dry parcel screening did not certify every road alignment or native footprint.

A short freeway-taper command timed out without a segment in fresh graphs. Subsequent builds remained busy after native tool changes and reloading a verified checkpoint. A full game restart restored construction; the underlying cause remains unproven. Record this as one recovery observation, not a reliable general cure. Reload/restart automatically ran the city briefly; initial paused native time15:42June2030 became final16:06, frame20590103. Always refresh pause after loading. The final city is paused, population41,443 and treasury13,807,674. Treasury change across the whole session is not construction cost because simulation advanced during recovery.

Verified checkpoints: Ezra-Industrial-Roads-Start, Ezra-Industrial-Roads-Recovery, and final Ezra-Industrial-Roads. Final file37,440,553bytes, exclusive-readable archive with36,886,758-byte SaveGameData. P remains future-phase roads; facility-specific gates, rail crossings, actual upgrade fit, zoning, solar and other facilities were not built. Raw evidence `.local/ib-*`, independent review `.local/ib-review-final-check.json`; bridge improvement B018. The original concept map remains historical and does not show the as-built E and collector corrections.


## L047 Facility connectors can invalidate a previously reserved approach

October10, one paused industrial construction phase. Native RailYard02 placement unlocked CargoTrainTerminal01/02. Airport01, RailYard02 and CargoTrainTerminal01 were placed and verified by inventory/graph. The cargo terminal's internal rail runs from X-3376 to-3144 atZ3545.8418; its lot extends west to-3452. Thus the western track endpoint is inside the lot and an external west spur would cross its truck circulation. The east endpoint is4m outside the lot and is the appropriate observed connection candidate. The resulting broad east/north rail approach requires shifting O's worker hub east; no rail was built before interruption. Lesson: distinguish internal track ends from external asset connectors using footprint, internal roads and native preview; endpoint degree alone is insufficient. This is one asset observation, not a universal rule for all terminals.

P/C/K/D road requests succeeded while paused; C's road arrangement preserves a geometrically screened400×320m core. D contains a live power-line pylon and K's originally drawn southern edge overlapped a highway corridor; D received only east frontage and K shifted north. Final topology/traffic performance remains untested. The user purchased36tiles and asked for curved outer-edge rail with switches near the incoming connection, keeping the new interior land open. Computer Use was stopped by physical Escape before rail/O work; root ceased game input. Latest save Ezra-Industry-Track-Ready is verified exclusive-readable,37,547,941bytes. Raw evidence .local/if-*.

## L048 A technically connected railway can still fail the industrial-yard design brief

October 10, the user rejected the first revised rail layout visually: the second cargo track merged too early, switches looked untidy and sharp, Q bent south instead of heading west, and the large yard was missing from the district. Both cargo tracks were connected and two sidings were built, but those results do not prove an attractive or high-capacity yard. A fresh graph after interruption showed Q's unfinished fan ended at a degree-one node; no wye branch persisted. This is explicit design feedback, not measured rail performance. The revised concept keeps separate parallel cargo leads, long switching ladders, straight westbound Q approaches, a large yard with maintenance grounds, and aligned tunnel ramps. Native asset connectors and known pylons must constrain the whole layout before individual turnouts are committed.

One successful native relocation observation: CargoTrainTerminal01 moved and rotated from 180 to 0 degrees for 34,500 while preserving its Extra Tracks upgrade, at (-3297.99976, 3525.99976). A zero-scroll pointer action provided a valid relocation preview before clicking; all UI work used the approved Computer Use API. A fresh graph confirmed two exterior west connectors. Seven obsolete roads were removed only after checking the exact targets against the graph and proving that B/Q/C/D access would remain. Automatic approval review's stale-track and broad-demolition objections were resolved with fresh read-only evidence.

Code audit shows negative elevations on free ends with a -30 m handler clamp, but X/Z snapping ignores height and keeps the snapped node's height. No tunnels were built in this review, so do not claim adequate cover, grade or operating connectivity from supported parameters alone. Checkpoint **Ezra-Rail-Design-Review** was verified at 37,716,036 bytes. Revised concept: [RAIL-REDESIGN.md](RAIL-REDESIGN.md). Raw evidence: `.local/rr-*`.

## L049 Native rail turnouts and portals reconstruct geometry

October 10, one paused rebuild after explicit user approval. A quadratic merge ending exactly parallel to an existing rail failed with OverlapExisting/InvalidShape. A revised 130 m finishing curve with about a 15-degree joining angle succeeded, following a longer shallow approach. Multiple later finite-angle merges succeeded. This supports using a deliberate switch angle instead of making two tracks tangent at the merge; it does not establish a universal minimum angle. Keep approach lengths and final joining angles distinct.

Six 600 m descents to nominal -20 m were accepted, with fresh graph endpoint elevations below terrain and visible portal approaches. Native reconstruction shifted the surface/portal transition nodes, including the southern return from the requested Z2600 to about Z2512. A lookup for an exact historical coordinate incorrectly reported no connection; graph traversal confirmed the retained southern mainline was reachable. Adjacent demolition also combined segments and invalidated IDs, so refreshed exact targets were necessary. Do not retry stale removals or infer failed connectivity from a missing old node.

The large yard's two upgrades fit within its base lot, and the Extra Tracks extension adds a sixth west/fourth east gate at Z3619.15576. Native UI showed both upgrades installed later in the session; their exact purchase timing was not isolated. Inventory queries list the base building rather than separate in-place upgrades. Reinspect asset-owned rails after any upgrade.

An undirected connected graph did not establish a forward depot-to-terminal movement. A buried return leg and complementary eastbound cargo crossover were added for that purpose. Independent geometric review found roughly 145–300 m minimum curve radii on the return and owned tiles along its sampled path. Native construction succeeded, but train operation was not tested because the game remained paused. Final verified save: Ezra-Industrial-Rail-Rebuilt. Physical Escape stopped Computer Use during the final visual check. Raw evidence `.local/rb-*`.

## L050 Service capacity and staffed upkeep require native inspection

October 10, one developed-city service expansion. At about 42,600 residents the city had two clinics, three small firehouses, one police station, two crematoria and a cemetery. Building count alone hid a death-care deficit: native UI reported 312 deaths/month versus 221 cremation capacity, and 1,033/1,500 cemetery places occupied. A third crematorium in the southern city was therefore justified by measured processing and geography. After placement it had 20/20 staff, 9/10 hearses gathering and 114 bodies/month processing. Total city handling was not reread before user interruption, so do not claim a lasting surplus from addition alone.

Hospital, headquarters, large fire station and crematorium bases cost 6,370,000. Two short empty-frontage one-way roads were converted to Medium Road for 480, preserving the signal and roundabout at their ends. Native roadside placement aligned all four sites without demolishing houses. Hospital access opposite an existing pedestrian path was acceptable because the path ran away from the proposed footprint; raw junction segment count did not mean a road crossed the site.

Hospital, headquarters and fire station all reached 120/120 staff during a short running check. Hospital accepted patients and dispatched ambulances; headquarters sent 20 patrol cars. Native operating bills included substantial wages: hospital about 528,300/month, headquarters 395,000, fire 303,500 and crematorium 53,500. Asset cards showed lower maintenance figures for police and fire. Use staffed totals for operating forecasts, then refresh the city budget; observed surplus remained positive (+1.46 million and later +2.14 million/month), but taxes and transport fees also changed.

User pressed physical Escape during final review. The game was paused and no more inputs were issued. Start checkpoint Ezra-City-Services-Start is verified; no post-construction save was completed. Future handoffs must distinguish verified construction/operation from uncompleted saving. Evidence `.local/sv-*` and native service panels; final frame 21290603. The user has control.


## L051 Underground station surface size does not describe its track footprint

October10 transit expansion, one operating observation. SubwayStation01 occupies24×16m above ground but supplies a200m underground double-track platform at20m depth. Four entrances could fit existing vacant frontage without clearing homes, but planning needed their actual generated endpoints. The first Northgate candidate failed native overlap validation; a clear site across from the bus terminal succeeded. A yard survey whose radius clipped an outer segment mistook a portal node for the free end. Native InvalidShape rejected the attempted branch there without changing the city; a closer fresh graph found the real free end44m farther east and the continuation succeeded.

Exact platform IDs allowed explicit outward and return stop order. Both metro lines passed native pathfinding while paused, then dispatched3 and1 trains and carried passengers during a short running check. That separates placement/connectivity evidence from operation. M1 final32 aboard and M2 one aboard are early observations, not sustained ridership. Both new bus feeders also dispatched and carried passengers. A lower overnight queue than an earlier daytime queue does not isolate the effect of added buses or metro. Final checkpoint Ezra-Transit-Network-Ready verified; raw evidence .local/tr-*.

## L052 Bicycle overlays require the native existing-road upgrade path

October10 source audit plus one native trial. Correction to an initial hypothesis: a FencePrefab classification does not mean a bicycle overlay is decorative or nonrideable. Native Double Bicycle Lane is a road-service overlay. Generic bridge connect/build accepts network prefabs but emits a new NetCourse, which does not establish equivalence to native existing-road upgrade definitions. The current upgrade endpoint accepts eight aliases only; no native-enum fallback exists. Installed metadata maps bicycle and public-transport lanes to SecondaryLane and PrimaryLane side flags, subject to native set/unset and composition compatibility rules.

A native bicycle-lane action applied to one segment. A long drag did not establish a continuous upgraded corridor; exact extent remains unverified after interruption. User took over cycling work. Do not report a cycle network or bus lanes from tool selection or an isolated successful click. Proposed API/readback changes are B019. No bridge implementation changed.


## L053 Developed-neighborhood park distribution needs an occupied-building survey

October10, one park/service placement pass. Far-west street grids had almost no housing despite looking like completed neighborhoods on a road map; the northern civic block contained offices and City Hall rather than residences. Existing park inventory was concentrated west, with only two cafes on the dense eastern side. Eight additions therefore used occupied-frontage gaps: two playgrounds, two green squares, exercise park, dog park, urban plaza and an entrance garden opposite City Hall. Exact footprint checks plus native placement and inventory confirmed all eight without house demolition. This establishes placement and distribution, not measured leisure coverage or aesthetic approval.

A fresh inventory found no road or park maintenance depot. Both were placed outside the reserved factory interior on a two-way street near the northern gateway. Native pre-run panels showed10 vehicles/30 employee positions each. Generic bridge inspect later reported40 and48 employees, so those values should not be equated with native staffed capacity without a fresh panel check. Physical Escape interrupted that check and final saving. Do not claim vehicle dispatch, mature park benefit or a completed save from placement receipts alone. Total paused capital337,500; start checkpoint verified, no final save. Evidence .local/pk-*.


## L054 Predetermined zoning batches need spatial exclusions and resumable receipts

October 10, one northern industry/factory session. The user requested faster chained bridge actions. Six current-unlocked signature factories fit reserved local frontages; one470 m Small Road closure connected the D/E loop and supplied refinery access. Independent current-graph review verified all six frontages reachable in both directions. Placement/access topology does not establish production or traffic capacity. Refresh unlocks before committing: the Oil Refinery changed from locked to available during the pass, while three other signatures remained locked.

Three sequential brush groups completed390 radius8 m / step8 m actions, painting343 industry,76 low-office and77 high-office cells. Before each group the helper refreshed roads/buildings and screened a predefined rectangle against avenue/highway clearance, surface rail, full building footprints and protected facility/service reservations. Every mutation required the same paused frame and recorded its intent/result; any existing receipt prevented blind replay. No errors occurred. Area readbacks agreed with the new-cell totals. This is one successful batched run, not proof of universal brush precision or a measured speedup. The brush remains radial and does not expose exact cell IDs or occupied-cell protection.

Decision rule: batch predetermined, screened steps sequentially; inspect outcomes at meaningful boundaries. Stop on an error, ambiguous response or changed precondition and refresh before continuing. Keep adaptive placement, demolition and recovery decisions outside blind chains. A later city graph/UI check remains necessary when native reconstruction or user edits can alter a target. Final overlay inspection was deferred while the user actively used transit UI. The user resumed during saving, so the final handoff reports running rather than the earlier paused construction state. Final checkpoint Ezra-Northern-Factories-Zoned verified. Evidence .local/sf-*; complements the minimum-brush method in L041.

## L055 Cargo discovery does not imply creation or operating support

October 10 cargo session, reconciled October 11 after the user confirmed takeover. The bridge found cargo train platforms, airport freight gates and outside connections, but source audit showed line creation selects passenger-only prefabs/stops and rejects air routes. Its rejection message claiming air routes cannot be drawn is contradicted by the native cargo-air routes returned later. This is a verified capability/description mismatch, not a limitation of the game.

The user created two cargo-air routes and three cargo-rail routes while the agent was inspecting. The last paused read showed active routes but zero vehicles; no freight movement was demonstrated. Two Salford entries used the same platform/destination with reversed order, so they are candidates for a fresh redundancy review, not an agent-created duplicate or an authorized deletion. The agent created no cargo route and no final cargo save was verified after interruption. Start checkpoint Ezra-Cargo-Lines-Start is verified. User takeover supersedes any pending construction sequence.

Operational rule: distinguish discover/read/create/edit support by mode and passenger/cargo purpose; verify platform compatibility, completed return path and actual dispatch/loading separately. For cargo routes, the current passengersOnBoard field holds generic native freight load and must not be called a passenger count. Units require native comparison before conversion. Capability status must also separate source implementation from client exposure. Proposed fixes and acceptance criteria: B020–B024 and [roadmap R01/R06](../BRIDGE-ROADMAP.md). Evidence .local/cg-*; no fresh game observation during the October11 roadmap review.
