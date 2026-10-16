# Larger city observation and reliability batch

Eric requested substantially more development before another PC package/playtest.
PC packaging is paused; preserve its checkpoint. Base is reviewed geometry
candidate `07aca0d2e83084253b4d126a469a95391139bdc6`. Work on the isolated
`codex/city-observation-workflow` branch; preserve PRs2–5 and d21a563. One source
implementer at a time, followed by independent Sol6.1/high review per increment.
Do not merge, install, launch/connect to the game, alter saves/configuration or
request repeated small PC packages. Publish a reviewed cohesive draft batch.

## Intended workflow

Gather city context and selected infrastructure in bounded observation groups,
inspect exact transit routes and their infrastructure, collect a limited history,
and compare like-for-like observations with gaps/unknowns intact. Protect delayed
rename, tile refresh, screenshot and timed-pause work across world transitions.
This expands useful planning/diagnosis while native write gates remain open.
It is not a promise of full operations, facility, cargo or construction completion.

## Sequential increments

1. **Deferred-world protection (O3 slice).** Consume an actual shared continuation
   fence at rename/response closure, tile refresh, screenshot and timed-pause
   sites. Capture the original world; check before native access/publication;
   discard stale deferred work without touching the replacement city. Preserve
   unknown/partial effects of prior actions; do not turn a rename failure into
   permission to repeat creation. No speculative native cleanup or unused
   operation coordinator. Disarm old timers on load/unavailability/destruction.
2. **Bounded observation groups (O7 read half).** Implement POST /v2/observations,
   typed BridgeClient.observe and cs2_observe_batch, sharing the real dispatch
   budget and pending-item capacity. Fixed typed source-bounded readers only:
   state, overview, cached services/labor/budget and explicit geometry, where
   each adapter's work/output bounds are established. Reuse typed native data
   readers rather than parsing serialized legacy JSON. No arbitrary URL or
   mutation dispatch; no unbounded scans/job-waiting helpers. Follow retained
   O7 limits and shared Observation shape exactly.
3. **Selected transit diagnostics (T1 slice).** Exact versioned line/stop reads
   with bounded waypoint/segment buffers, XYZ and owner/purpose facts, missing
   references and qualified lengths. Integrate the fixed observation catalog.
   Unknown native load/dispatch/committed-route validity stays unknown. No global
   line scan or hidden unbounded UI helper; discovery remains through legacy
   tools until bounded native discovery is established.
4. **Typed metric projection (M4 slice).** Add a small real metric catalog and
   production projection over supported city/transit readers, with keyed
   MetricValues and versioned member refs. Each key records source, unit, period,
   refresh limitations and support. Unknown units/refresh remain explicit; no
   arbitrary JSON-path extraction or inference of disconnected-consumer health
   from aggregate surplus. This is the consumed prerequisite for collected
   comparisons, not a claim of full utility/production diagnostics.
5. **Collected history and comparisons (M5 slice).** Use the real observation
   batch transport to collect bounded world-scoped series: at most8 series,
   max512 samples each, interval>=1000ms, no overlapping polls, explicit missed
   samples/eviction gaps, stop/disconnect cleanup. Register collect/stop/read/
   compare tools. Compare only source-defined metrics with compatible units,
   periods, windows and unchanged versioned cohorts. Bound total bytes and gap
   retention as well as samples; use global polling backpressure and late-response
   fences. Missing/new/rebuilt members
   cannot count as improvement; unknown units/native histories stay unknown.
   No automatic simulation control, mutation or file persistence.

## Shared execution requirements

Read the original operations/construction/transit/management plans and retained
IMPLEMENTATION-DECISIONS. Source-backed preflights are in workspace handoffs;
retain essential evidence/limitations in tracked task docs. Broader tasks remain
unchecked when this batch implements only a named slice. Native G1–G4 and
feature fixtures remain notRun until separately authorized PC validation.

O7 admission validates all1–64 unique items before dispatch; captures original
ready world even without expectedWorld; reserves pending slots atomically per
item against256; permits8 active groups. All normal/group work shares at most
8 reads or2ms additional work/update. Cap item JSON256KiB and group4MiB. Preserve
one ordered observation/error per item, original completed-item metadata, null
no-dispatch frames and worldChanged/endFrame=null across loads. Detach cancels
undispatched read-only work and releases slots exactly once. Ordinary mutation
timeouts retain their unknown-outcome semantics.

Tests exercise linked production seams and actual MCP registration/transport,
not duplicate policy logic. Run focused behavioral failing/passing regressions,
canonical net472 compilation and exact-source portable execution as appropriate.
Use existing dependencies. Do not repeat known missing-PowerShell/native runtime
failures; report them. Broaden final regression testing when integration changes
justify it. Independent review checks whole task and shared integration before
the next source task; fix Important findings and review their correction.

## Outlook and held work

A larger source candidate is feasible through several implementation/review
passes; October17 native playability is conditional, not a guaranteed deadline.
Full O3 native completion/cleanup, O4 save identity, O5 all-writer takeover/lease,
O6 supervised runs, O7 mutation batches and O8 native release remain unfinished.
M1 facility capacity/usage/efficiency lacks confirmed native field semantics here;
do not replace it with raw employee counts or budget factors. Exact zoning,
overlays, cargo creation and facility transformations still require their
completion/lease/preview and native gates. Keep developing supported work without
inventing APIs or enabling unverified native mutations to satisfy a date.
Depth1 infrastructure relationship expansion is a useful later C1 extension;
prioritize the consumed M4/M5 metrics workflow before adding it to this batch.

## Preserved implementation details

The reviewed execution scope is expanded in the
[selected transit design](2026-10-16-selected-transit-detail.md) and
[metric/history design](2026-10-16-metrics-history-detail.md). These preserve full
world-bearing EntityRefs, unknown filter evidence, raw segment-distance units,
fixed metric definitions, exact cohorts and compatible observed time windows.
Their design-only status means source work awaits the dependency review/explicit
root task assignment; it does not require a new user approval for this authorized
batch. Actual completed source and checks are recorded in the handoff receipt.
