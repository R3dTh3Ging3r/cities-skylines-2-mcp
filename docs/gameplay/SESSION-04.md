# Fourth challenge — Ezra City (archive)

Fourth challenge completed October 9, 2026. Refresh these historical measurements through the bridge before resuming. Previous result: [SESSION-03.md](SESSION-03.md); design/research: [FREEWAY-PLAN.md](FREEWAY-PLAN.md).

## Historical preparation for the next challenge

The user requested a junction near the westward highway bend, a southern branch serving the residential area, and a reserved continuation for much later urban expansion. Recycling and stable cash accumulation should support subsequent larger expansion and possible industrial tiles. The proposed brief is [SOUTHERN-CONNECTION-PLAN.md](SOUTHERN-CONNECTION-PLAN.md). The user subsequently chose a focused 30-minute run for the southern junction/extension, improving recurring revenue and accumulating cash. Recycling, loans and tile purchases are deferred. The user offered further landfill expansion if needed; last UI storage was 144/353 tonnes. The 30-minute clock has not started. Read-only preparation found the city paused at frame 9847192, cash 967,032 and monthly surplus 198,431; the fourth challenge's historical results below remain unchanged. Next action: survey the southern corridor and live recycling costs, refresh waste storage, and verify a new checkpoint when gameplay begins.

## Objectives and clock

One real hour: **15:24:25–16:24:25 UTC**, or **10:24:25–11:24:25 America/Chicago**. Paused construction time counts toward that hour. Primary: a coherent westbound freeway north of industry, expandable toward the western tile edge, with smoother ramps and deliberate industrial/town collector access. Secondary: wastewater treatment with future expansion space. Recycling only if affordable. Guardrails: practical 500,000 cash reserve, no loans, positive recurring balance, and a carried 100,000/month surplus target. No growth zoning during this experiment.

The bounded final observation auto-paused before the deadline at frame **9846473**, UI **16:30 January 2027**. No further construction or simulation was requested afterward. Final save **Ezra-Freeway** was verified complete by exclusive file open at 16:23:56 UTC: **26,720,580 bytes**, SHA-256 recorded locally in `.local/s4-save-verified.json`. The independent deadline guard confirmed paused at **16:24:25.166 UTC**; a fresh state read matched the saved frame. Leave the game paused until the user requests more play.

| Measure | Start | Final paused state |
| --- | ---: | ---: |
| Population | 3,719 | 3,869 |
| Cash | 1,171,721 | 966,413 |
| Monthly income | 546,275 | 671,040 |
| Monthly operating costs | 337,318 | 445,865 |
| Monthly surplus | 208,957 | **225,175** |
| Debt | 0 | **0** |
| Happiness | 71 | 70 |
| Health | 59 | 59 |
| City traffic flow | 66% | **66%** |
| Unemployment | 0.91% | 1.97% |

The city remained Tiny Town; Big Town was not this run's objective. Final XP was 20,118, with three homeless citizens. Population increased within existing zoning.

Final utilities: electricity 85,200 production / 63,716 consumption, fully served; freshwater 31,772 capacity / 13,142 consumption; sewage 244,000 / 13,142. No utility imports were needed. Garbage generation ended at 150,528 kg/month. Final notifications included the two intended western dead ends, disabled outlet, a disconnected high-voltage endpoint, one upgrading building and a traffic accident at (-1732,452). The earlier fire notification cleared. The high-voltage endpoint needs inspection, although electricity demand was fully fulfilled.

At final pause the interchange-area census had 52 vehicles, seven stopped: a four-vehicle queue on the eastern industrial road, another car at a signal, and a landfill work vehicle among the reported queue heads. No deadlock or game-flagged bottleneck was reported. This does not erase the lower morning queues, but it prevents claiming a universally queue-free network.

## Freeway and collectors

Replaced the improvised entrance and sharp industrial ramp with a divided westward mainline, two through lanes each way, and a four-ramp service interchange. The crossroad bridges the freeway at x=-1950; the mainline ends at x=-2160, just inside the owned western boundary. The eastern ramps serve the existing outside connection. The western pair is future-facing: the western mainline stubs are not outside connections yet. Dead-end icons there are expected.

Compound ramp curves use aligned tangents, with auxiliary lanes on the active mainline approach. Native snapping changed some requested positions; verified the resulting graph and vehicle use. Converted the former western highway feeder to a four-lane city collector, removed its unused 46 m stub and redundant junction signal, and added street trees. Existing buildings were not intentionally demolished. The bus loop remained active with six vehicles and 83 passengers in an afternoon check; this is a snapshot, not daily ridership.

