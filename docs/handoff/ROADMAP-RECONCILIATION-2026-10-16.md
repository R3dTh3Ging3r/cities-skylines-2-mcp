# Roadmap reconciliation after PRs 4–8

**October 17 post-playtest update:** The historical source-only assessment below
is now supplemented by [the completed native playtest reconciliation](POST-PLAYTEST-2026-10-17.md).
The tested PR9 candidate is `0b913a3dcd41b0795e8c130aa81ee4c344a6c099`.
Selected C1/C5/O7/M4/M5/T1 reads and a nine-mutation legacy construction expansion
were observed successfully. Earlier tool-exposure blockers were resolved locally.
This does not close G1 completion/takeover/save attribution or broad G2–G4 gates.
The observed relationship-inventory overflow motivates bounded read recovery;
the [next lifecycle sequence](../superpowers/plans/2026-10-17-construction-lifecycle-next.md)
prepares consumed observation before changing writer semantics. No new native
test, installation or gameplay session is requested by these updates.

Started October 16, completed October 17, 2026. Reconciled from source at
`d1840f736978748a6778feff44aba62a24ab9a98` and GitHub readback of all five
open, unmerged draft PRs. This is a source delivery assessment, not native
acceptance. PC packaging, installation, gameplay, merge and deployment remain
paused. The dated original roadmap and task plans remain the requirements;
partial source acceptance does not check off their full exit conditions.

## Published stack and actual scope

| Draft | Exact published head | Accepted source capability | Still outside acceptance |
| --- | --- | --- | --- |
| [4](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/4) | `88d1965c167d6220aa82e7bab4af687cce15d612` | Partial O2: managed world identity, bounded registry, strict UUID/request transport and refusal gates | Native submit/status/cancel authority, completion, all-writer lease and migration |
| [5](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/5) | `07aca0d2e83084253b4d126a469a95391139bdc6` | Partial C1: exact versioned roots, full network cubic/endpoints, root building oriented bounds | Spatial discovery/paging, full family/connector/platform/approach envelopes and native geometry parity |
| [6](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/6) | `ecc4e2bf12416f99563cc2a9b6235ff2b0387d11` | Deferred-world guards; O7 bounded read groups; selected-line T1 structural reads; M4 sixteen typed metrics; M5 bounded collected histories and stored comparisons | Native operation completion; mutation batches/takeover; measured traffic/load/headway/utility health and native clock/panel parity |
| [7](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/7) | `db066079d483b678a787d32e4801d0fb77b2446e` | Partial C1: bounded depth-one Owner/Attached/child/upgrade/subnet/subarea slots and supported target geometry | Recursive or complete facility inventory, connector roles, capacity, native preview and edits |
| [8](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/8) | `d1840f736978748a6778feff44aba62a24ab9a98` | M4/M5 extension: ten total/free workplace counts by education tier, consumed by direct/O7/history tools; actual metric SDK discovery/errors fixed | Worker supply or labor-shortage diagnosis, education capacity, native parity and original broader M4/M5 requirements |

PR4 is stacked on PR2; PR5–8 form the successive stack shown above. The separate
PR3 curve smoke receipt is narrower than G1–G4: it does not validate these newer
candidates or enable writers. Earlier F1–F6 offline foundation acceptance and O1
fixture preparation remain accepted; O1 native investigation remains incomplete.

## Roadmap status

| Roadmap / tasks | Delivered portion | Remaining feature and dependency |
| --- | --- | --- |
| R01 / F1–F6, T1 | Encoding/errors/catalog/qualified units and bounded structural transit detail | Installed capability/panel parity, calendar and cargo units remain native checks |
| R02 / O2–O4 | Registry/world preparation and selected deferred-world guards | Verified apply/finish/cleanup, reconciled receipts, save result/durable artifact identity: G1 |
| R03 / O5–O7 | O7 read scheduling and bounded groups | All-writer lease/player takeover, owned wall-clock simulation and resumable mutation batches: O3/O4/O5 and G1 |
| R04 / C3 | No exact-cell feature | Bounded block discovery, native corners/frontage/occupation and apply equivalence; lifecycle plus G2 |
| R05 / C4–C6 | C1 cubic readback only | Overlay masks/preservation, 3D snapping/elevation and complete junction actions: C2 preview, lifecycle, G2 |
| R07 / C1–C2, C7 | Exact geometry and direct relationships | Complete inventory/connector semantics, native previews/cleanup, reservations tied to real validation, facility preservation: G2 and M1; persistence also save identity |
| R06 / T1–T4 | Selected-line structural evidence | Verified loads/health/discovery, cargo rail/air, passenger air and original-route edits: lifecycle, native route preview and separate G3 gates |
| R08 / M1–M2 | Existing city aggregates and budget observations | Per-family capacity/usage/storage/efficiency, aggregation and observational loan quote: G4 source/panel evidence |
| R09 / M3 | Existing XP and lock/warming evidence | Stable development tree/node identity, points/settings and normal purchase action: G4, with lifecycle for purchase |
| R10 / C8–C9 | Direct owner/attachment/area references only | Native owner-area and object geometry/roles/preservation; edits need C2/lifecycle and M1 or T4 where applicable |
| R11 / M4–M5 | Twenty-six typed keys, bounded observation/history/comparison workflow | Local utility flow, production/resource grids, real traffic/queue/load/headway, happiness/severity and verified clock/unit context: source-specific native evidence |
| R12 / O1/O8 and feature gates | Offline evidence discipline and scoped source reviews | Exact-candidate native compile/runtime fixtures, release matrix and rollback acceptance; no release claim |

