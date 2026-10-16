# Workplace counts by education — source acceptance

The workforce observation increment is accepted at
`4d28065be1ae1af1f153be6964a415a2a5802fd8`, based on accepted construction
relationships `db066079d483b678a787d32e4801d0fb77b2446e`. Independent
specification, quality/security and complete-increment integration review passed
with no Important findings. Root accepts those verdicts. The increment is
preserved on `codex/workforce-observations`, stacked on
`codex/construction-relationships` (draft PR7).

## Available behavior

Ten fixed city metrics expose total and free workplace slots for uneducated,
poorly educated, educated, well educated and highly educated native buckets.
They flow through the existing metric catalog, direct reads, O7 `metrics.read`
and process-local collection, stop, paged reads and stored comparisons. Each
bucket can be compared separately over qualified observation windows. These
counts do not measure resident qualifications or worker supply, prove a labor
shortage, or recommend education capacity. Improvement remains null.

Every definition is count/workplaceSlots with period:null and unknown cached
refresh time. Null counter objects remain unavailable; present zero remains
supported. Invalid negative counts affect only their individual values. No sum,
ratio, cross-bucket aggregate or derived filled count is asserted. All selected
labor metrics reuse one existing typed labor callback. The actual native labor
assignments are unchanged.

The catalog expands from sixteen to twenty-six keys while retaining
`diagnosticMetrics.v1` and every original definition. Updated clients accept
valid older sixteen-key catalogs and old-key batches; selecting an absent new
key refuses collection before polling. Old clients with a sixteen-key catalog
limit are not promised compatibility with the expanded producer. Existing
histories continue to pin their selected authoritative definitions.

The actual SDK metric inputs now expose their required/default/nested schemas
and bounded known-key lists. Reusing the bounded object helper also caps malformed
input errors. The original keys XOR queries refinement remains explicit in the
real handler before normalization or HTTP. Strict scope, world, parameters and
query constraints remain enforced.

No endpoint, MCP tool, native reader, writer authority or limit is added. Direct
and O7 item results remain 256KiB, groups 4MiB, and metric queries 1–64 with the
existing GET encoding limit. History retains two global network owners, eight
series, 512 samples, 128KiB/sample, 1MiB/series, 8MiB total, 64 detailed gaps and
256KiB paged tool output. Original world/clock and cancellation rules remain.

## Verification

At the accepted source, both canonical net472 test projects compiled with zero
warnings/errors and `SkipDeploy=true`. Exact-source portable execution passed
22 metric, 13 O7 and 32 response/serializer checks. TypeScript build and 73
focused Node tests passed. The same eight workforce tests also passed using
the checked-in synthetic wire fixture; those are not eight additional tests.

Independent review ran 22 metric, 32 response and 35 focused Node checks plus
TypeScript build. The Node run consumed fresh actual-producer/serializer wire;
comparison with the checked-in fixture differed only in sampledAtUtc values.
Tests cover all ten distinct mappings, null/zero/independent invalid buckets,
old catalogs and absent keys, actual SDK discovery/bounded errors/XOR admission,
and catalog → collection → stop → pagination → stored count comparisons.
Stored comparison uses retained observations without new HTTP requests and
refuses missing/gapped/incompatible clock or cohort evidence.

Behavioral RED evidence and intermediate fixture/schema-test corrections remain
in the local implementation report, with unsuccessful checkpoints distinguished
from the final passing runs. No installation or unavailable suite was needed.

## Native and roadmap limits

Portable tests execute actual producer/handler/serializer code against controlled
typed boundary data. The unchanged Game-backed ReadLaborData assignment method
was source-inspected only; its native getters were not executed. Installed
Game/Unity binding, Framework runtime execution, getter internals/performance,
refresh time, calendar conversion and panel parity remain unverified. Fixed
callback counts do not certify hidden native work or wall-time limits.

Facility capacity/usage/efficiency, worker supply, utilities, C3 bounded discovery
and frontage/occupation, construction previews/mutations and broader native gates
remain open. This acceptance does not complete full M4/M5 or authorize release.
Packaging, installation, gameplay, merge and deployment remain paused. The
original checkout and recovery snapshot remain preserved.

See the [frozen plan and source anchors](../superpowers/plans/2026-10-16-workforce-observations.md)
and [observation contract](../observations.md).
