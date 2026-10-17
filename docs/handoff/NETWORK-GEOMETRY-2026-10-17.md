# Selected-network geometry — source acceptance

Accepted source: `14cd65e8c531f31ce3b5795736b86906e057ef64`, comprising initial
`5edcf6de9a1a5a8cc83a43ff26fee6996191be88` plus the reviewed numerical correction.
Base is accepted PR8 `d1840f736978748a6778feff44aba62a24ab9a98`.
Independent specification, quality/security and complete-increment integration
review passed with no remaining Important findings. Root accepts those verdicts.
The increment is preserved on `codex/network-geometry-diagnostics`, stacked on
`codex/workforce-observations`.

## Useful workflow

Add `geometryDiagnostics:"sampledCubicV1"` to the existing
`cs2_construction_inventory` request or O7 `construction.inventory` item. For
selected existing road/rail curves it returns 17 ordered t/XYZ samples, signed
grade, 3D curvature and osculating radius, plus sampled extrema and their
locations. It supports inspecting where a selected segment rises, bends or has
undefined geometry quantities before considering later layout work.

The original observed world, full entity versions, cubic control points,
endpoints, native length and frame evidence remain visible. Only explicit root
curves are profiled. Combining directDepth1 keeps relationship inspection but
does not profile related targets or repeat native geometry reads. Omitted mode
retains old behavior; a producer omitting requested diagnostics is refused.
No new endpoint, tool, arbitrary-input math API or writer is added.

Grade is dimensionless rise/run along curve parameter direction, not lane travel
direction. Curvature is a 3D mathematical quantity in inverse native coordinate
units; radius uses native coordinate units. Physical metre conversion and native
engineering limits are unverified. Vertical/stationary derivatives, unbounded
straight-line radius and arithmetic overflow/underflow retain separate reasons;
missing values are not silently zeroed. Sampled extrema describe returned points,
not continuous extrema between them. No clearance, terrain, collision, native
preview, suitability, traffic or construction-feasibility verdict is produced.

## Numerical correction and integration

Independent review reproduced a translation defect in the initial implementation:
scaling absolute controls before taking differences could make an exact straight
line appear curved and report an incorrect grade. The correction derives local
captured-coordinate differences before normalization, with a guarded fallback
only for overflowing subtraction. It preserves absolute source positions and
uses no small-curvature clamp. Analytic untranslated, translated, reversed and
bowed fixtures now exercise the actual producer, serializer and typed consumers.
The original straight-line probe returns grade 1, curvature 0 and unbounded
radius at offsets 0, 100000 and 1e16.

Maximum 32 roots and fixed 17 samples give at most 544 pure evaluations. Original
world/readiness checks bracket computation and publication. Existing direct
256KiB, O7 item 256KiB/group 4MiB and escaped MCP 256KiB caps remain independent;
over-size results refuse whole without truncation or resampling. Existing
relationship limits, scheduling, cancellation, strict typed admission and SDK
bounded-error behavior remain intact.

## Verification checkpoints

Initial implementation passed 27 core, 13 O7, 35 response/serializer and 59
focused Node checks, plus TypeScript and both canonical net472 test builds.
Independent review passed 27 core, 35 response and 26 Node checks but found the
additional translation defect through its own consumed-source probe.

At the corrected source, 29 core, 36 response/serializer and 27 focused Node
checks passed using freshly emitted C# wire. Both canonical net472 builds passed
with zero warnings/errors and SkipDeploy=true. The same nine geometry tests also
passed with the checked-in synthetic fixture; they are a subset, not nine extra
unique tests. Independent re-review repeated the corrected 29/36/27 checks,
verified the original probe and found no remaining Important issue. Its fresh
fixture matched the checked-in fixture except sampledAtUtc.

These are separate checkpoints. The earlier 59 Node tests and unchanged broader
suites are not claimed as a full rerun after correction. Behavioral RED logs for
ingress and the numerical regressions are retained locally with their actual
failing results. No unavailable platform/game suite or dependency installation
was needed.

## Limits and next dependency

The actual native curve reader is unchanged. Linked-source tests with ECS-shaped
boundary doubles establish source field use and read reuse; they do not prove
installed Game/Unity binding, Framework runtime execution, native parity, timing,
performance or physical units. Extreme-double fixtures are managed numerical
evidence, not claims about achievable native float geometry. Full C5 snapping,
connection, offset layouts and mutations remain open, as do G1–G4.

The [PR4–8 reconciliation](ROADMAP-RECONCILIATION-2026-10-16.md) maps the accepted
read-only stack to the remaining roadmap. The next major dependency for playable
mutation batches is native G1 completion/takeover/save-identity evidence, followed
by consumed writer authority and feature-specific preview/action gates. PC
assembly inspection and runtime fixture proof are distinct stages. This receipt
does not authorize either, and packaging remains paused.

Rulings retained: fixed 17-point sampling bounds work but can miss narrow extrema;
reusing C1/O7 preserves one source path but rich maximum-root requests may need
fewer roots to fit byte caps; native-coordinate units avoid unsupported physical
claims and require later calibration before engineering interpretation.

Original checkout, prior PR branches and recovery snapshot remain preserved.
No package, installation, gameplay, merge or deployment occurred. See the
[frozen plan](../superpowers/plans/2026-10-16-network-geometry-diagnostics.md)
and [API contract](../bridge-api.md).
