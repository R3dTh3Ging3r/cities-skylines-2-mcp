# Bounded observation groups — partial O7

`POST /v2/observations`, `BridgeClient.observe` and `cs2_observe_batch` sample
selected city context and explicit construction geometry sequentially. This is
the read-only half of O7. Mutation batches, replay/resume, leases and native
writer completion remain unavailable; native parity/performance is `notRun`.

```json
{
  "expectedWorld": { "bootId": "example-boot", "epoch": 7 },
  "items": [
    { "id": "city", "name": "city.overview", "args": {} },
    { "id": "utilities", "name": "city.services", "args": {} },
    { "id": "budget", "name": "city.budget", "args": {} }
  ]
}
```

Substitute the ready world observed through capabilities. `expectedWorld` is
optional; admission always captures the original ready world. Every item is
validated before reserving any work. Require 1–64 unique IDs, each 1–128 UTF-16
characters. Only the names below and their exact arguments are accepted; unknown
fields, writers, URLs, dependencies and leases reject the whole request.

| Fixed name | Arguments and source scope |
| --- | --- |
| `state` | `{}`; game state, simulation clock/speed and city name. Requires ready world here; legacy `/state` remains available outside a city. |
| `city.overview` | `{}`; existing city/population/finance aggregates. Missing Population retains legacy zero fields but adds `populationComponentUnavailableLegacyDefaults`; typed projection retains an internal presence flag and must not treat those defaults as measured zeros. |
| `city.services` | `{}`; cached electricity, water and garbage aggregates, not individual facility capacity/coverage. |
| `city.labor` | `{}`; household counters and two education-counter structs, no entity query. |
| `city.budget` | `{}`; existing signed monthly/hourly budget data. Both enum counts must be 0–128 before loops; category names at most128 characters. |
| `construction.inventory` | Existing strict `{world,entities}` with 1–32 same-world versioned refs; all refs must match the group's captured world. Root-only C1 scope/warnings remain intact. |
| `transit.selectedLine` | Strict `{line:EntityRef,mode?,purpose?}`; one full versioned line with its own original world, shared128 waypoint/segment entry budget and structural-only T1 evidence. |
| `metrics.read` | Strict `{world,scope,queries}`; the fixed typed [M4 producer](bridge-api.md#typed-diagnostic-metrics-partial-m4). Explicit city or one full-line scope, distinct known keys and empty parameter maps. Returns MetricBatch with original source/clock/member evidence. |

The native adapters reuse typed readers beneath legacy JSON wrappers; they do
not deserialize legacy responses. City names are capped at256 characters for
observation admission to a result; oversized strings fail explicitly rather than
being truncated. Legacy wrapper fields/defaults remain available.

Result shape is `{world,startFrame,endFrame,worldChanged,results}`. Rows preserve
input order and contain exactly one `{id,observation}` or `{id,error}`. Successful
items use the shared Observation shape (`data`, world, sample UTC, frame bounds,
freshness, warnings). Cached context adds `cachedSourceRefreshTimeUnknown`:
sampling time is not a source-refresh timestamp, and a paused city can retain old
counters. Freshness is `warming` before the existing first-simulation-tick flag,
otherwise `current`; it does not certify native correctness or source refresh.
Multi-frame item reads warn `multiFrameReadNotAtomic`; groups are never atomic.

Group frame bounds describe actual dispatched reads, including ordinary reader
errors. No-dispatch bounds are null. Ordinary item errors allow subsequent reads.
A changed world or lost readiness stops remaining items, preserves completed
observations, and nulls group endFrame. `worldChanged` reflects identity change;
same-world readiness loss uses `NOT_READY`. Error-only rows do not invent an
Observation. There is no durable result cache or mutation-style replay promise.

One production scheduler serves ordinary requests and group items:256 shared
not-started slots, all-or-none group reservation, eight active transports, and
one global one-writer/eight-read/2ms additional-work budget per update. Ordinary
work and groups rotate fairly. A current native call or bounded encoding cannot
be preempted; 2ms is a check between work items, not a hard handler wall-time cap.
Completed groups retain their transport slot until HTTP write/cleanup, bounding
completed-but-unsent response ownership. Listener capabilities/operation refusals
remain independently available.

Serialized limits are256KiB per item and4MiB per group, including IDs, wrappers,
errors and actual UTF-8 bytes. Space is reserved for every remaining error row.
Oversized output becomes ordered `RESULT_TOO_LARGE` rows, never omitted data or
silent truncation. HTTP uses strict JSON/UTF-8, a1MiB request limit and no query
parameters. Single-request connections must remain open while awaiting a reply.
Timeout/disconnect cancels undispatched read items, releases reservations once
and fences late completion. It does not cancel an ordinary dispatched writer.

`BridgeClient.observe(request,{signal})` accepts an optional AbortSignal, composes
it with timeout using Node18-compatible listeners, and cleans listeners/timers.
MCP cancellation forwards the SDK signal. The client independently validates IDs,
exclusive unions, world/frame evidence and streamed response bytes before
returning; it does not trust Content-Length or retry automatically.

Source inspection establishes fixed component/counter field counts and budget
loop bounds with no explicit scans/job waits in these adapters. Native getter
implementations (household/workplace/service/budget internals) are absent here:
hidden synchronization/traversal latency and panel parity remain unverified.
Actual game-thread/lifecycle timing and whole-candidate compilation require the
reviewed SHA on PC with `SkipDeploy=true`; G1–G4/native fixtures remain `notRun`.
