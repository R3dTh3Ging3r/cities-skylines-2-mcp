# Ezra City — northern factories and stepped zoning

October 10, 2026. User requested the available special factories, final useful road additions in the northern industrial area, then industry plus low/high office zoning using the step method. Keep zoning off avenues. User explicitly prefers chaining predetermined bridge actions, checking completed groups rather than interrupting every small step. No wall-clock deadline. Previous parks session archived in [SESSION-21.md](SESSION-21.md).

## Verified construction

Six industrial signature factories were natively accepted, visually checked, uniquely matched by prefab/position, and independently audited for bidirectional local-road access to the collector network:

| Prefab suffix (IndustrialManufacturingSignature) | Name when confirmed in native UI | Center X,Z |
| --- | --- | --- |
| 01 | Vehicle Factory | -3150,3152 |
| 05 | Chemical Plant | -3650,3210 |
| 08 | Native name not recorded | -4436,3088 |
| 02 | Native name not recorded | -4280,3048 |
| 03 | Native name not recorded | -4128,3056 |
| 09 | Oil Refinery | -3890,3228 |

The Oil Refinery became unlocked during this pass; it was reread as unlocked before placement. Do not attribute that progression to a particular action without evidence. Prefabs 04,06,07 remain locked. All six base construction cards/receipts reported zero capital cost; company operation and staffed economic effects have not been tested.

One 470 m Small Road connection at Z3100, X-3570 to-4040, closes the D/E factory loop and provides refinery frontage. Native name Camberwell Street. Three final segments match the successful build receipt, total 470.007 m. Paused cash comparison isolates 4,160 for this road (6,861,236 to6,857,076). No demolition. User was also editing/inspecting the city; later treasury differences are not attributable solely to our construction.

## Zoning result

390 sequential brush actions at radius8 m / step8 m completed without error, in three checked groups. Verified additions:

| Area | Zone | Newly painted cells |
| --- | --- | ---: |
| P local frontages | Industrial Manufacturing | 343 |
| O worker/office loop | Office Low | 76 |
| O worker/office loop | Office High | 77 |

P paint-center strips: X-2456…-2360 and-2168…-2064, Z3648…3680 and3816…3848. O paint-center strips: low X-3040…-2952; high X-2904…-2816; both Z3664…3696. Brush reach extends 8 m beyond centers. Geometry checks protected avenue/highway bands, surface rail, buildings and facility/service reserves; the O west service/queue reserve remains clear. N was already occupied industry and was left alone. Existing P edge industry was not repainted.

Each batch refreshed roads/buildings and required paused frame22446423 before every mutation. Every response was recorded. Do not rerun completed receipts. Zoning readbacks confirm all 343 new industry cells and all153 office cells were empty at the paused audit. Final native zoning-overlay review was deferred because the user was actively navigating transit UI. Current evidence establishes placement, cell counts and screened frontage, not mature companies, jobs or traffic performance.

## Clock, city state and checkpoints

Construction remained paused at frame22446423, native18:03 January2031. The final pre-save read showed population46,675, treasury6,851,092. The user resumed during the save; fresh reads returned running at speed4, frame22455130 at18:51, then population46,796, treasury7,003,206 at18:52. The agent did not start a simulation test or change the resumed speed. Do not describe the final city/save as paused.

Start checkpoint **Ezra-Special-Factories-Start** verified exclusive-readable42,168,150 bytes, SaveGameData41,545,664 bytes. It also preserves the prior parks/depots and intervening user work. Final **Ezra-Northern-Factories-Zoned** verified exclusive-readable42,093,839 bytes, SaveGameData41,671,213 bytes. Save completion is confirmed.

Initial live survey, now historical: monthly balance+3,936,296; industrial demand66 and office99 on0–255 scale; unemployment8.9568%,98 free jobs. Power was fully supplied but production733,311 was below consumption760,573, with battery11,032,191/15,000,000. Do not infer sustainable utility headroom from the positive balance or a single fulfilled-demand sample.

## Next step

Let the user review the factory grounds/zoning; on the next authorized growth pass inspect actual company occupancy, hiring, freight queues and power/battery trend before expanding these blocks. Preserve rail/airport/utility/service space and the three locked-factory opportunities. No new bridge feature was implemented. Raw evidence is ignored .local/sf-*; final audit is .local/sf-factory-final-audit.json and all390 mutation receipts are .local/sf-brush-receipts/.
