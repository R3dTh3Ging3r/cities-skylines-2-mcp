# Cities Skylines II project instructions

## Gameplay memory

Before planning or resuming gameplay, read `docs/gameplay/README.md`, `docs/gameplay/SESSION.md`, `docs/gameplay/PLAYBOOK.md`, and the recent entries in `docs/gameplay/LEARNING.md`. Treat saved city measurements as historical until refreshed through the bridge. Check the current user's objectives before acting.

The user authorized maintaining these Markdown files as a learning record. After a meaningful experiment, failure, milestone, or session end:

- Update `SESSION.md` with the current objective, measured progress, clock basis, checkpoint, unresolved actions, and next step.
- Append an evidence-backed entry to `LEARNING.md`; distinguish a hypothesis, one observation, and a repeated result.
- Revise `PLAYBOOK.md` when the evidence supports a better decision rule. Link the lesson that motivated the change. Preserve corrections in the journal rather than silently rewriting history.

Keep raw screenshots, detailed telemetry, save files, machine settings, and configuration backups in ignored `.local/` paths. Keep portable strategies and concise lessons in tracked Markdown. Follow the approved personal-fork publishing scope; do not send upstream messages without user instructions.

## Bridge operation

Discover tools available in the current session; an upstream tool may exist but be disabled in the local allowlist. Read schemas and response notes before interpreting units. In this bridge, building demand is reported on a 0–255 scale even though the MCP description says 0–100; budget values are monthly rates refreshed hourly.

Read state before changing a city. Verify a separately named checkpoint before substantial construction. Use sequential mutations and inspect results; a timeout does not prove cancellation. Saves are asynchronous. Distinguish wall-clock deadlines from in-game elapsed time and never claim completion from an unverified save or stale observation.

For development, retain upstream history and attribution, keep fixes focused, and follow the approved design/plan under `docs/superpowers/`. Parent Windows path-safety instructions still apply.
