# Selected-network computed geometry inspection

Base `d1840f736978748a6778feff44aba62a24ab9a98`; branch
`codex/network-geometry-diagnostics`. This is the next coherent offline increment
after the PR4–8 reconciliation, a read-only portion of C5 consumed by C1/O7.
It helps inspect grade and curvature along selected existing road/rail curves.
It does not implement C5 connection, explicit snapping, offsets or construction.

## Global constraints

One source implementer, followed by one independent specification, quality/security
and complete-increment review. Reuse the existing lean team. No new workers,
installation, packaging, gameplay, merge, deployment, credentials/configuration
changes or native mutation capability. Preserve prior branches, original checkout,
recovery snapshot and local evidence. Build with SkipDeploy=true; use cached
dependencies and exact-source portable harnesses, not unavailable native suites.
Root owns this plan, roadmap reconciliation and final acceptance receipt.
Eric authorizes ongoing repository draft PR creation/updates with feature/test
results. Root publishes only after review accepts the exact final source.

## Frozen consumed contract

Add optional `geometryDiagnostics:"sampledCubicV1"` to the existing inventory GET,
`cs2_construction_inventory` and fixed O7 `construction.inventory` item. No new
endpoint/tool or arbitrary-input math API. Query/schema decoding must preserve
strict unknown-key, duplicate/full-version/same-world checks. The omitted mode
preserves existing response behavior and native access. Additive diagnostic
output is absent or null when omitted; old producer responses remain valid for
omitted-mode callers. A requested mode must never accept a missing diagnostic
result as success.

Compute only explicit root network curves already obtained by C1. Combined
`relationships:"directDepth1"` retains current source-slot/target bounds and never
profiles related targets or re-reads native geometry. Missing, stale, building or
unsupported roots each retain an unavailable profile/reason; do not invent a
flat curve. All existing identity, original-world/readiness, source frame,
freshness and warnings remain visible. World validation must bracket production
and publication, including the computation. No global query, recursive walk,
per-sample native callback or other new native getter.

Implement one BCL core algorithm consumed by the real inventory producer.
For each available exact cubic, return **17 ordered parameter samples**, t=i/16
including both endpoints. Maximum32 roots gives at most544 evaluations. No
caller-controlled resolution, adaptive loop or tolerance search in v1. Expose
these finite work bounds without promising game-thread latency. Keep all17 rows,
including undefined samples, or refuse the entire over-size response.

Each sample includes t, world XYZ position, grade, 3D curvature and 3D osculating
radius with explicit individual support/reason when unavailable. Preserve source
control points, endpoint refs and native reported length separately through the
original inventory output. Never replace native length with chord/polyline length;
no computed length is added in this increment.

For P(t), use analytic first and second derivatives. Grade is signed
`P'y/sqrt(P'x²+P'z²)` in dimensionless rise/run along a→d; it is not lane direction
or percent unless an explicitly named derived presentation is provided (not
needed). Curvature is `|P'×P''|/|P'|³`, a **3D** curve quantity, not horizontal
road turn radius. Radius is its finite positive reciprocal. Zero curvature is
supported zero; its radius is null with an unbounded/straight reason. Stationary
first derivatives make curvature/radius unavailable. Zero horizontal derivative
makes grade unavailable, not zero; curvature may still be available. Invalid
arithmetic affects the values it prevents, not unrelated valid fields.

Coordinate basis is native worldXYZ with Y vertical. Grade is dimensionless;
radius uses native-world-coordinate units and curvature their inverse. Physical
metre conversion/native engineering units remain unverified; do not label m,
metres or inverse metres. Do not invent native limits or feasibility thresholds.
Use stable scaled vector/norm arithmetic where appropriate and finite guards.
Do not clamp overflow or round tiny derivatives into a flat curve. If conditioning
prevents reliable finite evaluation, return a bounded reason. All scalar values
and positions must be finite or explicitly unavailable; JSON must never acquire
NaN/Infinity disguised as supported null.

