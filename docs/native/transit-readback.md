# Selected transit diagnostics — partial T1

`transit-readback-v1`, G1 and G3 remain **notRun**. The cloud has no game
assemblies: source-backed adapter syntax and portable tests do not establish
installed signatures, committed-route semantics, panel parity or runtime timing.
Full T1 discovery, native route validation, load and dispatch remain open.

`cs2_transit_status` / GET `/v2/transit/status` inspect one full caller-observed
`EntityRef`. The tool accepts `{line:{world:{bootId,epoch},index,version},mode?,purpose?}`.
The compact GET encodes that same world as `bootId`, `epoch` and `line=index:version`;
it does not attach a new current world to historical IDs. O7's fixed
`transit.selectedLine` entry uses the same full-ref arguments and reader. The
catalog rejects conflicting worlds before reserving pending slots, including when
`expectedWorld` is absent. Sampling and publication validate the original world.

Mode filters are `bus|tram|metro|train|airplane`; purpose filters are
`passenger|cargo`. A proven mismatch returns `matched=0` and no selected item.
Any unavailable requested-filter facts instead preserve the diagnostic item with
`matched=null`, `filter.state=unverifiable` and a warning. The response purpose
union adds `mixed` and `unknown` to describe both/neither/unavailable native flags
without guessing a single transport purpose.

| Projection | Existing source evidence and limits |
| --- | --- |
| Root identity, mode, purpose, prefab and immediate owner | `TransitLineQuery`, `TryGetLineTransportData`, `TryResolveLinePrefab`: Route/TransportLine, PrefabRef and TransportLineData transport/passenger/cargo fields; no global query is used by the new reader. |
| Ordered waypoint, connected entity, XYZ and waiting count | `DescribeLineStops`: RouteWaypoint.m_Waypoint, Connected.m_Connected, Routes.Position.m_Position and WaitingPassengers.m_Count. Missing/nonfinite/negative evidence stays qualified. |
| Stop XYZ, immediate owner, attached parent, active flag and role flags | `DescribeTransitStop`: Transform, Owner, Attached, TransportStop/StopFlags.Active, TransportStopData and OutsideConnection marker. Ownership is not a verified platform/station role; stop active is not line active. |
| Segment reference and raw distance | `BridgeTransitToolSystem.TryGetReadyTempRoute/DescribeLineFailure`: RouteSegment.m_Segment and PathInformation.m_Distance. Their use for committed lines, cardinality and distance units needs the native fixture. |

The adapter reads buffer presence and both lengths before any indexed access.
The shared extraction cap is **128 combined waypoint plus segment entries**.
Missing buffers or excess counts yield explicit incomplete evidence and no element
traversal. There is no truncated prefix, recursive ownership/connected-line scan,
city discovery or cursor. Root component/prefab accesses and each indexed element
have fixed source-level call paths, without BuildTransportLine or
GetRouteVehiclesCount. Native getters/job synchronization and execution time still
need Windows measurement; the 2ms scheduler budget cannot preempt a current read.

Conservative direct adapter bounds, counting each EntityManager Exists/Has/Get
invocation separately (including immediate stop/parent/prefab checks):

| Work | EntityManager checks/reads | Buffer access | Other native helper |
| --- | ---: | --- | --- |
| Selected root | <=24 | Two Length reads; no element access | <=1 PrefabSystem.GetPrefab |
| One waypoint and its connected stop metadata | <=32 | One indexed waypoint read | None |
| One segment | <=5 | One indexed segment read | None |

Thus <=4,120 direct EntityManager calls plus two lengths/128 indexed reads bound
the largest accepted extraction conservatively. Handler system acquisition is
one additional existing GetOrCreateSystemManaged call. These counts exclude
opaque helper internals, native dependency synchronization and scheduler/encoder
work; they establish source call cardinality, not a hard runtime or2ms guarantee.

Each slot's `structuralAssessment` is `knownFault|noKnownFault|unassessed`. Observed
missing/deleted/temporary/null required refs and negative/nonfinite segment
distances are known faults even when a null native ref cannot populate a versioned
fault-reference list. Missing optional XYZ/wait metadata is not a reference fault;
missing PathInformation and unsupported connection metadata remain unassessed.
Health derives known faults from slot assessments, not nullable ref-list lengths.
`nativeValidity` always remains `unknown`, including structurally clean samples.
M4 must consume these producer assessments and completeness rather than infer
faults from missing values or count reference-list lengths.

`health.rawLength` is only a complete finite nonnegative sum of observed
PathInformation distances, with `lengthValidation=finiteNonnegativeSegmentsOnly`.
`lengthUnit=null` / `nativeSegmentDistanceUnitNotVerified` is mandatory. The metre
qualification of the separate UITransportLineData.length source does not establish
this field's unit. Initial M4 excludes a route-length metric. Missing required
evidence, cardinality differences and incomplete traversal prevent a usable sum.

GET and O7 share a finite-number, actual UTF8 **256KiB** item encoder. The entry
cap and byte cap are independent: a supported 128-entry extraction can exceed the
wire cap with long escaped worlds repeated in full refs. It then returns explicit
`RESULT_TOO_LARGE`, without shortening IDs, dropping arrays or weakening refs.
Portable serializer checks cover 64/64 and 128/0 entry distributions, ordinary UUID
worlds and adversarial escaped 128-character worlds; these are not a claim that
all possible 128-entry responses fit.

`cs2_transit_capabilities` / GET `/v2/transit/capabilities` accepts no arguments and
returns the same fixed partial descriptor present in every status page. The
global capability manifest marks both readers degraded and native support
unverified. Name, line active state, vehicle/load/capacity/occupancy, native
validity, dispatch readiness and duplicate discovery remain unavailable. Existing
legacy fields and tools retain their behavior.

The declarative `tests/native/transit-readback-v1.json` is a manual read-only
handoff, not a newly executable runner registration. On Windows, first compile
the exact reviewed source using existing references and `SkipDeploy=true`. In a
disposable paused fixture, compare passenger/cargo/air selected-line and stop
facts against panels, inspect committed segment cardinality and missing/path
states, verify immediate ownership, and measure bytes/time. Do not load cities,
mutate lines, advance simulation or deploy as a hidden validation step. Record
unsupported readings and source/candidate/installed/loaded identities separately.
