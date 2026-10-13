# Gameplay knowledge and memory

These files are the durable reference for running the city and improving decisions between sessions. Read them before gameplay; update them as evidence accumulates.

| File | Purpose | Update when |
| --- | --- | --- |
| [BRIDGE-ROADMAP.md](../BRIDGE-ROADMAP.md) | Consolidated bridge priorities, dependencies, existing capabilities and acceptance criteria | Reconcile after a focused feature or bug-fix release |
| [BRIDGE-NOTES.md](BRIDGE-NOTES.md) | Evidence-backed bugs, limitations and improvement requests B001–B029 | A gameplay observation or source audit adds/corrects a finding |
| [PLAYBOOK.md](PLAYBOOK.md) | Researched mechanics, strategy, and decision rules | A patch or a supported lesson changes the approach |
| [INDUSTRIAL-PLAN.md](INDUSTRIAL-PLAN.md) | Built northern industrial road phase and future freight, factory and utility reservations | Refresh before facility placement; short freeway connection handed to user |
| [RAIL-REDESIGN.md](RAIL-REDESIGN.md) | Revised industrial yard, parallel freight leads and underground departure concept | Supersedes the initial local rail layout after the user's visual review |
| [REDEVELOPMENT-PLAN.md](REDEVELOPMENT-PLAN.md) | Surveyed Beech corridor concept, two interchange options, districts, costs and paused cutover | Current planning reference; refresh before construction |
| [ROAD-DESIGN-STUDY.md](ROAD-DESIGN-STUDY.md) | October 9 road/interchange research, geometry checks and testing method | Before designing the Beech corridor; after relevant experiments |
| [EXPANSION-BRIEF.md](EXPANSION-BRIEF.md) | Current western housing/eastern waterfront requirements | User changes the district objectives or a survey resolves constraints |
| [FREEWAY-PLAN.md](FREEWAY-PLAN.md) | Northern freeway, feeders and expandable utility-campus brief | Before the next infrastructure challenge; revise with measured results |
| [SOUTHERN-CONNECTION-PLAN.md](SOUTHERN-CONNECTION-PLAN.md) | Proposed southern highway branch, recycling and cash accumulation | Before challenge five and after its live survey |
| [SESSION-21.md](SESSION-21.md) | Archived parks and road-maintenance placement; preserved in the next session’s verified checkpoint | Historical; operation was interrupted |
| [SESSION-22.md](SESSION-22.md) | Archived six special factories, local-road closure and stepped industrial/office zoning | Historical; later user play supersedes its city measurements |
| [SESSION.md](SESSION.md) | User objectives, time limits, current progress, and next action | A meaningful action, milestone, interruption, or session end |
| [SESSION-01.md](SESSION-01.md) | Archived result of the first 30-minute challenge | Historical reference; do not treat as current state |
| [SESSION-02.md](SESSION-02.md) | Archived result of the second 35-minute challenge | Historical reference; do not treat as current state |
| [SESSION-03.md](SESSION-03.md) | Archived third one-hour challenge | Historical reference |
| [SESSION-04.md](SESSION-04.md) | Archived fourth freeway/treatment challenge | Historical reference |
| [SESSION-05.md](SESSION-05.md) | Archived southern connection and cash-accumulation challenge | Historical reference |
| [SESSION-06.md](SESSION-06.md) | Archived interchange revision and northern industrial migration | Historical reference; later user utility work supersedes pending tasks |
| [SESSION-07.md](SESSION-07.md) | Archived redevelopment planning and initial readiness | Historical; user clearance and later construction supersede the old preflight |
| [SESSION-08.md](SESSION-08.md) | Archived western residential streets and service infill | Historical; revised user roads and latest school/east phase supersede pending actions |
| [SESSION-09.md](SESSION-09.md) | Archived western large schools, parks and eastern street construction | Historical; current eastern service placements are in SESSION.md |
| [SESSION-10.md](SESSION-10.md) | Archived city-version eastern fire, police and school placement | Historical; latest civic roads and reservations are in SESSION.md |
| [SESSION-13.md](SESSION-13.md) | Archived interrupted university construction and prior mailbox session | Historical; user campus changes supersede that layout |
| [SESSION-14.md](SESSION-14.md) | Archived completed western/southern residential roads and service reservations | Historical; later user gameplay supersedes its treasury and state |
| [SESSION-15.md](SESSION-15.md) | Archived industrial district survey and approved concept | Historical; current as-built corrections are in SESSION.md and INDUSTRIAL-PLAN.md |
| [LEARNING.md](LEARNING.md) | Experiments, outcomes, corrections, and confidence | A decision teaches us something useful, including a failure |

The research baseline is October 8, 2026. The local game log reports **1.6.2f1**; Steam build **25127643**. Recheck after updates. This is a written learning process, not a change to the model's training or an unattended background agent.

Use the playbook as a starting hypothesis where it gives strategy. Use current game data to decide whether it applies. Record the user's challenge before starting its clock. A later session can recover the reasoning from these files without relying on chat history.

Detailed local evidence belongs under `.local/`, with short paths such as `.local/play/01/`. Name captures `view.png`, `before.json`, and `after.json`; put descriptive labels in the session record. Keep raw evidence out of Git. Summaries can be committed to the personal fork when useful.

Gameplay bug evidence and proposed bridge additions are tracked in [BRIDGE-NOTES.md](BRIDGE-NOTES.md).
