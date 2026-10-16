# M4 workforce vacancies by education — implementation plan

October 16, 2026. Implementation plan based on the read-only source/design pass at `db066079d483b678a787d32e4801d0fb77b2446e`, branch `codex/construction-relationships`. Root selected this scope under Eric's continuing roadmap authorization. Implementation proceeds on `codex/workforce-observations`; the frozen execution rulings below govern scope and review. Packaging, installation, gameplay, merges and deployment remain paused.

## Recommendation

Implement a focused **M4 workplace-slot breakdown by education**, consumed through the existing catalog/direct metric/O7/M5 collection and stored comparison workflow. Ten useful count metrics are already available in the exact typed labor source: total and free workplace slots for each of five native education buckets. This is a stronger immediate increment than unit-unknown utility snapshots, and more usable than an explicit zone-cell API whose callers cannot currently obtain block identities through a bounded read.

The consumer can collect and compare how reported vacancies and total job slots change in each education bucket during city growth or a separately authorized intervention. These are workplace-demand-side counts, **not** employed citizens by qualification, evidence of a worker shortage, school enrollment/capacity, employment ratios, or an automatic recommendation to build education. Existing `labor.jobs.total/free` remain available. No count conservation equality or derived fill/vacancy ratio is asserted.

## Candidate comparison and exact anchors

| Candidate | Available evidence | Missing dependency/limitation | Decision |
| --- | --- | --- | --- |
| Workplace-slot education breakdown | `RequestHandlers.CityData.cs:80–110`: exactly the same two `GetTotalWorkplaces`/`GetFreeWorkplaces` structs already consumed by accepted total/free metrics; every bucket explicitly assigned. `Core/CityObservationData.cs:80–94`: five Int32 fields in each typed `EducationCounters`. `Core/DiagnosticMetrics.cs:70–73`: accepted count/workplaceSlots/null-period contract for these structs' totals. | Native getter internals/performance and panel semantics remain unverified, as with accepted labor totals; does not identify supply or facilities. | **Best coherent source increment.** No new native reader, enumeration or source interface required. |
| Cached utility snapshots | `RequestHandlers.CityData.cs:24–65`: five system lookups and fifteen explicit source-property assignments. `Core/CityObservationData.cs:49–78`: fixed numeric typed DTO fields. Existing O7 catalog dispatch consumes this typed source (`RequestHandlers.Observations.cs:12`, `Core/ObservationCatalog.cs:185–187`). | No verified physical units, scale, rate period, compatibility of operands, native refresh timestamp, or local service-network health. Additional metric-source wiring and max-source accounting needed. | Feasible **raw snapshots only**; useful retained values, but comparison arithmetic must remain unavailable while units are null. Lower priority than known count metrics. |
| C3 explicit block/cell read/computed selection | `RequestHandlers.Zoning.cs:151–179`: `Block.m_Size/m_Position`, row-major `z*size.x+x`, `Cell.m_State/m_Zone`, `ZoneUtils.GetCellPosition`; `RequestHandlers.Perception.cs:309–340`: zone index and Occupied flag. | Both existing block consumers enumerate `ZoneBlockQuery.ToEntityArray` (`Zoning.cs:147`, `Perception.cs:296`). Legacy `/zoning` outputs aggregates, not block refs; C1 reader/relationship sources do not expose zone blocks. No bounded discovery consumer, native corner/frontage/side mapping, occupation/application equivalence or geometry-fingerprint rule is proven. | Hold full C3. An explicit-only API has weak current usefulness and must not be made discoverable through an unbounded scan hidden behind an output cap. |
| General grid maps | `RequestHandlers.Perception.cs:188–235` explicitly calls `deps.Complete`; `:190–250` infers square dimensions with sqrt and fixed world extents; `:257–264` copies every map element. | Need authoritative map coordinates/layout plus a source-bounded access strategy without job waiting/full copy. | Not suitable for bounded O7 now. |

The original C3 plan (`2026-10-11-bridge-construction.md:106–119`) also requires native corners, owning-road/side and occupation proof, world/block/fingerprint validation, and mutation lifecycle/preview/native gates. The retained decision (`IMPLEMENTATION-DECISIONS.md:117`) allows computed selections as reads; it does not prove these native facts or waive discovery/work bounds. Do not claim C3 complete or implement paint/undo/preview scaffolding here.

## Required ten-metric contract

Retain existing `labor` family; add the following fixed parameterless city-scope keys:

- `labor.jobs.totalByEducation.uneducated`, `.poorlyEducated`, `.educated`, `.wellEducated`, `.highlyEducated`.
- `labor.jobs.freeByEducation.uneducated`, `.poorlyEducated`, `.educated`, `.wellEducated`, `.highlyEducated`.

The shorthand suffixes above expand against their full respective prefixes. Every definition has `kind:"count"`, `unit:"workplaceSlots"`, `period:null`, `refresh:"cachedSimulationCounterRefreshTimeUnknown"`, native support unverified. Exact sources are `CountWorkplacesSystem.GetTotalWorkplaces.m_Uneducated/m_PoorlyEducated/m_Educated/m_WellEducated/m_HighlyEducated` and the corresponding `GetFreeWorkplaces` fields. These are the same concrete `Workplaces` fields copied at `CityData.cs:98–110`, not arbitrary JSON paths or an invented getter.

