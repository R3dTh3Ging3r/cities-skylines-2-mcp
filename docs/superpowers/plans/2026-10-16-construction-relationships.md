# Bounded C1 direct infrastructure relationships

Implementation plan selected October 16, 2026. Preflight inspected source at `ecc4e2bf12416f99563cc2a9b6235ff2b0387d11` on `codex/city-observation-workflow`; checkout was clean. No source edits, tests, native/platform probes, installations, game calls, packaging, publication or PR metadata changes. Root has selected this increment under Eric's continuing roadmap authorization. The requirements below are frozen for implementation; this is partial C1, with native gates unchanged.

## Recommendation and useful outcome

Implement an optional **depth-one direct relationship expansion of existing C1 inventory**, keeping full C1 incomplete. This is dependency-ready after accepted world fencing, O7/T1/M4/M5. Prefer it to an M3 progression endpoint now: native relationship fields already have concrete consumers, while progression tree, point-balance, node identity, unmet requirements and gameplay-settings APIs have no proven producer in this checkout.

The useful workflow is explicit selection -> relationship slots -> geometry of supported referenced targets. For a selected station/depot/building, the assistant can see the actual direct child-object, subnet, subarea and installed-upgrade references, inspect related building root OBBs/network cubics, and distinguish missing, unsupported and unmeasured parts. For a T1 stop, which C1 currently labels unsupported as a geometry root, it can read the direct Owner/Attached parent facts and obtain the owner building or attached edge geometry. This is useful inspection before a placement proposal, not merely a list of unknown placeholders. A deliberate subsequent request may inspect a chosen related target; the server does not recurse automatically.

These facts do not identify every platform or exterior connector, certify a complete facility envelope, establish directed connectivity/capacity, explain unlock requirements or authorize transformation. Leave those fields unavailable. No writer, preview, family compatibility inference or M1 capacity substitute belongs here.

## Source-backed comparison

| Candidate | Actual current evidence | Assessment |
| --- | --- | --- |
| C1 direct infrastructure links | Explicit world/versioned geometry reader; concrete Owner/Attached and four instance-buffer member fields; reusable root geometry and O7 transport | Strongest useful bounded increment. Needs bounded traversal orchestration and actual direct-route output/client caps, but no new native method discovery. |
| M3 useful progression/settings reads | CitySystem.XP, enableable Locked state, tile-permit milestone-left boolean | XP already exists in M4. Lock-state-only wrappers mostly repeat existing prefab/asset reads. Current/next stable milestone keys, spendable points, development tree, prerequisite evaluation and named gameplay settings require source/API evidence before a useful new producer. Do not publish an XP-plus-unknown-tree scaffold as M3 completion. |

M3 reads are independent of purchase gates under retained decision156; purchases still require O1/O3/O5 and applicable O6 evidence. Those gates do not block a proven read, but they do not supply missing tree/settings APIs. No native point getter or normal purchase action is established in the current code. Never infer spendable points from XP, label any locked asset as a specific unmet milestone, toggle Locked components, simulate a tick or dismiss a modal.

## Exact source anchors

Paths below are repository-relative; line numbers refer to the inspected SHA.

