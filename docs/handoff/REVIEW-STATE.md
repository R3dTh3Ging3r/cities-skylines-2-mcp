# Retained implementation and review evidence

Prepared October 13, 2026 from the original local reports. This is a portable evidence summary, not a replacement for running tests or observing the game. Native evidence is notRun. Original reports remain under ignored `.superpowers/sdd/2026-10-11-bridge-{contracts,operations}/`; raw outputs remain in `.local/`.

## Foundation acceptance boundary

Plan baseline: `8f1d4325a2b65d58fff9ddb984c22eb78f897ca6`. Final approved foundation: `9ad3e6746cc3843958bead973fd4dd180c055a26`.

| Task | Implementation/fix commits | Accepted offline scope |
| --- | --- | --- |
| F1 | `99959d7`, `2ce85e4` | Encoding/client seam, actual fake stdio contract tests and Windows Node18 test invocation |
| F2 | `8f827da`, `31149fa` | Typed errors and conservative outcomes; BCL core tests |
| F3 | `f232f63` | Authoritative endpoint catalog and qualified capabilities |
| F4 | `7cf0a76` | Raw demand 0–255, monthly/hourly budget qualifications, unknown calendar/cargo units |
| F5 | `341fa30`, `467572b` | Role-aware stop eligibility, setter validation/readback-pending semantics, native float step precision |
| F6 | `85596e7`, `7ee8676` | Actual PowerShell evidence runner, provenance/binding/catalog/semantic validation |

The full package review at `7ee8676` found I1 and M1–M4. The sole fix commit `9ad3e67` was independently rereviewed and all findings addressed:

- Generic C# errors and untyped/legacy Node fallbacks preserve unknown outcome and are nonretryable regardless of HTTP status; only explicit known pre-apply failures claim notApplied.
- Capability reasons support null; actual wire/type fixtures cover it.
- Loan acknowledgments describe queued work, not completed repayment/borrowing; validation distinguishes invalid input.
- Evidence equality compares unordered case-sensitive object keys, ordered arrays and strict JSON scalar types, accepting equivalent numeric representations.
- Loan catalog evidence reads `loan-action-queued-ack; pending`.

Final fix evidence included 22 focused Node tests, contracts19/endpoints6 and actual serializer8/8. Core/native builds had zero warnings/errors; serializer restore had two NU1900 vulnerability-audit network warnings, no errors or dependency changes. Tests are historical, not a fresh handoff claim. The installed DLL was unchanged by SkipDeploy.

Accepted limitations: lifecycle/admission belong to O tasks; native panels/timing/save ownership/loaded identity were not run; F6 capabilityExposure means the HTTP catalog, not current MCP listTools/schema visibility; manual evidence authenticity requires human review. A validator exit0 does not fulfill every release criterion.

## O1 boundary — review still pending

**October 16 reconciliation:** The original independent Sol 6.1/high reviewer
confirmed its review of `9ad3e67..b9871af`: no correctness or security blockers
within O1's preparation-only scope; root accepted its specification and quality
verdict. [PR #2](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/2)
also records this receipt. The reviewer performed source inspection, not test
reruns, native execution or independent reproduction of metadata evidence.
Dispatch remains unavailable; two admission TODOs await real O2/O5 producers.
O1 preparation is accepted; G1 investigation/release evidence remains `notRun`.
The original pending-review record below is retained as history.

Review full range `9ad3e6746cc3843958bead973fd4dd180c055a26..b9871afc85d7ed4b47c35960d2c17868aa630c57` against [operations O1](../superpowers/plans/2026-10-11-bridge-operations.md) and [decisions](IMPLEMENTATION-DECISIONS.md). The task's own report is not independent approval.

O1 extends the existing F6 runner/validator with a fixed lifecycle descriptor, Collect-only opt-in and evidence-root controls, fresh-directory enforcement, retained refusal evidence, and immutable startup validation metadata. It reads only fixed `/capabilities` and `/state` in its collector. All five probes remain unverified/validationOnly with dispatchAvailable false. No native probe, load or restoration implementation exists in O1.

Preserve: full catalog/provenance/build/world binding and semantic equality; path length <=240; rejection of traversal/reparse ancestors; exclusive run-start receipt; no overwrite of previous evidence; exact default-off startup environment parsing; context claims are not authority. A fresh directory may contain only preseeded fixture-context input, with malformed input retained in refusal evidence. No fake world/lease/checkpoint may enable execution.

Historical RED evidence: runner seven expected failures and two pending cases before implementation; endpoint metadata6/8. GREEN: full Node18 suite39 passed +2 TODO loaded before the last junction case; final full runner10 passed +2 TODO includes junction and forged-observation checks; core19/8/13/16; native SkipDeploy0 warnings/errors. Two TODO names are `validation_mode_does_not_bypass_world_or_lease` and `release_defaults_reject_probe_commands`. Replace them only with real O2/O5 admission coverage.

The reset-GET experiment showed HttpWebRequest can reconnect transparently. The final truncated-JSON fixture proves application-level stop/no-repeat, not a blanket framework no-retry guarantee. Future mutation transport requires O2 stable operation identity. Metadata/selected IL identify candidates only; runtime callback order, native save success/correlation/durable identity, texture ownership and cancellation remain unproved.

O1 build readback recorded dirty source based on `9ad3e67`, candidate SHA256 `09EDEA118DD8BFCAE0F45C4869A12FE7270A8B2F70DC80B29C2D1FA611BF4FB2`, installed SHA256 `E41C68C7547C207A55A4222081CFEB8E247109D1A3734975D6821F056688D47B`, loaded module unknown. Do not infer a loaded assembly from either disk file. New builds may have different hashes; current handoff verification is in [HANDOFF](../HANDOFF.md).

O2–O8 and all C/T/M tasks remain unimplemented. Each task must consume actual preceding APIs rather than treat prepared inventory proposals as completed code. Important/Critical review fixes precede the next task; minor findings are retained for the package review. Preserve upstream attribution/history and do not publish upstream messages without user direction.
