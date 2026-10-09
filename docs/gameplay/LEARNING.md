# Learning journal

Use this journal to improve decisions from evidence. Append entries; mark superseded conclusions and point to their replacement. Do not turn a single successful outcome into a universal rule.

## How to learn from a session

1. Record a baseline with city, game/bridge version, settings, in-game time, wall time, and relevant metrics with units.
2. State the hypothesis and the expected result before the change. Choose a practical observation window.
3. Prefer one meaningful intervention at a time. If several changes are necessary, explicitly record the confounders.
4. Record what actually changed, including unexpected costs or failures. Separate construction success from the intended economic benefit.
5. Decide whether to keep, revise, or reject the hypothesis. Record confidence and circumstances where it may not apply.
6. Update the session state immediately after a meaningful outcome. Promote a useful decision rule to the playbook with this entry's ID, preferably after a repeat observation or strong independent support.

Do not run expensive experiments merely to improve the notes when the user's challenge has a deadline. Natural gameplay outcomes can provide evidence. Keep raw captures in ignored `.local/play/` paths and preserve a concise result here.

## Entry format

Use a short ID such as `L004`, followed by:

- **Context:** city, version, difficulty/mods, objective, and both clocks.
- **Baseline:** relevant values, units, collection time, and checkpoint.
- **Hypothesis:** the cause we suspect and expected result.
- **Action:** exact change and cost; entity IDs or coordinates if useful.
- **Observation window:** elapsed game time and wall time.
- **Outcome:** after-values and whether the expected result occurred.
- **Confounders:** migration, milestone rewards, weather, simultaneous changes, stale counters, or missing observations.
- **Conclusion and confidence:** source-confirmed, observed once, repeated, tentative, or contradicted.
- **Follow-up:** next check and any playbook change.

## L001 Bridge demand units differ from the tool description

**Date:** October 8, 2026. **Context:** bridge source at upstream base `f0894c1`; game 1.6.2f1.

**Evidence:** `mcp-server/src/index.ts` describes demand as 0–100. `CS2MCP.Bridge/RequestHandlers.cs`, in `GetDemand`, explicitly labels building demand 0–255 and warns that company counters can exceed 255 and values refresh only while simulation runs.

**Conclusion:** Source-confirmed mismatch. Do not call a raw value a percentage or diagnose frozen demand from paused readings. The playbook now specifies raw units and freshness checks. Live readings have not yet been compared with the UI.

## L002 Budget changes need an appropriate observation window

**Date:** October 8, 2026.

**Evidence:** `GetBudget` describes hourly-updated monthly rates with positive expense values. A quick successful request is not a new economic sample.

**Conclusion:** Source-confirmed reporting behavior. Record in-game timestamps and distinguish treasury change from recurring balance. The provisional 1–3 game-hour observation window is a strategy hypothesis, not a tested optimal interval.

## L003 Connection success is narrower than gameplay success

**Date:** October 8, 2026.

**Observed:** C# compilation completed with zero warnings/errors, installed hashes matched, and native Codex ping returned bridge 0.9.0 at MainMenu. State correctly showed no city loaded; city overview rejected the request.

**Conclusion:** Main-menu communication is verified. This does not yet establish save completion, placement, simulation controls, or city-management effectiveness. Keep those validation stages separate. No live city strategy experiments have been performed.
