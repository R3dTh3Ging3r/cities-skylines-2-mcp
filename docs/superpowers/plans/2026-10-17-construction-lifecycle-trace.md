# Consumed passive construction lifecycle trace

Base: `4683d277a9ece546d47d92e35ea41b3a40591825`, branch
`codex/construction-lifecycle-trace`. This implements the observation increment
in [the lifecycle sequence](2026-10-17-construction-lifecycle-next.md).

Status: implemented and independently accepted within source/offline scope at
`ae2c9aa0a84452ca0241f463f0fdbd4ae9c2a0b2`; see the
[acceptance receipt](../../handoff/LIFECYCLE-TRACE-2026-10-17.md).
Native evidence and completion/authority gates remain open.

## Global constraints

Passive diagnostics only. Preserve existing dispatch, response bytes/status,
stage ordering, deferred world guards, tool restoration and interruption behavior.
No admission, completion receipt, cancellation, rollback, cleanup verification,
save durability or player takeover authority comes from trace events. No native
installation, gameplay, merge, deployment, settings change or new worker.
Preserve all existing work and raw evidence. Use one existing implementer and
independent review; publish a draft stacked on the preserved inventory PR.

## Task 1: Instrument, consume and verify the real lifecycle

Instrument actual generic, road, transit and deferred transit rename boundaries.
Cover dispatch/queue, definitions, validation, Apply, Finish, reply attempt,
deactivation/restoration attempt, displacement and exceptions. Direct writers
receive only an envelope observation and a documented coverage map. Do not add
per-frame wait events or infer adapter stages for direct writers. Generic tool
displacement must retain its current behavior, including absence of cleanup.

Use immutable `legacy:<GuidN>` diagnostic identity, explicitly distinct from an
admitted operation UUID, original ingress world, separately observed world and
readiness, nullable frame with reason, UTC, fixed catalog route identity and adapter/kind/phase values,
optional status/ownership, and at most four already-held entity references with
fixed roles and omission accounting. Never read ECS just for tracing, retain
request/body/exception prose or arbitrary user strings, or relabel old references
as belonging to a new world. Old events survive until bounded eviction.

The observer isolates all token, timestamp, frame, ownership, serialization and
recording failures. Failures never alter original native calls, response,
restoration or thrown errors. Use bounded loss counters and no unbounded queue,
per-trace map or filesystem logging. Reply attempt is not transport delivery;
deactivation is not cleanup verification. Effects/cleanup/authority remain
explicitly unknown or unverified.

Enforce 256 retained events, 8 KiB serialized per event, 2 MiB serialized journal
bytes, safe integer sequence/counters with saturation and no rollover. These
wire/storage limits are not exact CLR heap claims. Expose eviction floor,
high-water, eviction/loss counters and saturation. Do not invent exact intervals
for failed observations. Reads and writes must remain concurrency-safe.

Provide fixed read/degraded GET `/diagnostics/construction-lifecycle`, consumed
also by the existing O7 `construction.lifecycleTrace` item (no additional MCP
tool). Strict query/options: optional `bootId`, `afterSequence`, `limit`; reject
unknown/duplicate keys and bodies. Default latest 16 ascending events, limit
1..32. Exclusive forward cursor 0..MAX_SAFE_INTEGER requires matching boot;
wrong boot is 409 and future cursor 400. Report tail omission, precise evicted
prefix, next cursor and hasMore. Pages are independent snapshots. Read response
cap 256 KiB; MCP item/error wrappers also bounded to 256 KiB; existing successful
O7 group cap stays 4 MiB. No automatic pagination or retries. Managed history can
be read without ECS, including loading, with honest world/readiness metadata.
O7 keeps its existing admission/readiness semantics and clearly labels history
as diagnostic rather than fresh native observation.

Append the fixed GET to `tests/native/lifecycle.json` so the existing generic
collector consumes the artifact. Keep all unexecuted lifecycle probes notRun,
native dispatch disabled, and compatibility/validation false. Verify actual
finite C# serialized trace consumption in the strict TS validator and fixture
metadata. Do not claim PowerShell execution when unavailable.

Use source-preserving partial extraction as needed so offline doubles execute
the real production queue/stage/complete/deactivate/stop/rename control flow.
Keep native definitions/readback leaves separate. Verify early generic reply,
late road reply, stop/line/rename sequence, busy/rejection, exceptions before and
after Apply, displaced tools, old-world callbacks, duplicate/late completion,
trace overflow and observer faults. Compare native calls, response bytes/status,
applyMode/stages and restoration with tracing enabled/failed/disabled. Include
strict read/cursor/boot/retention/size/SDK contracts and serializer-to-Node proof.
Run focused tests, canonical Framework build with `SkipDeploy=true` and existing
caches where available, and separate portable harness execution. No new package
installation or unavailable native suites. Keep exact commands and results.

## Acceptance and handoff

Independent review must assess both specification and quality across the whole
increment, particularly behavior preservation, failure isolation, boundedness,
world identity, consumer wiring and evidence labels. Fix substantive findings
with the same implementer and perform scoped re-review. Preserve the consolidated
native checks document, update evidence status and save a draft PR. The final
receipt distinguishes source/offline evidence from installed-reference compile,
native callback/effect/cleanup, takeover and newly requested save durability.
