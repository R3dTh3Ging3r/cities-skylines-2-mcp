# Native smoke-test report: optional road and rail curves

**Result:** The targeted curve-readback smoke test passed in a live, paused game. This is useful native evidence for the candidate feature; it is not a general release approval.

**Tested candidate:** source commit `d21a56315b9f4cb2e21670a25ca6f17a1e708868`, on personal-fork branch `codex/road-curve-readback`, draft [PR #2](https://github.com/R3dTh3Ging3r/cities-skylines-2-mcp/pull/2). The running bridge identified that revision. The game mod reported version `0.9.0`, assembly version `0.4.0.0`.

**Environment:** Cities: Skylines II was open in the user-designated `Round Rock` sandbox, with unlimited money and unlocks as reported by the user. The simulation was paused at bridge frame `6545271`; city population was 0. The test ran on October 13, 2026 local time (ending about `2026-10-14 00:57 UTC`).

## Procedure and observations

1. Repeated the same bounded `cs2_road_graph` query three times: center X `-755.738`, Z `-1339.409`, radius `900 m`, limit `300`, all networks.
2. With `includeCurve` omitted or false, each query returned 15 segments and all had `curve:null`.
3. With `includeCurve:true`, each query returned curves for all 15 segments. All control coordinates were finite; `curve.a` and `curve.d` matched segment start and end within `0.01 m`. Eight segments had at least one handle more than `0.1 m` from the straight chord, so the response contained nontrivial curve geometry.
4. Ran one wider all-network query with radius `2000 m` and limit `2000`. It returned 50 segments: 22 one-way highway, 2 medium road, 15 double train track, and 11 high-voltage line. All 50 returned curves with finite coordinates and matching endpoints; all 15 rail segments passed these checks.
5. Called the existing city overview, budget, city services and demand read tools. They returned successfully. The sandbox reported treasury `2,000,000,000`, population `0`, zero monthly budget and service metrics, residential household demand `35`, and building demand `100` for low/medium/high residential, commercial, industrial and office; storage demand was `0`.

The last values show that the existing read tools were reachable, not that nonzero values or parity with the game's native panels were validated. The empty sandbox provided no traffic, active service load or construction activity.

## Scope and follow-up

The result supports this narrow conclusion: for the sampled paused-city road and rail segments, the candidate returned the requested optional curve data, kept the omitted/false response shape, and serialized finite controls with endpoints matching the segment endpoints. The omitted/false/true sequence was consistent in three repeated runs.

The test did not exercise snapping, lane ownership, graph connectivity, construction or other mutations, transit behavior, nonzero economy/service measurements, another map, maximum-city-size performance, or native-panel parity. No construction, demolition, simulation advance or save occurred. No source implementation changed as part of the game test.

PR #2 remains draft and unmerged. The feature-specific native smoke test passed, while the project's broad native/release gates G1–G4 and remaining roadmap tasks remain open. Continue to follow [the development handoff](../HANDOFF.md); this report does not waive its O1 review gate or authorize deployment.
