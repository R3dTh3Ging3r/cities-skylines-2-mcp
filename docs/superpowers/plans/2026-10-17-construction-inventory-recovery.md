# Bounded construction inventory recovery

Base: `0b913a3dcd41b0795e8c130aa81ee4c344a6c099`, native-tested PR9 head.
Branch: `codex/construction-inventory-recovery`.
Authority: Eric's post-playtest request to reproduce inventory refusals and deliver a reviewed bounded recovery workflow, with no new installation/game session.

## Problem and scope

The [native playtest](../../handoff/POST-PLAYTEST-2026-10-17.md) reported RESULT_TOO_LARGE for five combined roots and a single park's directDepth1 attachments. Existing source independently refuses more than128 total raw relationship slots before geometry/slot reads, and refuses serialized observations over256KiB. MCP hides both behind a generic message. The raw native evidence is not published, so reproductions must be labeled synthetic source-path reproductions, not reconstructions of the city's exact cause.

Deliver a useful bounded alternative through the existing direct inventory and O7 read: one exact root, one fixed relationship kind, a bounded window of raw references. Users can pass returned valid references to the existing geometry inventory separately. This handles a large single root without increasing whole-result or work limits, recursively traversing targets, or silently truncating a requested complete expansion.

## Binding constraints

- Preserve existing root-only and directDepth1 semantics:32roots,128global raw slots (null/duplicates included),128additional geometry targets,256KiB individual wire/MCP cap and4MiB O7 group cap. No automatic retries, cap increases, new writer, global scan, or native API invention.
- Add optional `relationshipPage: {kind, offset, limit}` to the existing request. All three fields required; kind is exactly one of owner, attached, subObject, installedUpgrade, subNet, subArea. Offset is an integer0..2147483647; limit an integer1..32. Exactly one explicit root; mutually exclusive with `relationships:directDepth1`. Existing optional root geometry diagnostics may coexist. Reject invalid/mixed requests before native work or HTTP.
- Compact GET uses fixed fields for the page kind, offset and limit; require all or none. O7 and actual SDK schema must expose and enforce the same shape. Preserve query/body bounds and strict unknown-field rejection.
- Read all six fixed root headers but only the selected page's raw slots; no skipped-slot scans, hashing of unvisited content, recursion or target geometry reads. Root geometry remains available. Validate the request world around native reads and publication, preserve full entity versions and native null/invalid reference evidence. Never retarget a recycled index.
- Return request identity, selected kind/window, observed count/presence, returned count and next offset, plus header diagnostics. Only report whole-source coverage when the current page actually covers the entire observed selected source. Reaching the final offset is not proof earlier pages covered the same state. Nonselected sources are not marked visited/complete.
- Offset equal to an available observed count returns an empty terminal window; larger offsets reject. Missing/unavailable sources at offset0 return explicit unavailable/absent evidence, not fabricated zero native counts; nonzero offsets reject. Preserve null and malformed-native-reference slots in their original positions.
- Pages are independent live observations, explicitly non-atomic across calls. Counts and unchanged world/entity identity do not establish a stable snapshot: same-length replacements/reordering can occur. No opaque stable cursor, snapshot token or automatic aggregate completeness. Each page retains original world/frame/time evidence and explicit consistency warnings.
- Valid returned references mean only that a reference was read; target existence, geometry, role and usability remain unmeasured until the caller requests them. Do not label raw references as verified available targets. Keep facility/platform/connector/upgrade/engineering claims qualified.
- Preserve RESULT_TOO_LARGE and provide finite actionable diagnostics distinguishing source-slot, serialized observation, bounded transport and MCP wrapper failures where the source can establish them. Do not echo arbitrary bridge bodies or infer the stage of an unknown server refusal. Unknown refuses retain unknown cause and useful manual recovery guidance. Error responses themselves stay bounded and never trigger automatic work.

## Implementation and evidence

One source implementer owns the complete core/native-adapter reuse/GET/O7/TypeScript integration, tests and scoped user documentation. Root owns this plan, native reconciliation, lifecycle-next plan and final acceptance receipt. No subagents from the implementer. Preserve local evidence.

First retain source-path reproductions:129raw slots (single root and combined roots) refuse before geometry/slot reads; serializer overflow and escaped MCP wrapper overflow refuse under existing caps; demonstrate the old generic-error loss. Then add tests for bounded windows from a larger source, no preceding-slot/target reads, null/duplicate/invalid refs, per-kind selection, empty/final/out-of-range windows, wrong world/recycled root, same-count content changes across calls, world invalidation during reads, source coverage versus page end and strict request validation. Test actual SDK discovery/calls, typed direct GET and O7 ingress/results, malicious/mismatched page evidence and error bounds. Reuse the linked production native reader with ECS doubles for bounded slot-access evidence; do not equate it with game binding validation.

Run covering core/response and Node suites, canonical net472 builds using existing caches with SkipDeploy=true, and exact-source portable runners where necessary. Record Framework compile versus net8 execution separately; no reinstall, PowerShell/native suite retries, game connection or deployment. Export a fresh production-serialized page fixture to the Node parser test where feasible. Retain reproducible commands/results in the ignored task report.

Independent reviewer evaluates specification, code quality/security and the whole increment, including root documentation and the lifecycle-next plan. Address Important findings with the same implementer and scoped re-review. Publish only reviewed source/test/documentation in a draft PR stacked on PR9; prior branch tips and recovery snapshot remain preserved.

## Completion boundary

Acceptance is an offline-verified recovery workflow and honest failure diagnostics, with the real native failure as its motivation. New page behavior still requires future consolidated native validation. It does not close full C1, G1, facility-envelope, unit/calendar or guarded-writer gates. The next substantive construction lifecycle sequence is specified in [the companion plan](2026-10-17-construction-lifecycle-next.md), with native-dependent decisions explicitly retained.
