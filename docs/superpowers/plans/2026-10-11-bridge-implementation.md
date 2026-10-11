# CS2 Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the gameplay findings into a bridge that can chain reliable actions, zone precisely, upgrade corridors, build working cargo routes and support deliberate city layouts.

**Architecture:** Retain the existing game mod, loopback HTTP server and Node MCP adapter. Deliver five focused sub-projects with shared operation contracts; each produces usable, independently testable releases. This file is the execution index; the linked plans hold exact files, interfaces, tests and commit steps.

**Tech Stack:** Existing TypeScript/Zod/MCP SDK and C# 9/net472/Unity integration, Node built-in tests, pure C# console tests, disposable-city native fixtures.

**Spec:** [Bridge improvement delivery design](../specs/2026-10-11-bridge-improvements.md), derived from the [roadmap](../../BRIDGE-ROADMAP.md) and its complete B001–B029 traceability.

October 11, 2026 · Proposed for review · Documentation only; no code implemented or game controls used

## Global Constraints

- All game/ECS reads and writes stay on the game thread. A batch performs sequential mutations, never parallel native construction.
- Compile the mod with `-p:SkipDeploy=true`. Deployment requires the game closed, a verified candidate, installed-build readback and a rollback copy.
- Native tests use a disposable city and a separately named verified checkpoint. Do not use the player's developed city as a test fixture.
- Preserve upstream history, attribution and unrelated work. Use focused `codex/` branches in the personal fork. No upstream messages or PRs without user instruction.
- A timeout is an unknown outcome, not cancellation; a successful placement, connected graph, good appearance and working service are separate checks.
- All remaining project-wide constraints in the shared design apply to every linked plan.

## Review Focus

1. Native work can finish after the HTTP wait ends: F2 and operations replay/reconciliation tests prevent duplicate construction.
2. A reload or manual edit can make old IDs look valid: operations world fencing and C2/C3 fingerprints reject them.
3. One writer can bypass the shared guard: F3's exhaustive endpoint inventory and operations writer-matrix test cover management/UI/deferred work too.
4. A preview can succeed while losing stored material or a return route: C7/C8 preservation and T2–T4 directed/operational fixtures test those separately.
5. A passing build can hide an untested feature: F6 evidence validation rejects missing required native evidence; every release uses the capability matrix.

---

## Read this first

Start with **contracts F1–F6**, then the operations plan. The first feature targets are **exact zoning C3, bike/bus overlays C4, and cargo rail T2**. Cargo air and passenger air follow as separately validated capabilities. Do not build all five sub-projects into one large patch.

The primary dependency is reliable operation completion, not a new UI. The new v2 command transport makes operation IDs, world identity, leases and receipts explicit. Existing tool names remain adapters. Legacy reads stay compatible; legacy writes without world/lease fields require a documented opt-in in release2. This deliberate migration avoids claiming that an old client has protections it never supplied.

The roadmap's uncertain game APIs are bounded investigation steps, with documented pass/fail outcomes. We can finish and ship a supported feature even if an unrelated native gate fails. A failed gate is not an invitation to force game state or invent a replacement behavior.

## Execution packages

| Package | Detailed plan | First useful result | Main prerequisites |
| --- | --- | --- | --- |
| 1. Contracts | [F1–F6](2026-10-11-bridge-contracts.md) | Correct names, meaningful errors, accurate units, visible capability limits | Existing checkout only; native parity uses a disposable fixture |
| 2. Operations | [Operations and batches](2026-10-11-bridge-operations.md) | Verified saves, reconcilable timeouts, player takeover and resumable sequential batches | F1–F6 |
| 3. Construction | [C1–C9](2026-10-11-bridge-construction.md) | Exact zoning and corridor upgrades, followed by full geometry/facility tools | Shared world/operation contracts; per-feature native gates |
| 4. Transit | [T1–T4](2026-10-11-bridge-transit.md) | Cargo trains, then separately tested air routes and existing-line edits | Contracts; operations for mutations; native route preview |
| 5. Management | [M1–M5](2026-10-11-bridge-management.md) | Service capacity/staff/storage readbacks, then finance, progression and comparable diagnostics | C1 asset inventory for M1; lifecycle for point purchases; T1 for transit history |

### Release order and dependencies

1. **Contract release:** F1 encoding → F2 typed errors/test core → F3 capabilities/catalog → F4 metric truth → F5 stop/range/effective-value contracts → F6 validation report. No new construction capability is needed to ship this.
2. **Control release:** operations native probes → world/operation admission → verified completion → save identity → player lease/takeover → owned simulation/observations → batches. New features wait for the lifecycle behavior they use. Read-only inventory may land earlier once O2's WorldRef is available.
3. **Productivity releases:** C1 inventory and C2 preview primitives; then C3 exact cells and C4 bike/bus patches. Independently, T1 transit readback → T2 cargo rail → T3 air. Implement only the preview kinds a feature needs; overlays do not wait for the entire facility transformation suite.
4. **Layout releases:** C5 explicit 3D/cubic geometry → C6 junction presets/roundabouts. C1 → M1 service readings enables C7 upgrades/relocation and C8 owner-bound areas. C9 roadside inventory/unused-object lifecycle can ship before in-use stop editing; its in-use cases wait for T4. Reservation persistence waits for proven saved-asset identity; export/import is the stated fallback.
5. **Management releases:** M2 service/economy/quote analytics; M3 progression; M4 utility/resource/production data; M5 traffic/transit histories and comparisons. Read-only packages do not require every layout feature to finish.

