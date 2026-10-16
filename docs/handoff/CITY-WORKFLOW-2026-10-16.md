# Larger city workflow — source progress

Current status: the bounded deferred/O7/selected T1/M4/M5 source batch is
independently accepted through `7e8547933505132e79f654d04ed45d7a56f5e6b1`.
Earlier next-step paragraphs below are chronological history. The final M5
acceptance records the current managed capability and native limitations.
Publication uses existing draft PR6; PC packaging remains paused.

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

## Typed metric source acceptance

Source `8539b99af3acbdebc2764f8e5491597deffea63f` passed independent specification
and quality/security review with no open Important findings. Root accepts the
bounded M4 prerequisite. `cs2_metric_catalog` and `cs2_observe` expose sixteen
typed population, treasury/XP, labor/jobs, signed monthly budget and selected-line
structural metrics. Strict GET facades and O7 `metrics.read` consume the same
typed producer. Missing Population remains unavailable while present zero stays
zero; exact fault totals consume T1 slot assessments and refuse unassessed totals.

MetricBatch retains original source frames, world, UTC, freshness and unknown
refresh timestamps. Raw normalized clock evidence names its actual source via
`sourceObservationId`; city output scope includes explicit `line:null`.
Structural fingerprints cover only the bounded observed route sample. Native
calendar conversion, panel/unit parity, complete route geometry and performance
remain unverified. No metric has an automatic improvement direction.

Final checks passed:130 exact-linked managed assertions,27 actual serializer/HTTP
checks,61 portable Node tests and11 default-fixture tests; both canonical net472
test builds had zero warnings/errors. Independent review reran11 default-fixture,
27 serializer/HTTP and11 fresh-wire Node checks. The recovered stale checked-in
fixture first failed, was regenerated from the corrected actual producer, and
now passes both paths. Nine production C# files passed syntax-only parsing;
this does not establish native symbol binding or game execution.

The historical broader M4 attempt had60 passes and6 failures: one stale tool
count was corrected; five PowerShell ENOENT failures were excluded from the
resumed portable list without weakening their Windows gates. Linked managed
execution used net8 and SDK Newtonsoft, not the installed game runtime.

M5 collection/comparison source may now proceed using the accepted M4 contracts
and the preserved history-lifecycle plan. Full M4 utility/production analytics,
native history and G1–G4 remain open. PC packaging remains paused.

## Collected history and comparison source acceptance

M5 source `52267452823a366340375a9662b484b52d53a8d3` and scoped fixes
`7e8547933505132e79f654d04ed45d7a56f5e6b1` complete the bounded managed
workflow: `cs2_collect_observations`, `cs2_stop_observation_collection`,
`cs2_read_observation_history` and `cs2_compare_observations`. Collection pins
actual catalog definitions and exact world/cohort identity, polls the existing
O7 `metrics.read`, and retains original source, clock and gap evidence. Stop and
process shutdown abort pending reads and fence late publication.

Eight retained histories (including stopped), 512 samples per history, 128KiB
per sample, 1MiB per history and 8MiB total bound serialized retention. One
scheduler and two shared network permits cover polling and catalog admission.
Missed slots become gaps; there is no catch-up burst. Reads page whole samples,
and every tool's serialized result is bounded at 256KiB. This is process-local
history without persistence, export, automatic restart or native historical data.

Comparison uses only explicit retained ranges, fixed cohorts and compatible
producer definitions, structural fingerprints and raw normalized-time evidence.
It preserves unknowns and refuses missing, gapped, changed or unaligned evidence.
Qualified paired deltas do not imply improvement: `improvement` remains null.
No interpolation, rate integration, replacement-world read or mutation is added.
The four managed tools are reported separately from native capabilities.

The review found two SDK integration defects and the scoped fix preserves exact
advertised required/default/nested schemas while bounding validation errors, and
retains stream-error handling until actual close promises settle. Actual SDK
regressions cover queued, delayed, rejected and externally initiated closes,
plus malformed/oversized inputs rejected before HTTP. Independent specification, quality/security and final bounded whole-batch
integration review accepted exact source `7e8547933505132e79f654d04ed45d7a56f5e6b1`;
root accepts those verdicts, with no remaining Important findings. The reviewer
independently reran the TypeScript build and the final 18 focused tests, all passing.

Verification chronology: the implementation's 92-test portable suite passed
before the final schema facade/source-frame regressions; the later collector
and real-MCP focused suite passed 18/18. After the two review fixes, the final
TypeScript build and 18/18 actual tools/lifecycle/existing O7 integration checks
passed. These counts are separate checkpoints, not a claim that the full
92-test suite was rerun at the final source SHA. M5 changes no C#; accepted M4
managed/serializer/canonical build evidence remains recorded above. Known
unavailable PowerShell/native suites were not repeated.

Native binding, installed-game/runtime behavior, source refresh/calendar/unit
parity, opaque getter performance and G1–G4 remain unverified. Full roadmap
operations, transit and management work remains partial. No PC packaging,
installation, game/save/configuration activity, merge or deployment occurred.
The original checkout and recovery snapshot remain preserved.