| Native fact / integration | Anchor | What it establishes and what it does not |
| --- | --- | --- |
| Exact root identity and liveness | `CS2MCP.Bridge/NativeConstructionReader.cs:20` | Constructs Entity from full index/version, checks Exists/Deleted/Temp. Current early unsupported-kind return at29 prevents even Owner facts for stop/prop roots; relationship inspection must be separate from geometry support. |
| Owner | `NativeConstructionReader.cs:31`; `Transit/NativeTransitReader.cs:171` | `Game.Common.Owner.m_Owner` is a direct ref, not a certified ownership-family hierarchy. |
| Attached parent | `Transit/NativeTransitReader.cs:177` | `Game.Objects.Attached.m_Parent`, exact parent state, and Edge presence. Attachment is not a service-connectivity or exterior-gate proof. |
| Instance child object | `RequestHandlers.Transit.cs:1133` | Read-only `Game.Objects.SubObject` buffer and element `.m_SubObject` at1138/1140. Do not call `TryFindStopInStation`: its1146 fallback scans all stops and follows owner chains. |
| Instance installed upgrade | `CreateDefinitions.cs:711`,717–721 | `Game.Buildings.InstalledUpgrade` buffer and element `.m_Upgrade`; same field at198–200. Evidence of stored refs, not upgrade compatibility/capacity/current operational effect. |
| Instance subnet | `CreateDefinitions.cs:1067`,1072,1084 | `Game.Net.SubNet` buffer and element `.m_SubNet`. Subnets can be nodes or edges; unsupported node geometry must remain explicit. |
| Instance subarea | `CreateDefinitions.cs:1347`,1352–1356 | `Game.Areas.SubArea` buffer and element `.m_Area`. Area nodes/triangles require further traversals and are excluded from this slice. |
| Native lookups already wired | `BridgeToolSystem.cs:630`–635; `CreateDefinitions.cs:119`–133 | Distinguishes instance relationship buffers from similarly named prefab-definition buffers. Use instance buffers only. These mutation codepaths are provenance, never an invoked helper. |
| Network target geometry | `NativeConstructionReader.cs:48`–68 | Curve.m_Bezier a/b/c/d, Curve.m_Length, Edge.m_Start/m_End, finite width and layer metadata. Edge orientation is not lane direction; refs do not establish valid committed connectivity. |
| Building target geometry | `NativeConstructionReader.cs:70`–82 | Instance Transform position/rotation and prefab ObjectGeometryData bounds feed production RootBounds. Reuse this calculation for supported referenced buildings, not lot size or envelope union. |
| Asset key | `RequestHandlers.Transit.cs:1282`–1289 | PrefabRef.m_Prefab -> PrefabSystem.GetPrefab<PrefabBase>().name. Existing source lookup; bounded name output, no new native lookup API. Internal getter latency/native name stability remains unverified. |
| Ready-world orchestration | `Core/ConstructionInventory.cs:50`–68 | Validates shared WorldIdentity before/after every root and publication. Extend the same authority around headers/slots/target reads; do not add another epoch producer. |
| Direct route | `RequestHandlers.Construction.cs:6`–19; `RequestHandlers.cs:67` | Existing handler feeds the same reader, but direct JSON currently uses uncapped BridgeResponse.Json. Expansion must use capped actual serializer before publication. |
| Actual direct query ingress | `HttpBridgeServer.cs:157`–170; `Core/ConstructionInventory.cs:13`–48 | Dedicated strict percent decoding detects duplicate decoded keys before dictionary overwrite. Current exactly-three-key parser needs a narrow optional flag, preserving4096-character query/normal header bounds. |
| Shared O7 inventory binding | `Core/ObservationCatalog.cs:126`–142; `RequestHandlers.Observations.cs:15` | Fixed inventory name builds compact query and invokes the same producer. Add the identical literal expansion mode to admission/schema; no new name, endpoint or arbitrary dispatcher. |
| Existing capped serialization | `ObservationWire.cs:23`–68 | Capped actual UTF8 finite serializer, Standalone256KiB, Item256KiB, Batch4MiB. Reuse it; `RequestHandlers.Metrics.cs:42`–53 demonstrates direct-route RESULT_TOO_LARGE fallback. |
| Progression evidence actually available | `RequestHandlers.cs:35`–48,264–283; `Core/DiagnosticMetrics.cs:69` | Post-load lock warming, correct HasComponent && IsComponentEnabled for Locked, CitySystem.XP. XP already consumed by M4. |
| Milestone-related tile fact | `RequestHandlers.MapTiles.cs:120`–126 | MapTilePurchaseSystem.GetAvailableTiles and IsMilestonesLeft. This is a permit boolean/counter, not current/next milestone identity or development points. |
| Existing selected prefab lock read | `RequestHandlers.Placement.cs:101`–109 | PrefabInfo reports IsLocked with known geometry/placement metadata. No tree/point/prerequisite producer is hidden in that result. |