Parallelize research and independent pure helpers, but serialize edits to shared router/catalog/registry files. Only one implementer owns each shared-file change at a time; merge reviews happen before the next dependent task. Never run native fixture mutations concurrently against the same game.

## What is actually being built

| Roadmap scope | Implementation coverage | Proof of completion |
| --- | --- | --- |
| R01 contracts/capabilities | F1–F6; T1 adds typed transit detail | Fake MCP/HTTP tests, native panel parity or explicitly unresolved fields |
| R02 lifecycle/save | Operations plan | Same-ID replay creates once; load rejects old work; named checkpoint completion verified |
| R03 batches/player control | Operations plan | Partial batch stops and resumes without replay; all writers respect takeover |
| R04 exact zoning | C3 | Exact cell diff, occupied/avenue protections, stale-selection rejection, measured batch comparison |
| R05A bike/bus overlays | C4 | Unrelated flags preserved; valid lanes, stops and junction continuity |
| R05B network/intersection geometry | C1/C5/C6 | Explicit intended level, complete footprint and directed vehicle movement |
| R07A inventory/previews/reservations | C1/C2 | Full asset/connector envelope; no committed preview effects; invalidated stale preview |
| R07B facility changes | C7, consuming M1 | Upgrade/storage/identity preservation through supported native operations |
| R06A cargo and air | T1/T2/T3 | Actual freight/passenger movement; separate train/cargo-air/passenger-air gates |
| R06B transit editing | T4 plus C9 stop lifecycle | Same line identity/settings, valid route and observed round trip |
| R08A/R08B service/economy | M1/M2 | Family-by-family panel parity, qualified operating costs, quote leaves debt/cash unchanged |
| R09 progression/settings | M3 plus operations modal handling | Normal one-time point debit, verified prerequisite state, settings remain read-only |
| R10 objects/owner areas | C8/C9 | Native ownership/attachments preserved; no unintended neighbors/routes changed |
| R11 traffic/resources/utilities | M4/M5 | Units/periods known, local disconnection visible, matched comparisons and honest data gaps |
| R12 testing/release | F1/F2/F6, operations fixture runner, every feature's test/release steps | Builds plus actual affected native fixtures; rollback/build identity and scoped commits |

Out of scope: remote hosting, a broad bridge rewrite, force unlocks, general terraforming/decoration tools, ship routing, automatic upstream publication, and guaranteed aesthetic/traffic outcomes. A local HTTP token/authentication change remains a separate scoped hardening proposal; method classification and truthful capability reporting are included here.

## Working and release checklist

- [ ] Read the shared design and chosen sub-plan; confirm the native gates required by that slice.
- [ ] Inspect branch/worktree and preserve the current uncommitted gameplay documents. Use a focused `codex/` branch; do not stage all files. The existing October8 setup plan remains historical.
- [ ] Execute the task's failure → implementation → passing verification cycle. Commit task paths only. The specified commands are future execution instructions, not tests already run while planning.
- [ ] Run focused offline tests and SkipDeploy build; record failed/notRun checks honestly. Do not broaden native tests into the player's city to obtain green results.
- [ ] Schedule the candidate's game-closed install and disposable fixture session when the player has handed over testing control. Build/read-only development can proceed while the player continues playing.
- [ ] Check placement, graph/lanes, appearance and operation separately. Native capability flags stay unverified/unsupported until their fixture passes on the recorded build.
- [ ] Record measured call/time/frame/error/result-diff baseline and candidate for zoning/batches. Set performance targets from this evidence, not from a guessed multiplier.
- [ ] Update portable API/testing/native findings and bridge notes; keep raw outputs in `.local/`. Link corrected behavior to the original B-note instead of deleting history.
- [ ] Review the candidate and retain rollback/build identity. Personal-fork publishing follows approved scope; upstream contact requires a separate user instruction.

## Execution choice

Recommended: **subagent-driven implementation with a fresh review at each task boundary**, starting with F1. The lifecycle contracts are shared by many destructive game actions, so catching an incorrect completion or replay rule early is worth the extra review. A single-implementer run with an independent final review is also possible.

This plan deliberately has no speculative hour estimate. The first contract release has known source changes; later effort depends on the native investigation gates. Review this design and choose an execution approach before starting implementation. Planning is complete when the linked files, interfaces, dependencies and requirement coverage have been checked; none of the task boxes should be marked done from documentation work alone.
