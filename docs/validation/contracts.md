# Contracts fixture evidence, schema 1

The registered `contracts` fixture version 1 is defined in
`tests/native/contracts.json`. It contains only GET routes classified **read** by
the authoritative `EndpointCatalog.cs`. GET alone does not authorize a route:
simulation, camera, save, construction, purchase and setter routes are refused.
The PowerShell runner validates the whole definition before HTTP, extracts every
literal catalog descriptor without accepting partial parsing, then verifies the
observed `/capabilities` classifications and protocol/schema 1 before other reads.
Redirects are disabled; only an HTTP loopback IP origin without credentials, path,
query or fragment is accepted. No `-AllowMutation` exists in F6.

Native collection needs a separately authorized disposable-city testing session.
Do not inspect an active gameplay city, unpause, move its camera, create a save,
set sliders, install a candidate or change configuration to complete this fixture.
The runner itself reads existing state only. Its default URL is the usual local
bridge; offline tests always supply an explicit ephemeral URL and `-Offline`.

```powershell
# Only in the authorized native test session:
powershell -NoProfile -File scripts/Test-NativeFixture.ps1 -Fixture contracts -EvidenceDir .local/v01/contracts
# Offline validation: no network request, regardless of environment variables.
powershell -NoProfile -File scripts/Test-NativeFixture.ps1 -ValidateEvidence .local/v01/contracts/report.json
```

For hosts with ordinary local script restrictions, the process-only
`-ExecutionPolicy Bypass` option can precede `-File`; see [testing](../testing.md).
Do not override managed policy. An explicit `-BaseUrl http://127.0.0.1:<port>` may
select the local bridge under test. Output directories and every artifact path
must stay inside `.local/v01/`; artifacts must stay inside their report directory,
contain no traversal and resolve to at most 240 characters. Fixed short JSON
names keep Windows paths bounded. Keep panel captures there as well.

The script writes `report.json`, `catalog.json` and numbered raw read responses.
Reports include fixture/schema versions, native/offline provenance, timestamps,
independent assembly/mod/build/source/game identities, null identity reasons,
installed and candidate SHA256 separately, source revision/dirty state, start/end
city context, per-check expected/actual values, artifacts and limitations. Current
bridge contracts expose neither loaded-module SHA256 nor WorldRef bootId/epoch.
The runner records these as unavailable and leaves identity gates incomplete.
City name/frame are context, not WorldRef. A capabilities runtime observation is
fresh only within 5000ms; a sequential read collection is not atomic.

Required checks are exactly: catalogReadOnly, capabilityExposure, buildIdentity,
worldIdentity, budgetPanel, calendarPanel, serviceCountsPanel, stopRolesPanel and
sliderBoundsPanel. Each appears once and uses `passed`, `failed` or `notRun`, with
expected/actual values, observation timestamp, provenance, binding and artifact
references. Incomplete checks need an explicit reason. Passed checks need present
values and existing registered artifacts. Missing/extra/duplicate checks, invalid
hashes/types/timestamps/enums, boolean strings and contradictory flags are invalid.

`validated` is derived from all required checks being passed.
`compatibilityVerified` additionally requires native provenance and known
build/game/world identity, including loaded-module linkage. Exit 0 means
structurally valid and complete; exit 2 means structurally valid but incomplete
(including failed checks); exit 1 means invalid evidence or invocation. Unknown
version identity is valid incomplete evidence and never a compatibility claim.
An HTTP 200, fake response or finite route length cannot supply panel parity.
Offline fixture success cannot pass native identity/panel gates.

Manually supplied panel observations belong in a reviewed copy of the report,
with `provenance: "manualPanel"`, expected and actual objects containing every
probe named in `panelProbes` in the fixture, and matching values for a passed
check. Record concrete values and units, not a `passed` boolean alone. Attach the
capture and comparison notes; bind each observation to the same fixture,
buildIdentity, bootId and epoch as the report:

```json
{"fixture":"contracts","buildIdentity":"<observed build>","bootId":"<observed WorldRef>","epoch":1}
```

Each check's timestamp must lie inside the report observation interval. Extend
that interval to cover actual manual observations, preserving original collection
timestamps in the raw files. Retain failed and unobserved values and reasons;
do not fabricate a game version, loaded hash or WorldRef to satisfy the schema.
The validator checks structure, binding, probe presence and value equality; a
human must review whether attached captures substantiate the observations. It
does not authenticate screenshots or certify a report merely because fields are
filled in. With today's unavailable WorldRef/loaded identity, fully complete
native evidence remains blocked on later identity support; preserve panel findings
as incomplete evidence rather than inventing linkage.

Manual probes in a later disposable session:

- Budget: compare income and expense signs and balance with the economy panel;
  identify monthly rate versus treasury change and observe refresh across an
  in-game hour. Wall-clock waiting is not in-game elapsed time.
- Calendar: retain raw and displayed date, observe a month rollover and a year
  rollover, and compare native display at each boundary. Current calendar fields
  are raw/unverified until those observations establish semantics.
- Services: compare ordinary service buildings and extensions with panel counts;
  distinguish aggregation from individual entities. Preserve unresolved meanings.
- Stops: retain ordinary bus/tram prefab and instance examples with actual
  PlaceableObjectData roles; compare exclusions for PlaceholderObjectData,
  PlaceholderObjectElement and integrated platform roles. Normal roadside listing
  and route/platform discovery have different coverage. Do not create stops.
- Sliders: in an already warmed fixture record tax, fee, service-budget and an
  existing line-policy slider minimum, maximum and step without changing values.
  Discover line/prefab IDs read-only and capture detail through registered reads
  manually when needed; the runner does not guess IDs. Fee legacy sliderRange is
  deprecated income/export/import data, not bounds. Service 50–150 is the legacy
  bridge clamp, not proven UI bounds; tax integer step is representability only.
  Unknown native steps remain unknown. Deferred setter completion belongs to O3.

Record `/ping` and `/capabilities` at main menu, during loading and in a loaded
disposable city, including paused behavior; never change simulation state for
this read fixture. Compare actual MCP tool discovery/schema with the endpoint
manifest and archive differences, versions and local tool restrictions. Mod 0.9.0,
assembly 0.4.0, MCP package 0.8.0 and protocol/schema 1 are separate identities.
An endpoint being catalogued does not prove exposure in the current MCP allowlist.
Native support remains unverified until reviewed evidence supports it.

Before any separately authorized deployment: finish offline gates, close the
game, preserve the installed mod folder outside Mods, retain its hashes, copy
only the verified candidate DLL and optional PDB, then compare installed readback
against candidate hashes. Reopen the disposable fixture and establish loaded
build linkage; installed-file SHA alone cannot prove what a running game loaded.
For rollback close the game, restore only CS2MCP from that backup (or remove only
those files if absent before), verify restored hashes, then reopen. Preserve
upstream attribution and local allowlist differences. Native fixture/panel checks
in this implementation session are **notRun**; no deployment or publication is
implied by completing offline F6.