Read original C1 at `2026-10-11-bridge-construction.md:78` onward, M3 at `2026-10-11-bridge-management.md:76`, the bounded explicit-geometry brief, larger observation workflow outlook, and retained construction/management rulings. Retained rulings require game-thread reads, shared world observations, partial unknowns, source-backed metric meanings, no fake mutation descriptor and no manufactured enablement. Full C1 spatial/cursor/full-envelope tests and CON-01 remain open.

## Required bounded contract

Extend the existing strict request with **one optional literal** `relationships:"directDepth1"`. Omission preserves current behavior. GET encodes the same flag in the existing compact query; O7 passes it in `construction.inventory.args`. Reject unknown values, booleans, depths, recursion/family/spatial/cursor options and unexpected keys before native work. Do not add adjustable budgets or fake pagination.

Keep the requested root `entities` and `requestedEntities` arrays intact and ordered. Successful expansion adds one `relationships` object with:

- `scope:"directDepth1"`, explicit limits, read/coverage counters and native-notRun limitations.
- A per-root fixed source summary: Owner, Attached, SubObject, InstalledUpgrade, SubNet and SubArea presence/count/status, distinguishing component/buffer absence from zero-length and null slots.
- Ordered links `{source:EntityRef,kind,slot,target:EntityRef|null,status,reason,targetIndex?}`. Preserve every admitted buffer/scalar slot, including duplicates, self refs, null/missing/deleted/temp/recycled/unsupported targets. `kind` names the concrete source relationship; it does not classify a platform/family/connector.
- A deduplicated first-seen `targets` table containing compact existing geometry/asset/liveness facts for supported live building/network targets and explicit unavailable reasons for other targets. Link indices avoid duplicating full geometry for repeated refs. Exact identity includes world/index/version; do not deduplicate by index alone. Reuse root geometry when a target is already an explicit root.

The DTO is bridge-owned, not a proposed native API. Unsupported live explicit roots may have useful direct relationships even when their own geometry remains unavailable. Related targets never have their relationship buffers traversed unless they independently appear in the caller's explicit root list. Existing rich family/upgrades/envelope/connectors/cost/prerequisite/capacity placeholders remain unavailable; direct installed-upgrade reference rows must not be described as the complete semantic upgrade state. Qualify its placeholder reason accordingly when expansion is selected.

Per-source completeness means **slot coverage only**: a present empty buffer has observedCount=visitedCount=0 and coverageComplete=true; a missing component/buffer or unavailable root has explicit source status/reason and unknown count, never a made-up empty complete family. Every covered null/duplicate slot is still a covered slot, while target availability/geometry support is reported separately. All known slots being covered never implies complete facility/platform/approach coverage. The proposed over-budget mode refuses the whole expansion rather than returning a successful partial prefix.

### Work and output limits

Fixed bridge budgets, not native game limits:

