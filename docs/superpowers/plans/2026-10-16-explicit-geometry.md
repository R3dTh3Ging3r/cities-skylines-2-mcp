# Bounded partial C1 — explicit geometry inspection

Proceed only after O2 world producer/source review is accepted. This is a small
useful read-only increment of existing C1, not full C1 completion. Read C1 and its
retained construction decisions plus /workspace/handoffs/c1-preflight-oct16.md.
No game, installation, save, merge, configuration or allowlist changes.

## Outcome and scope

An assistant can inspect up to 32 explicitly selected live entities in an
observed world before proposing placement: network curves with versioned ends
and root-building oriented geometry bounds. It can distinguish those bounds
from unmeasured complete facility, child, platform and approach envelopes.

Use C1's GET `/v2/construction/inventory` and `cs2_construction_inventory`, but
advertise/document the restricted explicit-entity scope and partial support.
Only nonempty explicit EntityRefs, all in the same caller-observed WorldRef, are
accepted; 1–32 distinct refs per request. Require exact integer index/version,
safe epoch and bounded boot ID; reject duplicates, stale world, invalid limits,
unsupported spatial/cursor/family-expansion options before any native reading.
Do not silently truncate input or accept a worldless caller. Encode GET through
the existing shared encoder/typed client with a bounded compact query contract.
Use shared WorldRef/EntityRef/Observation shapes, no second identity authority.

O2 review integration finding: HttpBridgeServer.Dispatch currently routes every
`/v2/*` path to HandleOperationRequest. Narrow this to the three fixed operation
routes so inventory enters the normal main-thread queue; test the actual routing
seam. The queue auto-fences only writers, so this reader must explicitly validate
caller WorldRef/readiness before and after reads. Read native state on no listener
thread. Preserve disabled operation routes and their tests.

## Production reader

Execute on the existing game-thread dispatch only. Validate world and readiness
before reading; capture observation start/end frame and UTC; check world again
before returning. No ToEntityArray/global scan, native tools, asset instantiation,
temp definitions, changes or fake pagination. Bound the actual entity work, not
just returned rows. Missing/deleted/temp/recycled/unsupported entities must have
explicit per-entity unavailable reasons; never substitute a neighboring entity.

Return full finite cubic a/b/c/d, versioned start/end entity refs, actual owner
if available and source-proven metadata for networks. Edge direction is not lane
direction. For building instances, compute root OBB from actual prefab local
ObjectGeometryData bounds and instance Transform rotation/position. Return
axes/center/half-extents with a clear rootGeometryOnly scope. Missing geometry
is null with reason. Do not infer building geometry from lot size.

Preserve existing PrefabInfo/RoadGraph fields and defaults. Provide assetKey and
owner from supported existing fields. Unknown family compatibility, installed
upgrade/facility/connector envelopes, costs/prerequisites or capacity effects
must be null/unavailable with reasons. worldBounds remains null/notMeasured;
there is no claim of complete city or facility coverage. Snapshot returned scope
must identify explicit IDs, nextCursor=null, no silently omitted requested refs.
Add exact portable examples, supported/deferred fields and CON-01 notRun status.

## Test and review

Test first: actual bounded request validation (1/32 accepted, 0/33/duplicates,
malformed/nonfinite/unsafe values rejected), stale world, unavailable versus
zero, rotated/local-offset root-bound computation in a production pure seam.
Use counted fake entity access to show rejected/stale requests read nothing and
valid input never exceeds 32 root-entity lookups. Node MCP tests exercise actual registration
and encoded world/entity parameters, no real bridge. Add to canonical net472
runner and explicit Node test list. Cloud may execute exact linked BCL tests via
a temporary net8 harness, with target limits explicit; native compile and live
geometry/parity remain PC checks. Do not add unused spatial-selection algorithms
or claim C1 bowed-curve/spatial-pagination cases are complete in this slice.

Record any scope ruling and its consequence in the ledger. Independent review
must check actual native reader boundedness, source provenance of used fields,
world/epoch validation, DTO uncertainty and all production seam tests. No new
writer is introduced or enabled. Full C1 and G2/CON-01 remain open.
