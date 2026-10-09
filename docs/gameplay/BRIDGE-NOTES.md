# Live bridge findings — October 8, 2026

## B001 — Spaces in zoning and service names

Observed `cs2_zone_area` with `NA Residential Low` rejected as unknown `NA+Residential+Low`. The MCP server's URLSearchParams form encoding uses plus for spaces; the bridge path does not decode plus as a space. Calling the same documented route with percent-20 succeeded and painted 1,210 cells. Other older name-based routes may share this issue. Newer road routes already have a bridgeQueryString helper. Candidate patch: use one query serializer consistently, with regression tests for spaces, ampersands and literal plus. Temporary gameplay workaround: correctly percent-encoded localhost requests through ignored helper script.

## B002 — Construction acknowledgment precedes readiness

Successful WindTurbine01 placement followed immediately by WaterTower01 was rejected as another build operation in progress. Building confirmed once; delayed tower retry succeeded. Calls are sequential but game pipeline continues across frames. Candidate addition: operation ID, explicit busy/retryable state, and completion polling. Temporary pacing: 650–900 ms between mutations; inspect after uncertainty.

## B003 — Budget sign description is incomplete

Live budget: totalIncome 1918, totalExpenses -33324, balance -31406. Expense breakdown ServiceUpkeep is positive 33324. Tool note says expenses positive but totalExpenses is signed. Treat balance as authoritative; don't subtract a negative aggregate. Candidate patch: clarify aggregate vs breakdown signs or normalize contract with tests.

## B004 — Calendar output disagrees with UI

At the start, UI showed June 2026, API gameDateTime said 2026-01-06. Likely month/day construction or formatting issue; cause unconfirmed. Candidate patch: use game calendar conversion consistently. Until fixed use frameIndex and verified elapsed simulation hours, plus screenshot date.

## Useful additions

- Dedicated progression endpoint: current milestone, next threshold, XP, unlock points and available development choices.
- Completed-save status including safe checkpoint identity (current tool only acknowledges asynchronous start).
- Utility network connectivity and fulfilled water/sewage amounts, not only aggregate capacities.
- Batched observations with one frame/timestamp and typed units.
- Build completion receipts and idempotency keys, particularly for timeout recovery.
- Rectangular/block zoning and preserve-existing-zones mode for more precise urban design.
- Tool to dismiss construction panels and restore normal view for screenshots.

No upstream messages sent; preserve concrete evidence for a later contribution.

## B005 — Milestone dialogs hold simulation paused

Tiny Village popup visibly appeared at XP760. A new timed-run command reported running, but state re-paused after8frames at13:33. Screenshot confirmed blocking congratulations modal. User dismissed it. Candidate additions: blocking-dialog state, safe dismiss-current-milestone action, progression endpoint, timed-run interrupted reason. Do not repeatedly send resume while a blocking popup remains.

## B006 — Service building counts are zero despite real buildings

Service-budget response reports buildings:0 for every category even after wind turbine, water tower, outlet, cemetery, clinic, school and landfill are verified. Upkeep is nonzero. Candidate fix: correct service-entity association query; test against actual placed structures.

## Diagnostic additions confirmed useful during play

Prefab info needs capacity, base operating expenses and likely wages. Building inspection needs school enrollment, clinical treatment capacity, cemetery usage, landfill storage/processing and vehicles. Current inspect returned only building flag and28employees for landfill. UI inspection showed73%efficiency,30/30employees,2.67/151t garbage,10.83/100t monthly processing,39825/month cost,6vehicles in maintenance and0/14in use. Stored garbage demonstrates collection occurred; avoid treating decorative garbage geometry as proof.

## Further testing

- The sewage connection has an endpoint warning, although no household sewage warning appeared during the challenge. Inspect network attachment and default pipe elevation before treating it as fully resolved. The unused high-voltage outside connection also remains flagged.
- Native notifications include positive events (Happy Face, Leveling Building, Selected), so they are not exclusively warnings. Add severity/category filters and counts that exclude informational events.
- Add a read-only milestone/settings endpoint and a safe selection-clear command. Placement tools repeatedly reopen their construction panels, and closing one can reveal a previously selected building's panel.
- Distinguish service budget efficiency from actual building efficiency, which also depends on staffing and other conditions.

- Add a real-time stop deadline independent of agent polling. The requested 30-minute stop was detected a few seconds late, and pause/evidence tools finished within the next minute. A bridge-side UTC deadline should pause the game even while the agent is waiting on a tool or writing notes.