1. Preserve1–32 distinct explicit same-world full refs. At most32 root visits; each live root has at most two scalar-source checks and four buffer-header checks. Check liveness before relationship native access; deleted/temp/stale roots read no relationship entries. Geometry support does not gate relationship facts for a live unsupported root.
2. A **global128 relationship-slot cap across the whole request**, including present scalar Owner/Attached slots and every buffer element. A null or duplicate slot consumes the same work budget as a live unique ref. Bound actual element access, not just returned rows.
3. Use a header/count pass before any buffer element iteration. Sum counts using safe widened arithmetic. If the combined slots exceed128, return a bounded RESULT_TOO_LARGE/refusal before any buffer-element or related-target read, with guidance to select fewer roots or omit expansion. Do not read a prefix, silently truncate, allocate/copy the whole buffer, or return a fabricated complete list. Missing/deleted roots can still have ordinary root unavailable facts during a successful bounded request.
4. At most128 distinct **additional** referenced-target geometry visits, with an exact-ref cache including the32 roots. Each visit reuses fixed liveness/PrefabRef/geometry component access only; no target's relationship enumeration. Scalar owner metadata inside the existing geometry DTO may remain a pointer and does not trigger another visit. Total root+additional target visits <=160; actual ECS calls per visit remain fixed and must be documented/counted separately from visits.
5. Fixed relationship source order, caller root order and native slot ordinal provide deterministic within-observation rows. Do not promise cross-frame snapshot stability. Preserve null/raw bad-reference evidence without manufacturing a valid positive EntityRef or resolving a recycled index to another version.
6. Cap source-derived names/layer strings before serialization; retain existing256-character asset keys and define a conservative128-character enum-name bound. Invalid/nonfinite geometry remains unavailable. Max32 root summaries,128 links and128 compact target rows bound object shape independently of UTF8 cap.
7. **Actual finite UTF8 result cap256KiB** for both direct inventory and O7 item, including observation/IDs/unknown fields/wrappers; group cap remains4MiB. Larger complete results become explicit RESULT_TOO_LARGE, never omitted links/targets. A valid maximum-count request need not fit every worst-case string. A caller may choose fewer roots. Reuse ObservationWire.Standalone on the direct route and the original O7 Item/Batch caps.
8. Upgrade the actual typed inventory client from legacy getJson to the existing bounded all-status loopback reader, capped256KiB and strict schema/identity/cardinality/geometry/link validation. Keep omitted-mode compatibility in successful baseline payload fields. Test full returned request/root refs and partial/notRun evidence before exposing the result. MCP text output must also fit a declared256KiB result cap including escaping, with a bounded error fallback; avoid unbounded generic pretty-print for expanded inventory.

The direct client/serializer changes are necessary integration, not a second inventory producer. Do not register a new endpoint or change tool count. O7 admission retains immutable original args and referenced-world discovery. All header/slot/target access occurs on the game thread, fenced before/after each unit and again before publication. Item counts remain one O7 read slot; the concrete inner work bounds prevent multiplying that into an unbounded scan. Native calls cannot be preempted: fixed source operations do not certify hard wall time, hidden getter latency or actual game performance.

## Implementation and meaningful evidence proposal

Source file set: `Core/ConstructionContracts.cs`, `Core/ConstructionInventory.cs` and a small BCL `Core/ConstructionRelationships.cs` if separating orchestration improves review; `NativeConstructionReader.cs`; `RequestHandlers.Construction.cs`; `Core/ObservationCatalog.cs`; TS construction query/contracts/tool and shared observation contracts/client. Update EndpointCatalog's partial-support reason/documentation if needed, not its endpoint list. No new native producer, global query, management catalog, writer or M5 collection metric.

Tests should own actual production seams:

- Canonical `ConstructionInventoryTests.cs`/named suite: strict optional-mode parsing in actual URL decoder, duplicate decoded flags, immutable admission, world/readiness changes during headers/slots/target reads, empty/32 roots,128 versus129 slots, counts overflow, missing buffers versus empty, all-null and duplicate slots, roots also targets, same index/different version, self/cyclic references with no recursion. Count header/element/target callbacks separately; assert over-cap counts permit **zero element/target reads** and every successful requested slot appears once.
- Link actual native relationship adapter into a focused binding harness with minimal ECS-shaped stubs if practical, verifying the production members/order and no use of prefab buffers or scanning helpers. Otherwise document the exact native-binding verification limit; do not replace it with a copied fake adapter and claim native proof. Stubs cannot prove installed DLL symbol binding or runtime validity.
- Actual RequestHandlers/O7 bindings: the fixed inventory delegate consumes the new normalized flag and same reader. Existing routing still queues main-thread work; shared world authority fences publication. Do not merely test a parallel policy function.
- Response/serializer tests: exact production ObservationWire/standalone handler, long escaped/multibyte worlds/asset keys, many individually valid rows exceeding256KiB, nonfinite values, bounded errors and O7 exactly-one ordered result. Direct and O7 decoded data must agree for the same source fixture.
- Node actual SDK/tool/HTTP tests: literal mode advertised correctly with original required world/entities, no arbitrary family/depth keys, malformed/oversized/unknown inputs make zero HTTP calls, GET shared encoding and POST args agree, bounded streamed success/error responses do not trust Content-Length, full refs/slot cardinality/target indices reject malformed output. Exercise actual SDK schema/large-error behavior, preserving the M5 lessons.
- Consumer fixture: a selected live stop has Owner->station and Attached->edge; selected station has a child building, subnet edge, subarea and installed upgrade. Assertions show actual referenced building OBB/full cubic reuse and explicit unsupported area/object geometry, not a fabricated full envelope or functional platform classification. Include child->root cycles and a missing upgrade slot.

