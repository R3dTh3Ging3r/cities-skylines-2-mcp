# Construction geometry — bounded partial C1

`CON-01`, G1 and G2 are **notRun**. No native signature compile, live geometry
comparison, complete facility inventory or construction writer is certified by
this source increment. Full C1 in the construction plan remains unchecked.

`GET /v2/construction/inventory` / `cs2_construction_inventory` inspect **1–32
explicit versioned entities** from the same observed world. The bridge rejects
empty/oversized lists, duplicate indices (including different versions), unsafe
world epochs and unsupported options. It never silently truncates. It performs
one root read per selected ref, with fixed component/prefab lookups, no entity
query, owner traversal, family expansion or pagination. Missing/recycled,
deleted, temporary and unsupported roots retain individual unavailable entries.

| Supported source projection | Evidence and limits |
| --- | --- |
| Full finite cubic a/b/c/d, world XYZ, native length and versioned edge ends | Existing `RoadGraph` reads `Game.Net.Curve.m_Bezier/m_Length` and `Edge.m_Start/m_End`; edge orientation is not lane direction. |
| Actual owner ref | Existing readers use `Game.Common.Owner.m_Owner`; no owner component stays null with a reason. Owner descendants are not read. |
| Prefab assetKey | Existing `PrefabNameOf` uses `PrefabRef.m_Prefab` and `PrefabSystem.GetPrefab<PrefabBase>().name`. Names over 256 characters stay unavailable instead of being truncated. |
| Root building OBB | Existing `BuildingsNear` uses instance `Game.Objects.Transform.m_Position/m_Rotation`; `PrefabInfo` reads `ObjectGeometryData.m_Bounds.min/max`. Center includes the rotated local midpoint; half-extents use local geometry, axes use instance rotation. No lot-size substitution. |
| Network width and layer names | Existing `PrefabInfo` reads `NetGeometryData.m_DefaultWidth`, `NetData.m_RequiredLayers/m_ConnectLayers`. Missing/invalid width stays null with a reason. These are prefab metadata, not complete clearance or connectivity proof. |

`rootBounds.scope` is `rootGeometryOnly`. Family, compatibility, installed
upgrades, child/facility/platform/approach envelopes, connectors, costs,
prerequisites, capacity effects, elevation mode and lane directions remain null
with explicit reasons. `worldBounds` is null / `notMeasured`. The snapshot labels
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

Deferred CON-01 evidence must compare complete underground/platform/approach
geometry and exterior connector semantics with native panels in a disposable
fixture, and establish a genuinely bounded spatial traversal plus edit-safe
snapshot/cursors. The pure tests in this slice do not cover bowed-curve spatial
selection or pagination. Transformations depending on these fields remain open.