## Offline work versus native prerequisites

A useful offline candidate remains: **computed selected-network geometry
inspection** using C1's already exposed exact full cubic curves. It can provide
ordered positions, grade and curvature samples with original identity/frame
provenance. This needs no invented native method, global query or writer. It is
a bounded read-only portion of C5 diagnosis, not C5 connection/snapping/apply.
Its exact consumer contract and corrected-source review are recorded in the
[October 17 acceptance receipt](NETWORK-GEOMETRY-2026-10-17.md).

| Remaining work | Offline preparation possible now | Evidence needed for a useful native feature |
| --- | --- | --- |
| Selected-network geometry diagnosis | Consume actual bounded C1 reads; calculate profiles with analytic derivatives; strict SDK/transport/math tests | Installed curve binding/parity remains a separate check; native design limits, clearances and feasibility are not inferred |
| O3/O4 completion and saves | Pure transition/task-result tests and bounded instrumentation design | Apply/Finish/cleanup order and late effects; native save event/result/destination package attribution and preview ownership |
| O5/O6/O7 mutation control | Lease, clock and batch policy tests | Consumed all-writer adapters, visible takeover hook, actual pause/late-effect receipts and G1 completion; policy-only shells would not make a playable batch |
| C1/C3 discovery and selections | Pure intersection/polygon algorithms and explicit-ref schema checks | A bounded native discovery/iteration path, stable snapshots/cursors, block corners/frontage/occupation evidence; existing whole-query materialization does not supply this |
| C2/C4–C9 native construction | Validation math and input contracts | Native temporary preview lifecycle/cleanup, exact source roles/masks, limits and action-specific preservation/effects; ownership or curve readback alone does not prove them |
| T1–T4 transit | Exact-selector/preservation policies and structural comparisons | Directed native path/stop validity, purpose-specific gates/dispatch/load units, preview cleanup and original-route edit definitions; structural segment refs do not prove operation |
| M1/M2 facility/finance | Aggregation and estimate policies after definitions are supplied | Per-family capacity/usage/staff/storage/efficiency and upgrade semantics; observational loan-offer behavior; raw Employee length and budget efficiency factor are insufficient |
| M3 progression/settings | Node/version/point admission policies once source identities are known | Stable native tree/point/settings readers and normal purchase action; existing XP/lock state is not the development tree |
| Remaining M4/M5 diagnostics | More raw cached fields could be exposed, and existing histories can retain them | Utility units/periods/local connectivity, native grid layout and bounded access, production identity, notification severity and measured traffic/load/headway semantics |

Adding unconsumed policy classes or repeatedly exposing small unit-unknown
counters would not meet the requested larger playable-batch objective. The
selected geometry workflow is useful without those claims; after it, reassess
against actual evidence rather than assuming another metric is progress.

## Concrete native boundary and next owner step

Installed-assembly metadata inspection and `SkipDeploy=true` compilation are
PC **offline** work: they can establish signatures and compile compatibility,
not native runtime behavior. No Game/Unity assemblies are available in this
cloud checkout. A running disposable fixture is separately required for
callback order, cleanup, durable save attribution, tool takeover and panel parity.
Neither step is executed or authorized by this reconciliation.

The highest-leverage next native checkpoint is **G1 lifecycle evidence**, which
unblocks reliable operations, then preview-dependent productivity features.
Retained metadata already shows `ToolSystem.activeTool` assignment/event differs
from later tool stopping; generic placement currently replies at Apply before
Finish. `GameManager.Save` task success can differ from its saveSuccess flag;
the save event URI refers to a preview texture. These concrete unknowns prevent
truthful verified-completion and checkpoint contracts.

The owner can next authorize a bounded PC investigation of these signals and a
disposable-fixture observation plan. First inspect the installed build and add
reviewable, bounded observational instrumentation where needed; then explicitly
authorize any runtime/operator actions. O1's collector currently refuses native
mutation dispatch and the v2 writers remain disabled. Do not bypass those gates
or assume the existing lifecycle fixture can already perform the full probe.
After evidence, implement and review O3 completion plus O5 writer authority,
with O4 save identity and O6/O7 consumers following their proven dependencies.
C3 zoning, C4 overlays and T2 cargo each still need their specific native gate.

Read-only G2/G4 investigations can proceed independently if later authorized:
complete connectors/platform envelopes, bounded block discovery, facility
panels/units and progression source identities. They do not require inventing
C2 previews or claiming all G1 work completed first.

Source anchors: `RequestHandlers.Operations.cs` refuses scoped status/cancel;
`BridgeSystem.cs` constructs a refusing operation authority;
`BridgeToolSystem.cs` replies in Stage.Apply;
`RequestHandlers.Meta.cs` discards the Save task;
`RequestHandlers.Zoning.cs` and `Perception.cs` materialize whole zone queries;
`RequestHandlers.Perception.cs` waits on map dependencies and copies map data;
`RequestHandlers.Management.cs` calls both RequestLoanOffer and ChangeLoan.
See [retained lifecycle metadata and limitations](../native/lifecycle.md),
[construction gates](../native/construction-gates.md) and the five linked
[implementation packages](../superpowers/plans/2026-10-11-bridge-implementation.md).
