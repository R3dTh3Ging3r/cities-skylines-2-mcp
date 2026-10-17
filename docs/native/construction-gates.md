# Construction geometry — bounded partial C1

`CON-01`, G1 and G2 are **notRun**. No native signature compile, live geometry
comparison, complete facility inventory or construction writer is certified by
this source increment. Full C1 in the construction plan remains unchecked.

`GET /v2/construction/inventory` / `cs2_construction_inventory` inspect **1–32
explicit versioned entities** from the same observed world. The bridge rejects
empty/oversized lists, duplicate indices (including different versions), unsafe
world epochs and unsupported options. It never silently truncates. It performs
one geometry read per selected ref, with fixed component/prefab lookups. Omitted
relationship mode performs no relationship header/buffer reads. No entity query,
recursive owner traversal, family expansion or pagination is added. Missing/recycled,
deleted, temporary and unsupported roots retain individual unavailable entries.

| Supported source projection | Evidence and limits |
| --- | --- |
| Full finite cubic a/b/c/d, world XYZ, native length and versioned edge ends | Existing `RoadGraph` reads `Game.Net.Curve.m_Bezier/m_Length` and `Edge.m_Start/m_End`; edge orientation is not lane direction. |
| Actual owner ref | Existing readers use `Game.Common.Owner.m_Owner`; no owner component stays null with a reason. Optional direct relationship mode can read one stored owner target; no recursive descendants. |
| Prefab assetKey | Existing `PrefabNameOf` uses `PrefabRef.m_Prefab` and `PrefabSystem.GetPrefab<PrefabBase>().name`. Names over 256 characters stay unavailable instead of being truncated. |
| Root building OBB | Existing `BuildingsNear` uses instance `Game.Objects.Transform.m_Position/m_Rotation`; `PrefabInfo` reads `ObjectGeometryData.m_Bounds.min/max`. Center includes the rotated local midpoint; half-extents use local geometry, axes use instance rotation. No lot-size substitution. |
| Network width and layer names | Existing `PrefabInfo` reads `NetGeometryData.m_DefaultWidth`, `NetData.m_RequiredLayers/m_ConnectLayers`. Missing/invalid width stays null with a reason. These are prefab metadata, not complete clearance or connectivity proof. |

`rootBounds.scope` is `rootGeometryOnly`. Family, compatibility, installed
upgrades, child/facility/platform/approach envelopes, connectors, costs,
prerequisites, capacity effects, elevation mode and lane directions remain null
with explicit reasons. Direct installed-upgrade reference slots are distinct from
the still-unmeasured full upgrade description. `worldBounds` is null / `notMeasured`. The snapshot labels
its explicit requested refs, partial support and `CON-01:notRun`; `nextCursor` is
null and `truncated` is false because every requested ref gets an entry.

The game-thread reader validates the shared world/readiness authority before
sampling, around each root and after end-frame capture. The shared Observation
contains `data`, `world`, `sampledAtUtc` (UTC at the end of sampling), nullable
`startFrame`/`endFrame`, `freshness` and `warnings`. This producer supplies both
frames and `freshness="current"` after ready-world validation. Warnings identify
partial/root-only coverage and native validation notRun; differing frames add
`multiFrameReadNotAtomic`. Multi-frame reads are never atomic; equal frames also
do not prove an immutable ECS snapshot or safety for later placement. An
intervening load/readiness loss rejects
the observation. The listener only decodes bounded input; inventory dispatch uses
the existing main-thread queue. The three operation routes remain disabled.

## Direct relationship source evidence and limits

Select `relationships:"directDepth1"` on the existing inventory or O7 item to
inspect fixed instance Owner/Attached/SubObject/InstalledUpgrade/SubNet/SubArea
sources. Concrete members are Owner.m_Owner, Attached.m_Parent,
Game.Objects.SubObject.m_SubObject, Game.Buildings.InstalledUpgrade.m_Upgrade,
Game.Net.SubNet.m_SubNet and Game.Areas.SubArea.m_Area. Existing Transit readers
and CreateDefinitions provide field provenance; no mutation helper is invoked.
Prefab-definition buffers, global queries, area nodes/triangles and target
relationship traversal are excluded.

An expanded request first reads at most32 root liveness/header sets, each with
two scalar presence checks and four buffer-presence/length checks. It sums all
present scalar/buffer slots using widened counts and refuses above128 before
root geometry, slot values or additional targets. Successful requests read each
slot exactly once, retaining null/duplicate/self/invalid/stale refs separately.
At most128 additional unique exact targets use the original geometry reader;
explicit roots are reused. These160 possible geometry visits are distinct from
header liveness checks and fixed ECS operations per visit; they are not a count
of all ECS calls or a hard native latency guarantee. Owner/asset lookups can have
opaque native getter costs. Prefab names cap256 characters, layer names128.

Per-source presence/count/visited/coverage evidence distinguishes absent or
unavailable sources from a present empty buffer. Complete source-slot coverage
never certifies complete facility/child/platform/approach geometry or connector
roles. Unsupported stop/object/area/node geometry remains unavailable, while a
live selected stop can still expose a direct owner building or attached edge.
Every phase validates the original shared world/readiness, and publication keeps
the existing native-notRun/multi-frame caveats.

The exact NativeConstructionReader and RequestHandlers.Construction sources are
linked into the existing response test project against minimal ECS-shaped
boundary data. This checks concrete field selection, read-only buffer access,
root geometry reuse, counts and no recursion. It cannot establish installed
Game/Unity signatures or runtime/native validity. Actual Newtonsoft serialization
checks the direct256KiB and O7 item/group caps. The strict typed Node client and
MCP tool have independent streamed/wrapper byte limits and semantic validation.

Deferred CON-01 evidence must compare complete underground/platform/approach
geometry and exterior connector semantics with native panels in a disposable
fixture, and establish a genuinely bounded spatial traversal plus edit-safe
snapshot/cursors. The pure tests in this slice do not cover bowed-curve spatial
selection or pagination. Transformations depending on these fields remain open.

## Computed selected-network diagnostics — partial C5

The optional `geometryDiagnostics:"sampledCubicV1"` inventory/O7 mode adds a
consumed BCL calculation over the existing native root cubic, with no new native
getter. It returns17 ordered t/XYZ samples per available curve, at most544 across
32 roots, plus per-quantity grade/3D-curvature/radius support/reasons and sampled
extrema/counts. Related target geometry is not profiled. Omitted mode preserves
prior reads; native length remains separately reported.

Scaled derivative and norm arithmetic guards representability loss, stationary
and vertical tangents; nonzero curvature underflow does not become supported
straightness. A finite reciprocal can remain available when the other quantity
overflows. Strict actual C# serializer and TS direct/O7 consumers preserve nulls,
reasons, counts, root identity and extrema provenance under existing byte caps.

This is computed double-precision evidence in native worldXYZ coordinates, Y
vertical. Grade is dimensionless; curvature/radius physical engineering units
are unverified. Between-sample extrema are unmeasured. No prefab design limit,
terrain, clearance/collision, lane/vehicle, native-preview or feasibility claim
is supplied. The linked native-boundary tests prove consumed field selection,
read reuse and serialization, not installed Game/Unity binding, game timing or
performance. Full C5 snapping/splits/offsets/construction and G1–G4 remain open.
