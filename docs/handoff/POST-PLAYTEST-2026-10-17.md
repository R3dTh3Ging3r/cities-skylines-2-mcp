# Post-playtest evidence reconciliation — October 17, 2026

This reconciliation was completed before further implementation. Eric then authorized a bounded inventory recovery increment and the next construction lifecycle plan. Work remains in `/workspace/cs2-operations`; no new installation, game session, routine small playtest request, merge, or deployment is part of this work.

## Evidence and provenance

Freshly fetched PR9 comment: https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/9#issuecomment-6234541118
Author R3dTh3Ging3r; updated 2026-10-17T15:39:17Z. The final post-refresh and timed-expansion sections supersede earlier connection blockers and construction-not-started statements. The comment is the published evidence; detailed reports, raw telemetry, and screenshots remain local to the playtest PC and were not independently inspected here.

GitHub commit readback independently confirms `0b913a3dcd41b0795e8c130aa81ee4c344a6c099` directly follows `93b2b67661b241c8fa64e38b0339faf4c361082b`. Its only changes qualify Game.Routes.TransportStop and adapt the Windows signal lifecycle test while retaining the production entry point and assertions. No local source integration performed.

Execution access recovered on this check. Development checkout remains clean on `codex/network-geometry-diagnostics` at `93b2b67661b241c8fa64e38b0339faf4c361082b`.

## What the native report establishes

- Full bridge compilation against installed CS2 1.6.2f1 passed with SkipDeploy=true, zero warnings/errors. Reported Node18:142 pass/0 fail/2 TODO; core:165 assertions; response:36; Node24 lifecycle:6. These are reported PC results, not new cloud runs.
- Loaded bridge identifies source 0b913a3. The host allowlist problem was corrected locally and actual exposure reached44tools. Runtime gameVersion remains unobserved even though compile-target version is known.
- C1/C5: exact road/station/root geometry, bounded direct relationships, and17-sample road/rail profiles worked on selected native entities. Facility envelopes, connector roles, complete upgrades, physical-unit calibration, between-sample extrema, and engineering limits remain unverified.
- O7/M4: bounded reads and all26catalog metrics returned supported values. Panel/calendar parity, labor freshness, and multi-source atomicity remain unverified. Workplace slots do not establish worker supply or skill shortages.
- T1: selected passenger metro, bus, cargo-air, and cargo-rail structure reads worked. Null distance units and unknown native validity/health remain. Existing cargo reads do not establish creation, dispatch, or route-edit capability.
- M5: live collection, rolling retention, explicit stop, mismatched-phase refusal, and same-range control comparison worked. An independent before/after comparable pair and causal improvement were not demonstrated.
- O2/world guards: synthetic stale epoch/version requests were rejected or marked missing. Actual reload, deletion/recycle, and UI-selection transitions were not induced.
- Legacy construction:9successful mutations with readbacks over10min18.595sec;6connected road edges,183new rowhouse cells,22new residential buildings and1park. Both avenue junctions retained4segments/traffic lights. No final warnings in the new block; off-site warnings remained. Trees/lighting receipts succeeded but full composition readback remained unknown. Road receipt length851.092864 is qualified as native-coordinate units, not verified metres.

## Errors and remaining roadmap boundaries

Two relationship inventory requests returned RESULT_TOO_LARGE: combined5-root relationships+geometry, and the new park's directDepth1 attachments. Splitting the first and requesting park root-only succeeded. This is diagnostic access failure, not failed construction. Published evidence does not distinguish raw-slot limit from serialized-byte limit; do not assert a cause yet.

O3/O4/O5/O6 and O7 mutation workflows remain gated: legacy receipts contain no operation UUID, and v2 admission/status/cancel, Apply/Finish/deferred cleanup, late effects, takeover and cancellation reconciliation were not exercised. Reading an existing checkpoint does not prove new bridge save completion or durable attribution. No agent simulation-control request was used. Native G1 and broad G2/G3/G4 exit criteria remain open, while the selected successful native smokes above must no longer be described as all notRun.

## Supported next development

Source feasibility subsequently supported a bounded page-based exact-root relationship reader with honest live-page consistency, actionable limit reasons and preserved existing caps/world/entity guards. The [frozen inventory recovery plan](../superpowers/plans/2026-10-17-construction-inventory-recovery.md) records the selected scope and offline reproductions. It does not promise complete facility semantics, silently truncate, raise caps, add global scans, or claim stable snapshots from unchanged slot counts.

The existing sole implementer and independent reviewer own implementation and review; no duplicate workers. The original checkout, recovery snapshot and existing local evidence remain preserved. The [next lifecycle plan](../superpowers/plans/2026-10-17-construction-lifecycle-next.md) distinguishes useful consumed instrumentation from native-dependent completion and authority. No further native playtest is requested now.
