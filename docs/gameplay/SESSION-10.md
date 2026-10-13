# Ezra City — eastern city services

October 9 task completed: the user requested city versions of fire, police, elementary and high school on the east side. Four additional buildings placed; the western large campuses remain. Previous schools, parks and eastern street work is archived in [SESSION-09.md](SESSION-09.md).

## Measured state and checkpoints

City remains paused at frame 14928842, native UI 01:48 September 2028 (bridge date formatting still reports 2028-01-09). No simulation elapsed and no active wall-clock deadline. Frozen population 7,418 is not evidence of post-clearance recovery.

Initial read showed 1,319,233; treasury changed to 1,313,293 during the survey before agent construction. Do not attribute that 5,940 difference to the four placements. Construction cost exactly 795,000, leaving 518,293. No loan, demolition, road construction or tile purchase in this pass.

Pre-work save **Ezra-East-Services-Start** verified exclusively readable, SaveGameData 25,588,714 bytes. Final **Ezra-East-City-Services** verified exclusively readable, SaveGameData 25,648,440 bytes and texture 456,981 bytes. Raw receipts and surveys are ignored `.local/r5-*` files.

## Built services

| Building | Prefab / entity at verification | Site | Cost | Native asset-card base provision |
| --- | --- | --- | ---: | --- |
| City Fire House | FireHouse02 / 85017v315 | Coleridge Street, (-882.18, 892.67), facing east | 125,000 | 6 engines; listed upkeep 12,000/month |
| City Police Station | PoliceStation03 / 99719v335 | Coleridge Street, (-890.18, 216.67), facing east | 220,000 | 16 patrol cars, 40 jail spaces; listed upkeep 25,000/month |
| City Elementary School | ElementarySchool03 / 102054v239 | Daffodil Street, (-702.18, 56.00), facing west | 150,000 | 1,500 students; listed upkeep 17,500/month |
| City High School | HighSchool03 / 103728v33 | Brightwater Street, (-742.00, 1410.67), facing east | 300,000 | 800 students; listed upkeep 22,500/month |

All four localized city names and prices were confirmed from the native menu before placement. Normal game validation accepted each building. Refreshed inventory and individual inspections confirm all four exist, with zero employees while paused. Screenshots checked alignment; school entrances face local streets, emergency services face the collector, away from the immediate central ramp approach. School surroundings retain open land for future landscaping and possible additions; full upgrade envelopes have not been tested. No additional landscaping was built this pass.

Current graph verifies all four frontage segments and all 68 eastern district road segments connected to the existing network. This is geometric/topological evidence, not a response-time or pedestrian-use test. The four cards list 77,000/month combined upkeep; do not treat that as verified fully staffed operating expense or quote the frozen budget as a post-build surplus.

## Next step and unresolved checks

Preserve the user's revised roads. Keep paused for remaining zoning, utilities and service-readiness checks, then observe staffing, enrollment, emergency response, traffic and financial recovery when the user is ready to run. Do not duplicate west schools/fire/clinic from stale records. Police and education are now physically present on the east side. No game simulation was run to commission them. See [L039](LEARNING.md#l039-city-service-variants-have-distinct-capacity-and-footprints).
