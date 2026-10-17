# Passive save-task observation — October 17, 2026

Accepted scope: bounded managed observations from the real legacy SaveGame call,
consumed through the existing lifecycle journal, diagnostic GET and O7 item.
This is preparation for O4 evidence gathering, not verified save completion.
Independent specification, quality/security/evidence and whole-increment review
accepted source `a71d8d191fe2877cd4d9311f9e834d756971cd35` with no Important
findings. Later receipt commits change documentation only.

## Behavior

The candidate follows lifecycle trace PR11 at
`8f62388de452f8cbd0aaaa95d413134dbb54897b`, on branch
`codex/save-task-observation`. The existing save method is extracted into a partial
to test its actual control flow with native boundary doubles. City/menu guards,
default naming, native preparation and argument order, preview identity and
ownership, synchronous exceptions and immediate legacy response are preserved.

Finite phases record the save expression's attempt, return or throw and the
returned Task's observed true, false, faulted or canceled state. Attempt/throw
also encompass evaluating the manager and database arguments; they do not prove
native method entry if a getter throws. Neither Task true nor the legacy
`saving: true` response proves native saveSuccess, event delivery, destination
identity, a durable artifact or a completed operation. False/fault/cancel also
do not exclude partial effects, and observed cancellation adds no cancellation
capability.

At most 32 observers are reserved, attached, queued or running per journal/boot.
Never-completing tasks keep their slots; further real saves proceed with explicit
capacity skips and observation loss. Null tasks and registration failures expose
unavailable capture. Counters saturate safely and no diagnostic failure becomes
a save admission decision. No extra writer queue, timer or worker is introduced.

Static default-scheduler continuations retain only the original immutable
diagnostic context, managed journal and release guard. Ambient execution context
is suppressed only during attachment and restored afterward. Continuations do
no Unity/ECS/UI/native work, wait, preview cleanup or destination lookup. Late
events remain attributed to their original request/world. Their UTC value marks
observation time; task completion and legacy reply can have different ordering.
Pending count includes a callback until its finally release.

The original journal and wire bounds remain in force. GET
`/diagnostics/construction-lifecycle` can expose managed history while unavailable;
O7 `construction.lifecycleTrace` retains ready-world admission. The existing
lifecycle fixture already consumes the fixed GET. Native mutation probes remain
notRun and dispatch disabled. No new endpoint, MCP tool or operation receipt is
introduced. Save names, paths, preview objects and exception details are absent
from the retained events.

## Offline validation and review

The implementation's linked-production runner passes 53 checks, including
immediate/delayed true, false, fault and cancellation; legacy bytes/default names;
native argument/order and preview parity; early guards, synchronous throws and
null tasks; concurrency/reload; bounded pending capacity/release; recorder faults;
ambient isolation and original-journal ownership. The registration-throw path
uses a narrow transient test injection; production uses fixed BCL attachment.
Initial behavioral failures are retained as evidence, not hidden by final reruns.

Actual C# serialized history passes 10 strict TypeScript/stdio SDK tests, including
finite terminal states, explicit capacity loss and old-world attribution. The
canonical lifecycle net472 build passes with zero warnings/errors using cached
dependencies and SkipDeploy=true. Portable net8 execution with native leaves
doubled is distinct from Framework runtime and installed-reference validation.
The checked-in synthetic fixture suite separately passes 10 tests. A covering
fresh-wire/O7 integration run passes 20 tests, including the same 10 trace checks;
these overlapping runs are not added together. TypeScript compilation and
whitespace checks pass.

Independent review accepted the complete increment. Its targeted probe occupied
the only default-scheduler worker and invoked actual SaveGame 33 times with
already-completed native-double tasks: all 33 saves ran, 32 observations remained
pending, one was skipped/lost, and no terminal callback ran before worker release.
Reload preserved all reservations. Releasing the worker produced 32 original-world
terminal observations and pending returned to zero. The reviewer also independently
passed two focused fresh-wire SDK/strict-evidence tests. No source correction was
needed. Detailed implementation/review reports and raw logs remain in ignored
`.superpowers/sdd/2026-10-17-save-task-observation/`.

## Remaining gate

The [prior playtest reconciliation](POST-PLAYTEST-2026-10-17.md) records selected
native successes on older source. This save-task increment has not been installed
or run against a game. The [consolidated native checklist](../native/CONSOLIDATED-CHECKS-2026-10-17.md)
retains installed API compatibility, actual callback/effect/cleanup ordering,
all-writer takeover, native save events and destination attribution, durable
readback and preview ownership as separate requirements.

The supported passive capture for this sequence is exhausted. Further completion
or ownership behavior depends on that evidence; additional unused instrumentation
cannot substitute for it. No new game/save session, installation, merge,
deployment or settings change was performed or requested here.
