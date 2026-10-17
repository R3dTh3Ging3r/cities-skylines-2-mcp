# Next construction lifecycle increment after the native expansion

Status: development sequence and evidence requirements. The observation increment under the [bounded trace plan](2026-10-17-construction-lifecycle-trace.md) is implemented and independently accepted within its [source/offline scope](../../handoff/LIFECYCLE-TRACE-2026-10-17.md); it is not native accepted and does not complete O3/O5. This follows the [October 17 reconciliation](../../handoff/POST-PLAYTEST-2026-10-17.md) and refines O3/O5 in the [operations plan](2026-10-11-bridge-operations.md). No additional playtest is requested by this document.

## What changed

The tested `0b913a3` candidate built against CS2 1.6.2f1 and supported a real road/park/zoning expansion. Nine successful legacy mutation receipts received readbacks. This is useful evidence that these existing adapters perform those actions in the observed city. It supplies representative building, road and zoning cases for the next lifecycle work; it does not instrument or establish native Apply/Finish ordering, writer ownership, cancellation, delayed effects, or durable saves.

## Source facts and dependencies

`BridgeToolSystem.OnUpdate` calls `CompletePending(BuildSuccessResponse())` in Stage.Apply, then transitions to Stage.Finish. Finish clears applyMode and deactivates; the acknowledgement is therefore earlier than that cleanup path. A new coordinator cannot equate the response with verified completion. Keep road and transit adapters separate until their own Finish/error/readback behavior is mapped; do not normalize all adapters to the generic placement assumption.

`OperationRegistry` already rejects success without a verified receipt and prevents implicit terminal transitions/replay. The production authority still refuses native v2 dispatch. Integrate one coordinator with this registry; do not create an independent queue or manufacture a successful receipt from an observed stage name.

| Adapter at the tested source | Present acknowledgement/interruption boundary | Evidence still needed |
| --- | --- | --- |
| `BridgeToolSystem` generic placement/modification | Replies in Apply, deactivates in later Finish | Committed entities and cleanup after that early reply; interruption at each phase |
| `BridgeRoadToolSystem` build/replace | Sets m_Applied in Apply, builds reply in Finish before Deactivate; OnStopRunning clears pending state on displacement | Native playback, exact effect/cost readback and late effects when interrupted before/after Apply |
| `BridgeTransitToolSystem` stop/line | Finish reads stop/line results; line continuation has world fencing; OnStopRunning handles displacement | Path/commit/rename completion versus acknowledgement; cleanup and delayed naming outcomes |
| Direct zoning and setters | Bypass these tool stage machines | A complete admission map and action-specific readback, not inferred tool ownership |
| `RequestHandlers.Meta.SaveGame` | Returns saving=true after discarding Save task | Task/event/destination correlation, durable artifact and preview ownership |

Retained native metadata identifies active-tool setter/event and later stop callbacks but does not prove their runtime order or cleanup. A tool switch must not be treated as rollback or permission to reactivate the prior bridge tool. `RequestHandlers.Meta.SaveGame` still discards the Save task. A successful Task<bool>, preview event URI, or readable older checkpoint does not identify a completed new save package.

## Next source package: consumed lifecycle observation

1. Map all concrete construction dispatch, reply, Apply, Finish, exception and deactivation sites in the generic, road and transit adapters. Record which sites are actual source observations and which desired postconditions have no verified native signal. Include direct zoning/setters in the writer-coverage map even though they bypass ToolBaseSystem.
2. Add bounded, passive correlation at these real sites before changing completion semantics. A legacy diagnostic correlation ID must be explicitly distinguished from an admitted v2 operation UUID. Capture world, frame, adapter, phase, acknowledgement and observed ownership; include only exact entity references already available from the adapter. Do not add citywide scans, tool switches, scheduling changes, saves or gameplay calls. Unknown effect/cleanup evidence stays unknown.
3. Consume that trace through a fixed bounded read surface and the existing fixture evidence format. Specify memory/event/result limits before implementation, expose retention gaps and world invalidation, and keep ordinary operation independent of trace consumers. No filesystem logging of city data by default, unbounded queues, per-frame growing strings or arbitrary endpoint dispatch.
4. Verify linked production control flow using offline doubles: acknowledgement precedes cleanup, rejected validation, exceptions before/after Apply, displaced tool, old-world callback, duplicate/late completion and trace overflow. Prove observation cannot grant admission, change tool restoration, turn a timeout into cancellation, or mark cleanup/effects verified. Keep C# Framework builds distinct from executable portable harness evidence.

This is a useful offline candidate only if the trace is wired to production adapters and consumed by the read/fixture path. A standalone phase-policy class without those consumers does not complete it. An independent review must evaluate behavior preservation, boundedness and the evidence labels. A later installed-reference compile remains necessary for any new game API binding; native observation remains necessary for callback ordering. Neither is authorized here.

## Completion and authority after evidence

Once the adapter evidence supports concrete verification and cleanup predicates, implement O3 coordinator integration with its original failure/reconciliation tests. Preserve an unknown or indeterminate result after possible Apply; quarantine the writer until actual reconciliation, with no automatic replay or reset. Keep separate created/changed/reused/removed references and known/unknown costs. Successful road readback from this playtest does not prove every adapter's cleanup predicate.

O5 follows with one shared admission decision across every writer, immutable world/operation context at ingress and apply, bounded priority takeover, and the actual native user-control hook. Offline denied-admission tests must demonstrate zero native calls for each classified route, including direct setters, zoning, simulation, camera and saves. A trace of a tool change alone does not establish a reliable visible takeover action. Preserve the release plan's explicit legacy migration/default-off behavior as a coordinated change, not a silent change in a diagnostic PR.

Only then can O7 sequential mutation batches start the next action after verified completion/cleanup. O6 owned timed simulation needs the same authority plus verified pause/termination. C2 preview cleanup and action-specific C3/C4/C5 preservation still need their own evidence. O4 save attribution/preview ownership is a separate prerequisite for reliable new checkpoints, not an inference from this session's existing recovery save.

## Indispensable native evidence still absent

- Apply/Finish/deferred playback and cleanup ordering under normal operation, rejection and displacement; late effects and exact effect/cost readback.
- Actual user takeover and pause semantics, including all direct/deferred writers and world reload.
- Destination package identity and durability for a newly requested save; save task/event success and preview ownership.
- Action-specific preview validity/cleanup, connector/overlay preservation, and route edit/dispatch behavior.

Accumulate reviewed source packages and their offline tests before the next consolidated native validation. Do not mark these gates passed, enable guarded writers, or solicit a routine small test merely because the legacy expansion succeeded. If a source decision depends on one of these missing native facts, retain the exact blocked decision and proceed with independent work.
