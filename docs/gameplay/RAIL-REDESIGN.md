# Industrial railway redesign after visual review

October10,2026. This supersedes the earlier cargo hook, depot fan/wye, and northern S-bend concept. The user stopped construction to request a cleaner, more spacious railway, then supplied pink/green/orange/blue markup. This is a reviewable concept, not a set of validated build commands. Local drawing: .local/rr-revised-layout.png and .svg. Underlying bridge and asset audits: .local/rr-review-proposal.md and rr-tunnel-audit.md.

## Requested arrangement

- Pink: retain both cargo-terminal tracks as distinct parallel leads into an orderly switching area. Provide longer consistent sidings and a deliberate crossover; replace the short second-track merge and irregular loops. Use matching track types and long turnout approaches. No promise that extra sidings cause automatic train marshalling.
- Green: keep the existing depot and its Extra Rails tracks heading west. Remove the unfinished downward-curving fan. Consolidate tracks only where ample length permits broad turnouts; do not force a southward detour to meet the cargo line.
- Orange: place the large RailYard01 as the central depot feature, with enough surrounding land for its actual connectors, upgrades, sidings and maintenance access. A base-only candidate is(-4680,3702),rotation0,352×224m, footprintX[-4856,-4504],Z[3590,3814]. Unlocked base cost150,000. Extend existing service frontageZ3822 west to its north face. A precinct nearer860×660m is shown to account for approaches, not merely its building rectangle. Native upgrade and connector geometry is not known before placement/preview.
- Blue: keep the visible leads parallel and use an aligned band of straight descending approaches west of the new yard. Make broad turns and tie-ins underground; include an ascent before meeting the existing surface railway. Straight ramps of600–800m for a nominal20m drop give about2.5–3.3% grade, before actual terrain corrections. Final portal positions depend on native clearance and the large-yard connector survey.

## Enabling work

Pylons near(-4631.85,3490.92),(-4794.42,3554.78), and(-4957,3619) obstruct the intended straight rail corridor or the new yard approach. Plan a deliberate power-line reroute or buried replacement before rail construction, preserving the electrical circuit. A small northward track shift merely trades one pylon conflict for another. Current S-bend also crosses the proposed large-yard footprint and must be replaced. Do not add the large asset through either obstruction.

Retain the new B/Q east-side road access and O's restored service plot. Dirt maintenance access belongs alongside the yard, clear of turnouts and portal approaches. Confirm the large asset's road face and upgrade space before fixing detailed tracks around it.

## Execution order

1. Refresh current city and separately verify a save; remain paused.
2. Make power relocation and large-yard footprint concrete in the native preview. Confirm available cash and actual cost.
3. Place the large yard where approved design fits, then inspect its real track connectors and upgrade locations.
4. Replace the local railway as one coherent layout: both cargo tracks, generous ladder, straight depot leads and necessary maintenance roads. Preserve outside connectivity or a restorable current checkpoint.
5. Build straight portal approaches, underground curves and deliberate surface tie-ins. Validate levels and native tunnel state, not just X/Z coordinates.
6. Verify every facility track and permitted travel direction, roads/utilities, appearance, and save. Operating routes and traffic testing are separate from paused geometry; do not resume without the user's review.

## Bridge constraints

The current native construction handler supports negative e1/e2 only for free endpoints, clamped at-30m. Snapping checks X/Z and preserves the existing endpoint height, so e=-20 does not lower a snapped surface node. No tunnel-specific prefab, tunnel flag, cover depth, or tunnel cost was exposed by the current tool queries. Final underground topology and appearance require native validation. Current bridge has no cargo/air route creation tool.

## State at review

No construction was performed in this review turn; only read-only survey, planning, documentation and a save. Paused frame20653739, native21:56June2030, cash3,839,288,population41,507. Verified checkpoint Ezra-Rail-Design-Review. Large yard and tunnels remain proposed.
