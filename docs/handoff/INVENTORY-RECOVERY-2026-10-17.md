# Bounded inventory recovery acceptance — October 17, 2026

Accepted source scope: single-root raw relationship pages and finite recovery diagnostics through the existing direct inventory and O7 tools. Independent specification, quality/security and whole-increment review passed after one fix round. This is source acceptance, not a native release or completion of the broader construction roadmap.

## Source and behavior

Base: native-tested PR9 head `0b913a3dcd41b0795e8c130aa81ee4c344a6c099`.
Implementation: `14e3d7c96c36c72f3dbc1aa6d1021cc7a11c30d8`.
Reviewed correction/final source: `4d29af2747eade7a85556f9581ad239522606a49`.
Branch: `codex/construction-inventory-recovery`. Later receipt commits change documentation only.

`relationshipPage: {kind, offset, limit}` requires one exact versioned root and all three page fields. The six kinds are owner, attached, subObject, installedUpgrade, subNet and subArea; offset is a nonnegative Int32 and limit is 1–32. It cannot be combined with directDepth1. Existing root geometry and optional sampled profiles remain available.

The page reads only its selected raw slots, preserving nulls, invalid references, duplicates and source indices. It returns all six source headers, observed counts, window coverage and a next offset. Positive references are observed pointers, not verified target availability. Inspect chosen references separately through the existing inventory; no automatic target read, retry, recursion or skipped-slot scan occurs.

World/version checks and selected-source presence/count guards fence the page. Independent pages remain live and non-atomic; unchanged counts cannot detect same-length replacement/reordering. Reaching a source's final offset does not establish that earlier pages formed a stable snapshot. Existing complete mode keeps its 32-root/128-slot/128-additional-target limits. Individual wire/MCP and O7 group limits remain 256 KiB and 4 MiB.

Known slot, serialized-output, transport and MCP-wrapper failures receive finite actionable diagnostics. Unknown failure causes stay unknown. Inventory-containing O7 batches sanitize outer failure details and inventory error rows, including mixed groups, without changing other rows or successful group budgets.

## Reproductions, review and verification

The [native report](POST-PLAYTEST-2026-10-17.md) motivated the change but did not publish enough raw evidence to identify the exact cause of either historical overflow. Synthetic production-source reproductions separately establish the 129-slot refusal before geometry/slot reads, actual serializer overflow and escaped MCP overflow. These are not reconstructed city measurements.

Independent review found one Important issue at the initial source: an outer O7 HTTP error echoed arbitrary bridge text and expanded a 200,106-byte fixture body into a 400,394-byte MCP error. The correction returns 643 bytes without the sentinel text; each route makes one HTTP request with no retry. A valid 64-item success above 256 KiB still passes. Scoped re-review marked the issue addressed and accepted the whole increment.

Final covering evidence:

- 36 construction assertions, 13 O7 assertions and 40 response/native-boundary assertions passed.
- 69 focused Node tests and TypeScript compilation passed after the correction, including actual SDK discovery/admission, direct GET/O7 POST, production C# wire parsing, explicit chosen-target geometry and bounded failure handling.
- Both canonical net472 test projects compiled with cached dependencies and SkipDeploy=true, zero warnings/errors. Execution used exact linked production/test source in temporary net8 runners; Framework compilation and portable execution are different evidence.
- The reviewer independently ran 36 construction, 40 response and 36 Node checks on the initial source, then build plus 30 focused Node checks and the original overflow probe on the correction. These overlap the covering suites and are not additional aggregate test counts.
- Git whitespace checks passed. No new dependency installation, game connection/session, machine configuration change, save, merge or deployment occurred.

Raw logs, reports and review packages remain in ignored `.superpowers/sdd/2026-10-17-construction-inventory-recovery/`. The checked-in wire fixture contains synthetic native-boundary data, not private city telemetry. Existing workers and local evidence were retained per the user's continuity requirement; this costs local disk space, without waiving review. Native evidence is attributed to the published PC report rather than uninspected raw artifacts, leaving the exact historical overflow causes unknown.

## Remaining construction work

New page behavior still needs future consolidated native validation. Complete facility/platform/connector/upgrade semantics, stable spatial discovery, physical-unit/calendar parity and guarded writer lifecycle remain open. The prior playtest's selected native successes remain valid reported evidence; they do not certify this newer source.

The [next construction lifecycle plan](../superpowers/plans/2026-10-17-construction-lifecycle-next.md) specifies consumed passive observation at real Apply, reply, Finish, cleanup and displacement sites before changing completion semantics. It maps separate generic/road/transit/direct-write/save boundaries and the path to O3/O5 and later batching. Actual callback/cleanup ordering, late effects, takeover and save artifact/preview ownership remain indispensable native evidence. No new playtest is requested by this receipt, and no guarded writer is enabled.