After root GO, follow meaningful red/green for production orchestration, actual transport and serializer, compile canonical linked net472 suites with SkipDeploy where applicable and execute exact linked sources in the existing portable harness. Run only relevant changed Node/native-free integration suites; no known missing PowerShell/game/build-environment repetitions. Preserve exact commands/counts and independent review before publication. No tests were run during this preflight.

## Remaining owner/native gates

No new external owner decision or native API discovery blocks the recommended source slice. The exact option/slot/refusal contract above is frozen by root; budgets are bridge safety limits, not discovered native restrictions. Native G2/CON-01 and installed native compilation, missing-reference behavior, true facility/platform/exterior-connector completeness and runtime/performance parity remain notRun and cannot be certified in cloud.

M3's next useful step remains a future source/API investigation establishing stable milestone/tree identities, available-point balance, read-only gameplay-setting fields and requirement evaluation. Neither native purchase nor forced unlock is an acceptable substitute. Packaging and PR6 metadata remain separately paused/approval-owned; do not infer publication authority from this design recommendation.

## Execution and integration rulings

Root accepts this bounded scope at base `ecc4e2bf12416f99563cc2a9b6235ff2b0387d11`.
Use the existing feature checkout and preserve all prior draft changes/snapshots.
One implementer owns source; one independent reviewer checks specification and
quality plus the complete increment integration. No new workers are needed.
PR6 description remains pending Eric's specific approval; do not update it.
No installation, package, merge, running-game access or native mutation is authorized.

Omitted expansion preserves existing payload fields and native access pattern;
an additive nullable relationship result is acceptable if explicitly represented
in both schemas and fixtures. Root geometry reused in the target cache counts
as zero additional target visits. Headers may read source liveness/counts only;
validate their combined counts before any relationship slot or additional target
dereference. Preserve exact per-source slot counts and target-state uncertainty.
Malformed native references must not become positive valid EntityRefs.

The 256KiB caps are independent: actual direct HTTP serialization, O7 item and
final MCP text wrapper each enforce their own complete-result bound. Count-based
acceptance never guarantees that pathological strings fit the byte limit. Errors
from actual SDK validation must also remain bounded; preserve advertised required
fields and optional literal mode. Reuse a narrowly shared bounded-schema helper
only if needed, with relevant M5 regression coverage when shared code changes.
Do not add a second native producer or duplicate pure/native policy in tests.

Prior acceptance tests remain evidence for unchanged code. Final changed-source
checks must include applicable construction, O7, serialization and actual MCP
paths. Native source syntax/binding-stub checks cannot certify installed symbols
or runtime behavior. Avoid new synthetic native harnesses unless they establish
a useful production seam beyond existing linked tests. Record every check and
its exact final SHA; no unused framework or broad roadmap implementation.

Final source acceptance: initialad3735dc187576a67336ce44128f32243ad3ec94 plus
R1 correctiond7df128c16f7e07b460fdd53f84efc8eafdac34f passed independent
specification, quality/security and complete-increment integration review.
The source is preserved on codex/construction-relationships, stacked on the
accepted PR6 checkpoint ecc4e2b. PR6's exact prepared description was subsequently
approved by Eric and published with verified readback; that blocker is closed.
See ../../handoff/C1-RELATIONSHIPS-2026-10-16.md for final evidence and limits.
