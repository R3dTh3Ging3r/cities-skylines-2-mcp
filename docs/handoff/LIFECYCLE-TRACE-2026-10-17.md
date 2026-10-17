# Passive construction lifecycle trace — October 17, 2026

Accepted source scope: consumed passive observation of legacy construction lifecycle.
Independent specification, quality/security and whole-increment review passed after
one correction round. This is not native completion, cleanup, player
takeover or save-durability acceptance.

## Behavior and consumption

The candidate builds on inventory recovery at
`4683d277a9ece546d47d92e35ea41b3a40591825`, on branch
`codex/construction-lifecycle-trace`. Generic placement, road build/replace,
transit stop/line and deferred rename retain their existing control flow and
responses. Lifecycle methods are split into partial files so offline tests can
execute those production methods with native definition/readback leaves doubled.

Initial source: `1f2a44f0c04ed7459b381eb0c67333898b6a43f7`.
Accepted correction/final source: `ae2c9aa0a84452ca0241f463f0fdbd4ae9c2a0b2`.
Later receipt commits change documentation only.

The trace distinguishes generic reply during Apply, road reply during Finish,
and a transit rename reply after tool deactivation. Fixed catalog route identity
also distinguishes direct writer envelopes without inventing Apply/Finish stages
for zoning, setters, simulation, camera or saves. Diagnostic `legacy:` IDs are
not admitted v2 operation UUIDs. Reply attempts and managed completion acceptance
do not prove transport delivery; deactivation and restoration observations do not
prove native cleanup or player takeover.

The journal retains at most 256 events, 8 KiB serialized per event and 2 MiB of
serialized event data. These are wire/storage caps, not exact CLR heap promises.
GET `/diagnostics/construction-lifecycle` and the existing O7 tool's
`construction.lifecycleTrace` item expose bounded history. The default is the
latest 16 events; explicit pages allow 1–32. Forward cursors are exclusive and
boot-bound. Responses expose retention gaps, tail omission and observation loss;
independent pages are not one stable snapshot. Individual wire/item wrappers are
bounded to 256 KiB, while successful O7 groups retain their existing 4 MiB cap.

Events keep original ingress identity separate from observed world/readiness.
Old-world callbacks do not sample replacement-world native frame or ownership.
The direct diagnostic GET can read managed history during loading; O7 preserves
its existing ready-world admission. No per-frame wait log, new writer queue,
filesystem city log, retry, native scan or completion authority is introduced.
Observer failures leave original operation behavior intact.

The existing lifecycle fixture registers the fixed GET as a supplemental read
artifact. Its native mutation probes remain unexecuted and dispatch unavailable.
The production wire is consumed by a strict TypeScript validator and actual SDK
tests; this does not claim execution of the unavailable PowerShell collector.

## Offline evidence

Covering evidence, with the review correction described below:

- Initially 28 linked-production lifecycle checks passed, including queue/Apply/Finish,
  busy/rejection, exceptions, displacement, deferred rename and destruction,
  old-world fencing, duplicate completion, observer faults, retention/byte bounds,
  concurrent snapshots and real ingress/handler reads during loading.
- Independent review found a stale transit-result attribution after post-Apply
  interruption and adapter reuse. The correction keeps a diagnostic result only
  after the current operation assigns it, preserving native fields and cleanup.
  Four added cases cover line-to-stop and stop-to-line reuse in the same world
  and across a transition. They fail on the original source and pass after the
  correction, bringing the lifecycle suite to 32 checks. Recorder drop/throw
  modes preserve native calls, stages, restoration and response bytes/status.
  Scoped independent re-review marked the finding addressed, reran the original
  same-world/transition probes and the focused SDK reuse test, and accepted the
  whole increment with no remaining Important findings.
- Existing O7 core: 13 passed; endpoints: 8 passed; response/HTTP/serializer: 40
  passed. These use portable execution of linked production sources.
- 36 focused Node tests passed against this run's C# serialized trace output,
  including inventory error recovery, mixed O7 groups and existing large-group
  behavior. The default checked-in synthetic fixture suite separately passed 7
  tests; these overlap and are not additional aggregate coverage.
- After the review correction, fresh C#-to-SDK and checked-in synthetic trace
  suites each passed 8 tests, including reused-adapter histories. TypeScript and
  the canonical lifecycle net472 build passed again with zero warnings/errors.
- TypeScript compilation and all three canonical net472 test-project builds
  passed. The final Framework builds reported zero warnings/errors. Portable
  net8 execution is distinct from Framework runtime and installed-game evidence.

Raw reports, initial expected failures and build/test logs remain under ignored
`.superpowers/sdd/2026-10-17-construction-lifecycle-trace/`. Checked-in wire
fixtures are synthetic, not private city telemetry. Existing workers and local
evidence are preserved, trading disk space for continuity without waiving review.

## Remaining evidence

The [prior playtest reconciliation](POST-PLAYTEST-2026-10-17.md) remains the
record of selected native successes on the older tested candidate. This trace
increment has not been installed or run in the game. Installed-reference compile,
actual callback/playback order, effects and cleanup, visible all-writer takeover,
and newly requested save task/event/destination durability remain separate gates.
Tracing cannot satisfy them or enable guarded operations.

The [consolidated native checklist](../native/CONSOLIDATED-CHECKS-2026-10-17.md)
collects those checks for a later authorized session. No new small playtest,
installation, game connection, merge, deployment or settings change is requested
or performed here. The [lifecycle sequence](../superpowers/plans/2026-10-17-construction-lifecycle-next.md)
still governs completion/authority work after suitable evidence exists.
