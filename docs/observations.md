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
MCP discovery advertises this strict object and every typed catalog branch,
including the optional inventory relationship literal. SDK validation errors
are bounded to256KiB; this error limit does not replace the4MiB group result limit.

| Fixed name | Arguments and source scope |
| --- | --- |
| `state` | `{}`; game state, simulation clock/speed and city name. Requires ready world here; legacy `/state` remains available outside a city. |
| `city.overview` | `{}`; existing city/population/finance aggregates. Missing Population retains legacy zero fields but adds `populationComponentUnavailableLegacyDefaults`; typed projection retains an internal presence flag and must not treat those defaults as measured zeros. |
| `city.services` | `{}`; cached electricity, water and garbage aggregates, not individual facility capacity/coverage. |
| `city.labor` | `{}`; household counters and two education-counter structs, no entity query. |
| `city.budget` | `{}`; existing signed monthly/hourly budget data. Both enum counts must be 0–128 before loops; category names at most128 characters. |
| `construction.inventory` | Strict `{world,entities,relationships?,geometryDiagnostics?,relationshipPage?}` with 1–32 same-world versioned refs. `relationships:"directDepth1"` admits global128 direct slots and128 additional exact targets, no recursion. `geometryDiagnostics:"sampledCubicV1"` computes17 t/XYZ/grade/3D-curvature/radius samples per captured root curve, at most544 evaluations, with sampled-only extrema and individual unknowns. Optional `relationshipPage:{kind,offset,limit}` requires all fields, one root, limit1–32 and no directDepth1; returns raw selected-kind reference slots and six headers with zero target reads. Continue nextOffset manually; independent live pages never establish a stable snapshot or complete facility. Root diagnostics may coexist. No extra native or related-target profiles. World/root-only/full-envelope qualification remains intact; new pages require native validation. |
| `transit.selectedLine` | Strict `{line:EntityRef,mode?,purpose?}`; one full versioned line with its own original world, shared128 waypoint/segment entry budget and structural-only T1 evidence. |
| `metrics.read` | Strict `{world,scope,queries}`; the fixed typed [M4 producer](bridge-api.md#typed-diagnostic-metrics-partial-m4). Explicit city or one full-line scope, distinct known keys and empty parameter maps. Returns MetricBatch with original source/clock/member evidence. |

The native adapters reuse typed readers beneath legacy JSON wrappers; they do
not deserialize legacy responses. City names are capped at256 characters for
observation admission to a result; oversized strings fail explicitly rather than
being truncated. Legacy wrapper fields/defaults remain available.

`metrics.read` includes ten total/free workplace-slot counters by native education
tier. The existing typed labor source is read once for all requested labor keys;
source refresh time remains unknown. Collection and qualified stored comparison
retain each bucket separately, with no worker-supply or shortage diagnosis.
Missing buckets stay unavailable and improvement remains null. See the
[metric contract and compatibility notes](bridge-api.md#typed-diagnostic-metrics-partial-m4).

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

## Collected diagnostic history — bounded M5 slice

The MCP process can collect the fixed M4 `metrics.read` entry; it adds no native
history endpoint or gameplay authority. `cs2_collect_observations` takes a strict
`{world,queries,scope,cohort,maxSamples,intervalMs}`. Copy the ready observed world,
use the catalog's literal keys with empty parameter maps, and declare either
`scope={kind:"city"}` with `cohort={kind:"city",world,members:[]}`, or one full
selected-line scope with `cohort={kind:"fixedSelectedLine",world,members:[line]}`.
The line world/index/version must agree everywhere. City output's `line:null` is
an output field; omit it when constructing a new input scope.

Admission snapshots the authoritative catalog and freezes the selected definitions
and request. Collection starts only on explicit request. Eight retained histories
include stopped histories and pending admissions; a ninth gets `SERIES_CAPACITY`.
IDs and samples last only for this process. There is no delete, external append,
export, persistence, auto-restart or replacement-world API.

One monotonic scheduler owns at most one poll per series and two total network
requests, including admission catalog requests. It skips missed deadlines with
explicit gaps rather than issuing catch-up bursts. UTC labels do not schedule
polls. Stop, disconnect and shutdown abort real HTTP reads and fence late replies;
a cancelled read retains its network permit until its original promise settles.
World changes and backwards observed frames stop that history without adopting a
new world. Repeated paused frames can be retained, but establish no simulated
duration. Native getters remain nonpreemptible and their latency unverified.

| Retained or returned surface | Bound |
| --- | --- |
| Samples | Requested 1–512;128KiB serialized UTF8 per complete sample |
| History | 1MiB including spec, definitions, samples and gap metadata;128KiB reserved for metadata |
| All histories | 8MiB total; at most eight retained/pending histories |
| Gaps | 64 detailed entries/history, adjacent equal reasons coalesced; aged detail summarized by fixed reason/count/sequence bounds |
| Tool result | 256KiB including the actual MCP text/error wrapper and escaping, for collect, stop, read and compare |
| Read page | 1–128 samples within the result byte cap |

These are serialized-byte limits, not exact JavaScript heap measurements. The
underlying O7 client separately caps streamed responses at4MiB. Oversized samples
become gaps. Retention evicts whole oldest samples and reports their lost coverage;
unavailable values remain null. Source frames, freshness, warnings, unknown refresh
timestamps, exact member refs and producer-owned structural evidence are retained.

`cs2_stop_observation_collection({id})` is idempotent and returns status, counts,
retained span and gap summary; it does not dump the history. Read with
`cs2_read_observation_history({id,limit:20})`. `afterSequence` is exclusive. If the
page returns non-null `nextSequence`, continue with
`afterSequence: nextSequence-1`; this works across gaps and byte-limited pages.
A page that cannot hold its first sample returns `RESULT_TOO_LARGE`.

`cs2_compare_observations` takes two explicit inclusive retained ranges:
`baseline/current={id,fromSequence,toSequence}`, the original `cohort`, and
`policy={mode:"rawNormalizedTime",phaseTolerance:0.001,maxObservedPhaseStep:0.1,
frameDurationTolerance:0,maxSourceSpanFrames:8}`. Tolerances describe observed raw
phase/frame evidence, not hours. Each range needs at least two samples. Bounds
that were never observed or are no longer retained, crossed gaps, missing or
paused/wrapping/backwards clocks and incompatible definitions/cohorts refuse a
comparable verdict. Ranges are never silently clamped to retention.

Different absolute dates/frames between windows are allowed. Every point must
have a unique ordered phase match, compatible positive frame duration and bounded
original source span. The comparison preserves the producer's explicit clock
`sourceObservationId`, including State fallback; it does not guess from overview
availability. Matching structural fingerprints establish only equality of the
sampled structure. Missing/incomplete/changed or recycled line members cannot
produce gain for the original cohort, and caller reconciliation receipts reject.

Results contain qualified paired values/deltas and first-to-last stock/count
changes. Every `improvement` is null. There are no irregular unweighted averages,
interpolation, rate integration or zero filling. A complete comparison exceeding
256KiB gets `RESULT_TOO_LARGE`; request narrower sequence ranges. No pair is
silently dropped. `rawNormalizedOnly` and `nativeCalendarParityNotRun` remain
explicit: sparse observations cannot certify continuous coverage, no unseen
pause/wrap, native hours, native source refresh or service performance.

The four history tools are reported separately as `clientOnlyCapabilities` by
`cs2_capabilities`; they are not native endpoint descriptors. Actual registration
and supplied client visibility remain separate evidence. The MCP stdio owner
handles EOF, stream failure, SDK close and SIGINT/SIGTERM through normal SDK close,
removing its listeners/timer and preserving earlier callbacks. No writer timeout
or simulation timer semantics change.