Project directly from `CityLaborData.jobs.totalByEducation/freeByEducation` in the existing `DiagnosticMetricReader` switch. Reuse its finite/nonnegative/safe-integer count validation (`DiagnosticMetrics.cs:299–321`), one-read-per-required-source selection (`:365–377`), original-world fencing and recorded source/State clock evidence. Null labor/jobs/sub-counter objects produce unavailable values with a specific missing-source/counter reason; present zero remains supported. No missing field should turn into a supported zero. The actual native reader assigns every bucket explicitly, so no new population-style default-presence heuristic is required; prove null intermediary behavior in projection tests.

Update catalog size from16 to26 and mirror exact key/source/meaning definitions in the existing TS contract. Retain `diagnosticMetrics.v1`: this is an additive key extension with unchanged wire schema and unchanged meanings for all sixteen existing keys. Raise the catalog cardinality bound to26, not any transport/history bound. Histories already pin their actual selected definitions from the authoritative catalog. A new client must still accept valid older16-key catalogs and old-key batches; requesting an absent new key must refuse before polling. The newer catalog is not promised compatible with older client validators that cap it at16. Document that distinction. No schemaVersion change, second registry or forced revision migration is needed.

No endpoint or MCP tool is added. Existing `cs2_metric_catalog`, `cs2_observe`, fixed `metrics.read` in `cs2_observe_batch`, and collect/stop/read/compare are the consumed user path. Existing legacy `/city/labor` JSON stays unchanged. Existing transit/city scope restrictions, empty parameters and duplicate-key rejection remain exact.

One source-based integration risk to include in the source brief: `diagnostic-metrics.ts:8–10,20` still registers a whole-object refined `toolRequest`, the same SDK shape category fixed for O7 in R1. This pass did not rerun SDK probes. New-key discovery should be tested through actual listTools; if the inferred hidden-schema risk reproduces, use the accepted bounded object facade and explicitly retain full `toolRequest` refinement before HTTP (exactly keys XOR queries). Do not merely unwrap refinement and silently accept both. Reuse shared `boundedObjectInput`; retain default family and nested requirements. Test bounded SDK diagnostics at the consumed metric facade. This is a consumed integration fix, not permission to refactor unrelated registrations.

## Bounds and testing proposal

Native work remains one existing labor read per metric batch regardless of ten new keys: the same two workplace getters, fixed existing household property reads and five-plus-five bucket copies. No extra getter call, job wait, query, array materialization, buffer traversal or per-metric native lookup. The source count stays at the existing maximum4 (no `city.services` addition); State fallback still uses producer-owned clock source evidence. Opaque native getters are nonpreemptible and have unverified latency: fixed bridge call count is not a wall-time certification.

Keep every accepted bound:1–64 strict metric queries (only26 distinct known keys possible), original-world admission; GET encoded query cap12000 bytes; direct response256KiB; O7 item256KiB/group4MiB,1–64 item IDs and shared scheduler; MCP Node18 floor; M5 global two network owners, eight retained+pending series,512 samples,128KiB/sample,1MiB/series,8MiB total,64 detailed gaps,256KiB result cap and bounded pagination. Do not raise sample/output budgets because the catalog grows. Largest projection/catalog/collect metadata still must go through actual serialized-byte checks.

Meaningful RED first should show new keys rejected by current actual production parser/catalog and actual MCP workflow, then GREEN should cover:

1. Exact26-key catalog and ten definitions with source/unit/kind/period/refresh; no rate/ratio/physical-utility additions; unknown key/parameter, duplicate key and wrong selected-line scope still reject before source work/HTTP.
2. Production reader projects distinct sentinel values for all ten fields with **one** labor callback and no extra workplace getter calls; total/free buckets cannot swap. Null nested DTOs, exceptions, negative counters, zero and world/readiness transitions preserve unavailable/zero/frame/clock semantics. Never require bucket sums to equal totals.
3. Actual typed `ReadLaborData` binding plus real Newtonsoft serialization into direct/O7 wire fixtures, distinguished from real installed Game/Unity binding. Consume emitted wire with TS exact key/source/value validation and catalog size/revision. Keep old metric meanings and existing family counts correct.
4. Actual SDK listTools/direct GET/O7 POST discover and forward new keys; strict required/default/full-ref and keys-vs-queries contracts; malformed/huge unknown input errors remain bounded and make zero HTTP calls. Retain caller cancellation and prior C1 directDepth1 discovery tests.
5. Actual MCP catalog → collect using `metrics.read` → stop → paged read → stored aligned comparison over at least two controlled samples per window. Assert per-bucket count deltas/changes from actual retained facts, State source clock, no zero fill, no cross-bucket aggregation, improvement:null. Mismatch/gap/unavailable scenarios still refuse qualified comparison. Existing serialized storage/result limits remain active.

