# Gameplay knowledge and memory

These files are the durable reference for running the city and improving decisions between sessions. Read them before gameplay; update them as evidence accumulates.

| File | Purpose | Update when |
| --- | --- | --- |
| [PLAYBOOK.md](PLAYBOOK.md) | Researched mechanics, strategy, and decision rules | A patch or a supported lesson changes the approach |
| [SESSION.md](SESSION.md) | User objectives, time limits, current progress, and next action | A meaningful action, milestone, interruption, or session end |
| [SESSION-01.md](SESSION-01.md) | Archived result of the first 30-minute challenge | Historical reference; do not treat as current state |
| [SESSION-02.md](SESSION-02.md) | Archived result of the second 35-minute challenge | Historical reference; do not treat as current state |
| [LEARNING.md](LEARNING.md) | Experiments, outcomes, corrections, and confidence | A decision teaches us something useful, including a failure |

The research baseline is October 8, 2026. The local game log reports **1.6.2f1**; Steam build **25127643**. Recheck after updates. This is a written learning process, not a change to the model's training or an unattended background agent.

Use the playbook as a starting hypothesis where it gives strategy. Use current game data to decide whether it applies. Record the user's challenge before starting its clock. A later session can recover the reasoning from these files without relying on chat history.

Detailed local evidence belongs under `.local/`, with short paths such as `.local/play/01/`. Name captures `view.png`, `before.json`, and `after.json`; put descriptive labels in the session record. Keep raw evidence out of Git. Summaries can be committed to the personal fork when useful.

Gameplay bug evidence and proposed bridge additions are tracked in [BRIDGE-NOTES.md](BRIDGE-NOTES.md).
