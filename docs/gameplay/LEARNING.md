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