Likely source/test set: `Core/DiagnosticMetrics.cs`, mirrored `mcp-server/src/diagnostic-metric-contracts.ts`, `diagnostic-metrics.ts`/history revision constant as needed; exact catalog/generation fixture updates; `DiagnosticMetricTests.cs`, `DiagnosticMetricWireTests.cs`, focused Node metric/O7/history/comparison and actual SDK tests. Prefer existing `RequestHandlers.Metrics`/`ReadLaborData` untouched except test linkage needed for actual source binding. Documentation should explain vacancies by native education bucket and remain explicit that worker supply/education service capacity are not measured. Run focused changed suites plus canonical linked builds/exact-source harness as appropriate, not absent PowerShell/GamePath/native suites.

## If root instead selects utilities

The fifteen source-backed values are ElectricityStatisticsSystem.production/consumption/fulfilledConsumption/batteryCharge/batteryCapacity; ElectricityTradeSystem.import/export; WaterStatisticsSystem.freshCapacity/freshConsumption/sewageCapacity/sewageConsumption; WaterTradeSystem.freshImport/freshExport/sewageExport; GarbageAccumulationSystem.garbageAccumulation. Use explicit raw labels/keys, `kind:"state"`, **unit:null and period:null**. No watts/kWh/m3/kg/month or synthetic source-specific unit string to unlock arithmetic. Keep rate-looking names as raw field names, not certified rates. No derived surplus, headroom, fulfillment, ratios, service performance or local connectivity inference.

`MetricValue.support:"supported"` may mean only that the declared finite raw property was obtained; catalog native support stays unverified and labels/docs must make unknown units clear. `observation-comparison.ts:90–98` refuses arithmetic when definition.unit is null; retain that guard. Using state kind also avoids first-to-last count/stock changes at `:102–112`. M5 collection/read can retain raw points with source evidence, but paired deltas/relativeChange and overall comparable verdict remain unavailable for those keys. Physical-unit or explicit raw-arithmetic policy is a separate evidence/ruling task, not a shortcut in this increment.

Utility wiring would add `Services()` to `IDiagnosticMetricSources`/actual handler, one `ReadSource("city.services",...)` per batch and exact source selection in TS. Update allowed source IDs and worst-case source count4→5 (overview, labor, budget, services, plus State fallback when overview clock is absent); all output caps remain fixed. Adding all15 yields31 catalog definitions. None of this establishes hidden getter internals or native refresh time. It is feasible, but workplace counts provide a more complete immediately consumable diagnostic/comparison outcome with less new integration.

## Genuine gates and owner decision

There is **no new native-semantic blocker for the narrow ten workplace-count projection** beyond accepted cached-count/native-notRun limitations. Its workplaceSlots contract follows the same proven source structs and count family already used by accepted totals. Native DLL binding/performance/panel validation remains pending; this source increment cannot close those gates or M1 facility capacity.

C3 requires bounded block-reference discovery and authoritative frontage/corner/occupation evidence before a useful exact selection release; no safe fallback scan is proposed. Utility physical units/periods remain genuinely missing. No M3 tree/points/settings re-audit or capability placeholder was added. The preflight required root scope selection and source GO; the frozen execution rulings below record that decision.

## Frozen execution rulings

Base is `db066079d483b678a787d32e4801d0fb77b2446e`; source branch is
`codex/workforce-observations`, stacked on accepted PR7. Root owns this plan
and the final acceptance receipt. One source implementer and one independent
reviewer are reused; no new workers or parallel source edits. Eric explicitly
authorizes ongoing draft PR creation/updates with feature/test evidence.
Packaging, installation, gameplay, merges and deployment remain paused.

Scope is the ten count projections and their existing consumed catalog/observe/
O7/history interfaces. Preserve diagnosticMetrics.v1 for additive compatibility
as ruled above. Use existing GET/direct O7 native producer paths and bounds.
No age breakdown, utility field, ratio, worker-supply model, derived occupancy,
shortage diagnosis or capacity/control endpoint is added. Native totals and
tier counts remain separate observed fields; do not enforce unproven equality.

All ten keys must be actually discoverable in relevant MCP schemas. Reproduce
any refined-root discovery defect at cs2_observe before fixing it; preserve
keys XOR queries with explicit original-schema parsing in the real handler.
Use the accepted bounded object helper at the consumed metric facades where
needed; no broad tool refactor. Keep family default all, world/scope requirements,
strict keys/parameters, cancellation semantics and independent output bounds.
Test legacy16catalog/oldkey compatibility and absent-new-key refusal explicitly.

Meaningful behavioral RED/GREEN and exact-source/actual-serializer/actual-SDK
checks are required. Link the existing native labor assignment method into a
minimal boundary seam only if needed to establish all ten mappings/getter counts;
record stubs as source verification, never installed native binding. Do not
repeat known unavailable PowerShell/native suites or install dependencies.
After the scoped commit, root dispatches exact-diff independent specification,
quality/security and whole-increment integration review before acceptance.
