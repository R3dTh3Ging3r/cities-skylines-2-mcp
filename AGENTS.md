# Cities Skylines II project instructions

## Development handoff

Read [docs/HANDOFF.md](docs/HANDOFF.md) and [the retained implementation decisions](docs/handoff/IMPLEMENTATION-DECISIONS.md) before development. The authoritative local checkout is `D:\Codex-Projects\Mods-For-Games\CitySkylines_2`, branch `codex/bridge-operations`, in the personal fork `R3dTh3Ging3r/cities-skylines-2-mcp`. The implementation checkpoint is `b9871afc85d7ed4b47c35960d2c17868aa630c57`; later handoff commits change documentation only. Resolve the current branch tip and the commit containing `docs/HANDOFF.md` when resuming; do not reset to this older code checkpoint.

Feature execution remains paused after the October 13 handoff. F1–F6 passed offline implementation/review; O1 awaits independent review of `9ad3e6746cc3843958bead973fd4dd180c055a26..b9871afc85d7ed4b47c35960d2c17868aa630c57`. O2–O8 and all construction/transit/management tasks remain pending. Native validation remains `notRun`. On authorization to resume, finish O1 review first; do not restart F1 or skip to construction. Use GPT-6.1 Sol/high for task implementation/review, one source implementer at a time, with root orchestration and review after each task.

The installed game DLL and running MCP processes are separate from source HEAD. All development builds must pass `-p:SkipDeploy=true`; never replace the installed mod or restart the MCP/game as a side effect of tests. Keep `.local/`, `.codex/`, `.superpowers/`, saves, secrets, configuration and raw runtime evidence out of Git. Portable gameplay summaries and reviewed implementation decisions belong in tracked Markdown.

Cloud work branches from the latest personal-fork development tip and pushes a separate `codex/*` branch for review. GitHub is the exchange point; the D-drive checkout integrates only reviewed commits using the handoff procedure. Fetch before integrating, preserve local tracked/untracked/ignored changes, and stop on divergence instead of resetting, force-pushing, auto-stashing or overwriting. Publication is not deployment. No cloud environment/network changes, upstream messages, merges of unfinished work or deployment are authorized by this handoff.

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
