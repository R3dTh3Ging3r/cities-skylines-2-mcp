# Larger city workflow — source progress

Eric requested a substantially larger batch before the next PC package/playtest.
The PC checkpoint remains paused. No install, merge, game/save or configuration
change is authorized. Branch codex/city-observation-workflow starts at reviewed
PR5 `07aca0d2e83084253b4d126a469a95391139bdc6`, retaining PRs2–5 and d21a563.
See the [sequence](../superpowers/plans/2026-10-16-city-observation-workflow.md).

## Deferred-world source acceptance

Source `fd0100be61ead515e7671f102558f8b0b6bc691f` passed independent Sol6.1/high
specification and quality review for the bounded O3 slice. Root accepts that
verdict; no Important finding remains. Original-world guards now protect delayed
line naming/reply, map-tile refresh, screenshot publication and the legacy
auto-pause timer. Stale or failed work cannot revive after readiness returns.
Post-create failures retain partial/unknown effects and do not invite a retry.

The review corrected swallowed delayed name-read errors and stale line tool
restoration/idle fallback. Main-menu screenshots remain diagnostic, with explicit
capture-world/readiness metadata; legacy bridges without metadata retain image
output. All native new-writer capabilities remain gated.

Verification:17 exact-linked managed assertions and39 portable Node tests passed;
canonical net472 core build had0 warnings/errors. C#9 syntax parsing of9 changed
native files had0 syntax errors; that is not native symbol binding. Reviewer
inspected final source/evidence and verified preserved unrelated native methods;
it did not rerun unchanged passing suites. Native/PowerShell execution remains
unavailable in cloud. No dependency installation or game connection occurred.

Full O3 completion/cleanup, earlier native tool/apply/pathfinding phases, O5 lease
authority and O6 supervised-run ownership remain open. Actual reload callback
timing and native behavior require G1; managed fencing is not native cleanup proof.

## Next source work

O7 bounded observation groups are in implementation. Selected T1 diagnostics,
typed M4 metrics and M5 collected history/comparisons follow their dependencies.
Each receives independent review before the next source increment. No native
release gate closes from these source reviews, and no small PC package is requested.

## Bounded observation source acceptance

Source `5f0e36eb44ab82ae250bcd220959772f8d7a11cf` passed independent specification
and quality/security review; root accepts the bounded read-only O7 increment.
`cs2_observe_batch` and POST `/v2/observations` now consume six fixed typed readers
through the actual shared scheduler. All-or-none admission, original-world
fencing, one ordered result per item, bounded UTF8 output and real HTTP/MCP
cancellation are implemented. Legacy native success fields remain compatible.
See [observation usage and limits](../observations.md).

Final evidence:65 exact-linked managed assertions,15 production HTTP/serializer
checks and45 portable Node tests passed. Canonical core and response net472
projects compiled with0 warnings/errors. Independent reviewer reran13 observation
assertions,15 HTTP/serializer checks and6 Node transport/MCP tests, all passing.
The review found and closed ordinary-error ending-frame loss and failure to
forward actual MCP cancellation to HTTP; both retain behavioral regressions.
A final-frame world invalidation regression is also covered.

These are Node24/exact-linked net8 execution and canonical net472 compilation,
not net472 runtime execution or native mod binding. Opaque native aggregate getter
internals/performance, panel parity and actual lifecycle timing remain unverified;
the2ms dispatcher limit checks between nonpreemptible calls. Full O7 mutation
batches, leases and native G1–G4 remain open.

Selected T1 source work follows this acceptance. Its corrected design requires
full world-bearing EntityRefs, unknown/unverifiable filter evidence, a combined
128-entry route-buffer cap and raw distance with unknown units. M4/M5 follows
T1 review using the preserved detailed plans. PC packaging remains paused.

## Selected transit source acceptance

Source `f8c0dd9f2a2311fe76b7fa092f8414a940b1a9bb` passed independent specification
and source-quality review; root accepts the partial T1 prerequisite. New
`cs2_transit_status` / `cs2_transit_capabilities` tools and matching GET endpoints
inspect one full world-bearing EntityRef. `transit.selectedLine` consumes the
same reader in O7. The combined128 waypoint/segment budget is checked before
any indexed reads;256KiB output is a separate actual UTF8 cap with explicit
oversize errors. No whole-city discovery or unbounded UI aggregation is added.

The result preserves XYZ, direct stop ownership/attachments, mode/purpose facts,
unknown filters and raw segment distances. Per-slot `structuralAssessment`
distinguishes known faults from unassessed evidence; null required references
remain known faults even without a reusable EntityRef. Missing optional metadata
is not a broken route. Native validity remains unknown; rawLength has unknown
units. M4 consumes the assessment rather than counting nullable reference lists.

Final implementation checks:112 exact-linked managed assertions,18 actual
HTTP/serializer checks and50 portable Node tests passed; both canonical net472
test projects compiled cleanly. Independent review reran12 transit,13 O7,
18 HTTP/serializer and5 Node checks, all passing, and compiled TypeScript.
Review corrections cover unknown-vs-fault classification, null slots, bounded
non-2xx bodies, partial capability metadata and accurate native-error handling.

A full npm attempt encountered15 existing missing-PowerShell failures and2
existing TODOs. Its stale capability-count assertion was corrected and passed
in the final portable run. Do not repeat unavailable suites until the host has
the required runtime. C#9 syntax parsing of6 integration files passed but does
not establish native symbol binding. Full T1, installed-game compilation,
committed route semantics/units, timing and native fixture remain notRun.
See [native source findings and limits](../native/transit-readback.md).

M4 typed metrics are the next active source increment; M5 collection/comparison
follows independent M4 review. PC packaging remains paused.
