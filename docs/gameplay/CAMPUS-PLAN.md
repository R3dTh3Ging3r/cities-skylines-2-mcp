# Ezra City education campus

**Status: interrupted and not accepted.** On October 10 the user said the developing layout was being messed up and took over. All remaining steps below are historical proposals, not instructions to continue. Refresh the city and obtain a clear visual-layout direction before further work. At handoff University and all three upgrades were built; Technical University base was built without upgrades. No Medical University, extra paths, parking or row housing was built. Last cash 2,720,861, paused; no post-build save. The eastern surface roads had already been reshaped into a boulevard at z=920 and rear loop at z=1170 connecting to Girard at z=1180. See SESSION.md for exact handoff details.

October 10, 2026. The user wants a coordinated campus in the marked western expansion area: University, Technical University and Medical University, including their upgrades, with a few row-house clusters resembling campus housing, parks, pedestrian paths, parking and suitable roads. The user approved staging construction as the city earns the remaining money. There is no wall-clock deadline.

## Confirmed first phase

University01 is built at approximately **(-2913.025, 1078.639)**, rotation **-89.379°**, in the western parcel. Native placement confirmed all three upgrades fit: Extension Wing, Library across Woodland Street to the south, and Park beside the Library to its east. The detached library and park arrangement is now an observed fit, not merely reserved space.

| University component | Capital | Native listed monthly maintenance |
| --- | ---: | ---: |
| Main university | 1,500,000 | 100,000 |
| Extension Wing | 375,000 | 37,500 |
| Library | 250,000 | 47,500 |
| Park | 50,000 | 20,000 |
| **Total** | **2,175,000** | **205,000** |

The native panel showed **17,500 student capacity** after the upgrades. Listed capacity and maintenance are not measured enrollment or fully staffed operating cost. Placement occurred while paused; education results, pedestrian access in operation and traffic performance remain untested.

Last parent-agent observation when this plan was written: **treasury 4,320,861**, paused at approximately frame 18.28 million. Refresh this before further spending. The initial campus survey measured population 35,343, treasury approximately 6.469 million and a positive monthly balance of approximately 1.224 million; these are historical pre-construction readings, not guaranteed current income.

## Site and remaining layout

The marked area is approximately x=-3040 to -1950 and z=466 to 1340. The existing north–south highway near x=-2665 divides a narrow western parcel from a wider eastern parcel. Woodland Street crosses beneath the highway near z=861. Preserve this connection and the surrounding highway geometry.

- **West:** retain the placed University and its confirmed southern Library/Park arrangement. Complete connected walking routes and small row-house groups in the remaining usable pockets. Keep their frontage out of the university entrance and highway approaches. Row houses are ordinary residential zoning with a campus appearance; they are not a special student-housing service.
- **Northeast:** reserve a broad east–west frontage for the Medical University, with nearby room for its Research Facilities and Practice Clinic. Its long 496m frontage drives the street layout. Place its full upgrade arrangement before filling nearby land with housing or parks.
- **Southeast:** reserve the Technical University's 424×304m footprint. Its extension wing and workshop are expected to fit within its base lot, but verify native upgrade previews before treating this as confirmed.
- **Eastern campus streets:** the existing Woodland surface dogleg crosses near z=855 then turns south near x=-2138. The working concept moves the campus cross-street nearer z=920, connects it to the existing tunnel approach near x=-2528, and opens larger university plots. This is a candidate alignment, not an approved collision-free placement result. Refresh the road graph and check slopes, utilities, buildings and full upgraded footprints before changing it.
- **Access and landscape:** use local campus streets for entrances and peripheral parking; connect universities, housing, parks and adjacent neighborhoods with direct pedestrian routes. Keep enough clear frontage for later parking and transit access. Preserve useful green space rather than filling every leftover strip.

## Live asset specifications

The bridge's lot dimensions use 8m cells: width follows the building's front; depth runs behind it. Rotation changes their world-axis orientation.

| Prefab | Base or separate footprint | Live capital price |
| --- | ---: | ---: |
| University01 | 320×224m | 1,500,000 |
| University01 Library | 96×96m | 250,000 |
| University01 Park | 144×144m | 50,000 |
| TechnicalUniversity01 | 424×304m | 1,600,000 |
| MedicalUniversity01 | 496×144m | 2,400,000 |
| MedicalUniversity01 Research Facilities | 128×64m | 480,000 |
| MedicalUniversity01 Practice Clinic | 120×56m | 480,000 |

All seven were unlocked in the campus survey. Technical and Medical universities are unique assets; refresh the building inventory before purchasing either. A compact Medical arrangement with the two detached additions behind the main building needs roughly 496×208m before streets and walking space, but the actual native radius, access and collision checks decide the final layout.

ParkingLot01/02/03 were respectively 40×40m at 11,000, 56×40m at 15,000, and 80×64m at 35,000. **All parking prefabs were locked** in the survey. Use the normal native unlock if available; do not force placement. PedestrianPathWide01 and Pedestrian Street Small are both 8m wide, but a path is not a road frontage replacement.

## Financing and construction sequence

Historical reference prices for the remaining inline upgrades are Technical Extension Wing 320,000, Technical Workshop 320,000 and Medical Extension Wing 480,000. Combined with the live base and detached-upgrade prices, this gives a **provisional remaining university budget of 6,080,000**: Technical 2,240,000 and Medical 3,840,000. At the last 4,320,861 treasury reading, the gap is **1,759,139 before roads, paths, parks, parking and a cash reserve**. Confirm each inline price in the current native panel; the historical table alone is not a spending receipt.

1. Finish surveying the eastern layout and resolve the full upgraded footprints before committing its roads.
2. Build the campus access roads and Technical University with its upgrades while paused, allowing for the final Medical footprint and support features.
3. Check utilities, access and the current operating balance, then use the authorized earning stage to accumulate the remaining capital. Do not extrapolate completion time from one pre-campus balance reading.
4. Build the Medical University and all upgrades when the measured treasury covers the verified cost plus the remaining support work and a practical reserve.
5. Finish restrained row-house zoning, parks, pedestrian connections and parking; verify actual connectivity, then observe use and operating costs during simulation.
6. Save and independently verify the finished checkpoint. Record any unfinished items explicitly.

The historical maintenance estimates for Technical with both upgrades and Medical with all three are 187,500 and 302,500 per month respectively. Together with the confirmed University card, they suggest approximately **695,000/month listed maintenance for all three**, before staffing effects and other campus assets. These estimates require current native confirmation. The earlier 1.224 million surplus does not prove the completed campus will preserve that surplus.

## Bridge limitations and evidence

Current generic building placement sets position and rotation but does not supply an upgrade owner/attachment. Apply university upgrades through the native building panel; do not place their prefabs as independent buildings. Roadside `dryRun` reports geometry only and does not validate collisions. Native sub-building placement must still confirm ownership, radius and pedestrian or road access.

Official developer guidance permits detached sub-buildings within their defined radius, with the appropriate access. The western Library/Park placement confirms a usable arrangement in this city. [Developer explanation](https://www.paradoxinteractive.com/games/cities-skylines-ii/news/dev-diary-economy-part-two). Older upgrade prices and upkeep used solely for the provisional budget come from the [historical service table](https://wiki.biligame.com/csl2/Service_buildings); native values take precedence.

Raw live prefab evidence is in ignored `.local/campus-review-info-*.json`; the original site survey is `.local/campus-roads.json`. Treat those road entity IDs as historical after further construction. Preserve the separately verified pre-construction checkpoint recorded by the parent session; this document does not establish a new save verification.