Return min/max grade and curvature/radius where meaningful **among returned
supported samples**, carrying the corresponding t/location. Include supported
and unavailable sample counts per quantity. Summaries must not ignore missing
samples silently or claim continuous/global extrema. Equal extrema may choose
the earliest sample deterministically. Sharp between-sample variation remains
unmeasured. No terrain/clearance/collision, prefab suitability, lane/vehicle or
native preview conclusion, improvement score or default good/bad design verdict.

Mirror strict bounded output in TS. Validate mode/identity/root ordering,
exact17-grid sample rows for available profiles, unsupported rows/reasons,
finite-or-null consistency, summary provenance/counts/extrema coherence and
original C1 metadata. Do not introduce a second numerical production algorithm
in TS merely to duplicate the C# computation. Preserve authoritative old C1
parser invariants, directDepth1 and existing O7 catalog branches.

All existing bounds remain:32 roots; relationship128 rawslots/128additionaltargets;
direct HTTP256KiB; O7item256KiB/group4MiB; escaped direct MCP wrapper256KiB;
shared admission/update scheduling and cancellation. Maximum-count requests may
exceed serialized byte limits; return whole RESULT_TOO_LARGE rather than truncate
or silently reduce samples. Keep bounded SDK malformed-input errors and actual
required/default/nested discovery. No query/storage/result limit increase.

## Task 1 — Implement and prove the consumed profile workflow

Source owner: existing c1_implementation. Report:
`.superpowers/sdd/2026-10-16-network-geometry-diagnostics/implementation-report.md`.

1. Meaningful behavioral RED: actual existing query/core/SDK rejects or fails the
   requested profile; avoid merely missing-API/compiler failure. Retain logs.
2. Pure algorithm checks use analytic fixtures: line with constant signed grade,
   straight zero-curvature/unbounded radius, reversed orientation, bowed spatial
   curve, vertical and stationary derivatives, coincident controls, tiny/large
   finite arithmetic and overflow. Assert17 ordered points and per-quantity
   null/reasons and summaries. Include variation between sample points so tests
   cannot silently relabel sampled extrema as global bounds. Tests must exercise
   production math rather than a copied reference implementation.
3. Actual linked C1 producer/native-boundary/serializer checks: distinct observed
   curves, one existing native geometry read per root, original-world invalidation,
   unsupported roots and combined relationships without additional target profiles.
   Export actual synthetic C# wire for direct and O7 consumed Node checks. Boundary
   stubs are not installed Game/Unity proof. Preserve existing harnesses/evidence.
4. Actual SDK and HTTP/MCP checks: new literal discoverable in inventory and O7,
   strict unknown/depth/ref cases bounded and0HTTP, old omitted mode preserved,
   missing new result refused, finite/summary/count malformed producer data refused.
   Execute real MCP inventory GET and O7 POST over emitted wire including nonflat,
   degenerate and unavailable roots. Preserve existing O7 cancellation/detach and
   item errors; no new native writer capability.
5. Verify32root/544evaluation bound and actual serializer/wrapper byte pressure;
   refusals are whole. Build canonical linked projects with cached compatible
   Newtonsoft and SkipDeploy=true; execute focused exact-source portable suites
   and focused Node/SDK tests. Do not rerun unrelated unavailable native suites.
6. Document source qualification and usage in existing inventory/bridge docs.
   Commit only implementation/tests/docs scoped to this task; root plan and
   reconciliation remain unstaged. Return finalSHA, exact tests/checkpoints,
   implementation report and any real concerns. No self-push or PR changes.

After source freeze, root creates exact base..HEAD review package and resumes
existing o2_review for task and complete-increment integration review. Important
findings return to the same implementer; corrections get focused tests and scoped
rereview. Root then records acceptance and publishes a draft stacked on PR8.

## Rulings and limitations

Choose a fixed17-sample v1 over user resolution/adaptive math: useful bounded
profiles without a convergence/precision promise. Cost: narrow curvature or
grade extrema between samples can be missed; output explicitly says so.
Choose existing C1/O7 optional mode over another tool/client-only calculation:
one observed source path and consistent wire meaning, with fixed extra pure
work under the same native budget. Cost: rich32-root requests may hit byte caps;
callers reduce roots, never silently receive fewer samples.
The roadmap reconciliation establishes the G1/G2/G3/G4 native boundary for later
playable mutations and richer diagnostics. This slice does not close those gates.
