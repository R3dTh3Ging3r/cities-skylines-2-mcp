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
