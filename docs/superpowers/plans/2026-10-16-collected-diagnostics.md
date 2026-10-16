# Typed and collected diagnostics — larger-batch brief

Implement after accepted O7 observation groups and selected T1 readers. This is
the M4/M5 slice in the larger batch plan, not complete utility/production/service
analytics. Read original management contracts and retained rulings. No gameplay,
simulation control, configuration, persistence or new writer capability.

## M4 consumed typed metrics

Use the planned cs2_metric_catalog / cs2_observe tools and GET metrics catalog/
metrics routes with the shared strict transport. Add metrics to the fixed O7
catalog using the same producer. Implement a BCL metric catalog/projection over
the actual typed city and selected-transit readers, not arbitrary JSON paths.
Preserve MetricValue/MetricDefinition semantics from the management plan. Select
1–64 known keys, explicit observed world and only supported explicit scopes;
reject unknown keys/args/area/pagination/facility requests instead of ignoring.
Read each required fixed source once per observation, not once per key.

Initial useful keys may include population, money/XP, fixed labor/job counts,
signed monthly budget income/expenses/balance, and selected route structural
counts/qualified lengths. Every key requires a source-backed field, declared
unit/period/refresh/support and finite-number handling. Unknown unit/conversion
is unavailable/qualified, not persons/kg or an invented rate. Do not derive a
service-health verdict from aggregate surplus. Preserve budget sign conventions
and unknown source-refresh time when paused. Unsupported native load and capacity
stay unavailable. Catalog entries must have real production consumers/tests.

Use one explicitly documented MetricBatch projection for collection: keyed
MetricValues, original per-source observation frames/freshness/warnings and exact
member refs; any wrapper difference from direct dictionary reads must be explicit.
Capture raw normalized time metadata from the existing TimeSystem reader when
available. Do not relabel it as verified clock hours. Native panel parity remains
unverified. Preserve all direct reads and legacy response shapes.

## M5 real collection lifecycle

Register the planned collect/stop/read/compare tools. Collection starts only by
explicit tool call, through BridgeClient.observe against the fixed metric reader;
no client-submitted observations or arbitrary fetch targets. SeriesSpec contains
observed world,1–64 metric keys, explicit fixed versioned cohort, maxSamples1–512,
intervalMs>=1000. At most8 retained series; reject capacity rather than silently
delete another user's series. Stopped histories remain readable. Process-scoped
IDs, no file export/persistence in this slice.

One in-flight poll per series plus a global polling limit/backpressure. Use an
injected monotonic clock for scheduling, retaining UTC only as observation data.
Do not catch up missed intervals with bursts. Record bounded explicit gaps for
misses/errors/evictions; cap retained bytes as well as count (choose/document a
small fixed total and per-series cap), metric-key/string sizes and gap metadata.
Request abort/cancellation must reach the real read transport so O7 releases its
slots. Stop/disconnect/world mismatch fences late responses by series generation
and clears timers. No old-world response appends to new history. Stop must not
affect any game simulation timer. No automatic restart or world substitution.

Read history reports provenance, schema, bounds, actual sample spans, missing
observations and whether collection remains active. Do not pretend samples are
native historical telemetry, observed departures or continuous coverage. Native
timestamps/source refresh and unavailable values remain qualified.

## Honest comparison

Compare stored series only, using explicit Cohort/PeriodPolicy from the original
M5 plan. Require identical world and metric meaning, compatible units/periods,
matching observed time windows and caller-defined frame-duration tolerance.
Raw normalized time may support explicitly normalized windows; it must not be
silently converted to native hours. Unknown/missing temporal evidence prevents
a comparable verdict. Windows describe captured observations, not caller claims.

Use versioned member identity and available geometry fingerprints; reject or
separate rebuilt/deleted/new/unreconciled members. A new empty route/road cannot
improve the original cohort. Caller-supplied unverified receipts cannot establish
reconciliation. Missing/gap/unavailable metrics never become zero or get filled
from a later sample. Compare only like-for-like values with qualified deltas;
do not call a delta an improvement without a defined source-supported direction.
Inactive is not broken; unsupported freight units never produce occupancy ratios.

## Verification and integration

Production pure metric projection tests check source mapping, units/periods,
signed costs, finite/unavailable handling and one read per source. Node tests
exercise actual tool registration and typed transport with fake bridge replies,
then deterministic injected timer/clock tests for no overlap,8-series/512-sample/
byte limits, skipped intervals, bounded eviction gaps, abort and stale late replies.
Use actual collection/comparison code for fixed cohort/time-window rejection,
world reload, unknown units, missing samples and no false new-member improvement.

Wire lifecycle disposal to actual MCP disconnect/shutdown while preserving
existing callbacks. Expose client-only collection tools separately from native
endpoint capabilities; registration is not client allowlist exposure. Document
exact workflow examples and unsupported fields. Append explicit test files;
run changed suites, builds and justified final portable integration regression.
No repeat missingPowerShell/native checks. Independent review must accept the
whole consumed metric->batch->history->comparison path before draft publication.
