# O7 observation groups — implementation brief

Execute after deferred-world source review on city-observation-workflow. Read the
larger batch plan, original O7 and retained decisions. This implements the
read-only observation half; no mutation batch, lease or new command authority.

## Contract and actual producers

POST /v2/observations, BridgeClient.observe and cs2_observe_batch use the exact
ObservationBatchRequest/Result defined by O7. Input1–64 items, each unique ID
length1–128, fixed name and strict args; optional expectedWorld. Validate the
whole envelope and every argument before any native work/reservation. Reject
unknown fields/writers/URLs/dependencies/lease. Capture original ready world
even when expectedWorld is absent. Node registration must use complete strict
schema (avoid SDK unknown-field stripping). Body framing is strict JSON/UTF8,
same1MiB cap and no query parameters. Existing operation refusals stay intact.

Initial fixed names: state, city.overview, city.services, city.labor, city.budget,
construction.inventory. Catalog args for the five context readers are empty;
inventory consumes existing strict full world/entity input and32-root cap.
Factor actual existing legacy handlers into reusable typed/native data readers;
legacy wrappers preserve success fields. Do not parse legacy JSON in the
coordinator or dispatch arbitrary paths. Budget enums require a hard loop bound;
cap strings/results. Verify labor fixed-counter and service cached-aggregate
paths have no hidden entity scan. Exclude demand/job completion, whole-city
queries, screenshots, arbitrary inspect, UI transit helpers and all writers.

## Scheduling and ownership

Use one production scheduling/accounting seam consumed by BridgeSystem for
ordinary requests and group items. Pending256 counts each not-started item;
reserve a whole group atomically or reject it without effects. At most8 active
groups. Global dispatch allows at most1 writer and8 reads or2ms additional work
per update, not one budget per group. Groups must not monopolize controls or
hide operation/capability listener responses. No unbounded side queue or waiters.
Keep native calls on game thread; managed admission/detach must synchronize safely.

Results preserve input order, exactly one observation/error perID. Every item
has its own original world/start/end/sample UTC/freshness/warnings, and cached
source counters disclose unknown source-refresh time (paused does not mean new
simulation data). Existing inventory keeps its root-only/native warnings. Group
start/end represent actual first/final read; no-dispatch frames null. Ordinary
item failures allow later reads. Check original world/readiness before/after
each native reader; load/unavailability ends remaining items, worldChanged true
where applicable, endFrame null, and retains completed item metadata.

Cap serialized item256KiB/group4MiB; oversize is an explicit per-item error,
never omitted or silently truncated. Byte accounting must include wrappers/errors
and UTF8. No durable group cache. A disconnected/timed-out observation transport
cancels undispatched read items, releases each reservation exactly once and
fences late completion. Implement the actual HttpBridgeServer/BridgeRequest
detach path, not just a fake cancellation policy. Normal mutation timeout still
means unknown outcome and must not cancel a dispatched writer.

## Evidence and review

Link production parser/catalog/scheduler tests into canonical net472 runner with
a named observations suite. Use exact production BCL sources in temporary net8
execution as needed; label native stubs. Test actual callback binding, all-or-none
invalid rejection, duplicate IDs, capacity boundaries, global mixed ordinary/
group budget, two groups fairness/backpressure, per-item error continuation,
mid-load metadata, detach before/after partial dispatch, repeated detach/completion,
output limits and resource release. Count native callbacks, not copied policy.
Include actual HTTP ingress/routing and MCP/client tests, strict malformed response
unions/IDs/worlds/frames. Use production serialization tests with existing SDK
Newtonsoft for byte/shape evidence where needed, qualified as nongame serializer.

Run targeted behavioral regressions and affected portable suites, TS and canonical
core compilation. Existing missingPowerShell/native game checks remain notRun;
no installs. Update capability/API/docs/testing and append explicit Node test list.
Record known native parity/performance gaps and partial O7 status. Commit only
task source/tests/docs; root owns larger batch plan/handoff. Independent review
must accept actual integrated scheduler/transport bounds before T1/M4/M5 build on it.