An early post-construction morning check found 108 vehicles and 29 stopped within 600 m of (-1730,1280), including a 19-car signal queue. Removed the verified three-way industrial junction's light; the queue initially shifted onto Arborview before clearing. Later collector/stub changes also occurred. The following morning, the same survey showed 38 vehicles/0 stopped at 07:16, 93/2 at 08:26, and 52/1 at 09:44, without a persistent queue. These compare two mornings after replacement, not the original layout at a matching hour. Weather, month and multiple changes limit causal attribution.

Original industrial segment 79135 improved from 34% baseline flow to 51% at the second morning check. Shopping segment 79130 remained 34%. A later reading flagged the new inbound auxiliary-lane segment 52211 at 30% despite no stopped vehicles in the contemporaneous area census. This deserves lane/merge and movement inspection next time. No quantitative individual truck-speed test established that all braking issues are fixed, and future high-volume capacity is untested.

The working overpass remains at 12 m elevation with approximately 9–10% approach grades. Automatic approval review rejected an optional eight-segment rebuild to lower it to 8 m because it could interrupt active city access. No rejected rebuild was performed; the functioning bridge was retained.

## Treatment, waste and reserved land

Built WastewaterTreatmentPlant01 for 400,000 at (-1534,1200), on the former approach corridor, with service access on x=-1482. Surveyed cells had no groundwater or existing ground pollution before placement. At the late UI check it had 50/50 staff, 244,000 m³/month sewage capacity, 5% usage and 0% pollution in reclaimed water. The 50% purification figure describes water recovery separately from output pollution. At 65% Water & Sewage funding, full staffed cost was **103,675/month**: 39,000 maintenance plus 64,675 wages.

After replacement operation was verified, the original sewage outlet was disabled normally and retained as standby at 750/month. Electricity remains 75%, Water & Sewage 65%, Garbage Management 150%, Transportation 75%; other settings were retained. Distinct hourly balances after the water adjustment were 103,688, 139,111, 183,829 and 221,860/month, supporting profitability after the new service commitment.

Keep unzoned land west/south of treatment open for attached upgrades. Full legal attachment envelopes and maximum counts remain unverified, so full expansion fit is not guaranteed. A provisional recycling parcel lies east of the access street at x=-1470..-1180, z=1080..1280. Validate attachments, frontage and terrain before buying; do not fill it with industry. The service precinct can be reused for later industrial/service expansion if much larger future treatment needs require relocation.

**The user enlarged the landfill manually** after native automated corner drags failed. Verified capacity rose **151 → 353 tonnes**, with 133 tonnes stored at 02:49 January 2027. The final shape tapers along the neighboring ramp rather than matching the originally proposed narrow rectangular extension. This bought storage time. It did not fix processing capacity: generation rose to roughly 148–152 tonnes/month after treatment, while observed landfill processing peaked around 137–138 tonnes/month at maximum funding; a post-edit reading was temporarily lower. Full landfill cost was 79,650/month. Continue measuring storage and processing.

Recycling was deferred: its 880,000 base price would violate the 500,000 reserve before staffed operating costs. No landfill demolition or recycling add-on was purchased. The landfill's 56,000 pollution-reduction upgrade did not advertise additional processing and was not treated as a throughput fix.

## Checkpoints, limitations and next step

Verified intermediate checkpoints: Ezra-Freeway-Start, Ezra-Freeway-Connected, Ezra-Freeway-TreatmentStart, Ezra-Freeway-BeforeGrade and Ezra-Freeway-Utilities. Final save verification is recorded above. Raw evidence, screenshots and save metadata stay in ignored `.local/` files.

Next session: refresh finances, landfill storage/throughput and active incidents first. Prioritize a funded waste-processing replacement and normal landfill retirement plan; inspect the inbound auxiliary-lane segment and shopping spine before more housing. Confirm treatment upgrade envelopes before neighboring development. Bridge work should add normal landfill polygon editing, capacity/processing readback, building activation and upgrade-envelope inspection, plus road snapping/lane/grade preflight. See [BRIDGE-NOTES.md](BRIDGE-NOTES.md) and [LEARNING.md](LEARNING.md).

The API calendar previously disagreed with the UI (December 2026 appeared as 2026-01-12); January now displays 2027-01-01. Frame indices and real UTC establish the clock, not calendar labels alone. Earlier progress prose estimated construction at 15:52 UTC, but a recorded 15:49:16 check already showed treatment built; the independent deadline was unchanged.
