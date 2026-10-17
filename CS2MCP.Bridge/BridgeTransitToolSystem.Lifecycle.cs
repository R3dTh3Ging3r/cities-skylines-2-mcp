using System;
using System.Collections.Generic;
using Game;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using CS2MCP.Core;
using AgeMask = Game.Tools.AgeMask;
using Transform = Game.Objects.Transform;

namespace CS2MCP
{
    // Exact production lifecycle flow, split from native definition/readback leaves for execution tests.
    public sealed partial class BridgeTransitToolSystem
    {
        public struct LineStop
        {
            public Entity Stop;
            public float3 Position;
        }

        private LifecycleTraceContext m_TraceContext;
        private TraceKind m_TraceKind;
        private void BeginTrace(BridgeRequest request, TraceKind kind)
        {
            try
            {
                m_TraceContext = ConstructionLifecycleTrace.Start(request);
                m_TraceKind = kind;
                Trace(TracePhase.queued);
            }
            catch
            {
                ConstructionLifecycleTrace.Loss();
            }
        }

        private void Trace(TracePhase phase)
        {
            try
            {
                ConstructionLifecycleTrace.Record(m_TraceContext, TraceAdapter.transit, m_TraceKind, phase, () => m_ToolSystem.activeTool == this, references: () => TraceReferences(), omitted: Math.Max(0, (m_LineStops?.Length ?? 0) - 2));
            }
            catch
            {
                ConstructionLifecycleTrace.Loss();
            }
        }

        private LifecycleTraceReference[] TraceReferences()
        {
            var refs = new List<LifecycleTraceReference>
            {
                ConstructionLifecycleTrace.Reference(m_TraceContext, TraceRole.prefab, m_PendingPrefabEntity),
                ConstructionLifecycleTrace.Reference(m_TraceContext, m_PendingKind == OperationKind.Line ? TraceRole.line : TraceRole.stop, m_ResultEntity)
            };
            if (m_LineStops != null)
                for (int i = 0; i < Math.Min(2, m_LineStops.Length); i++)
                    refs.Add(ConstructionLifecycleTrace.Reference(m_TraceContext, TraceRole.stop, m_LineStops[i].Stop));
            return refs.ToArray();
        }

        private void TraceBusy(BridgeRequest request)
        {
            try
            {
                ConstructionLifecycleTrace.Record(ConstructionLifecycleTrace.Start(request), TraceAdapter.transit, TraceKind.direct, TracePhase.busy);
            }
            catch
            {
                ConstructionLifecycleTrace.Loss();
            }
        }

        private enum Stage
        {
            Idle,
            CreateDefinitions,
            WaitForPaths,
            Apply,
            Finish,
        }

        private enum OperationKind
        {
            Stop,
            Line,
        }

        // The HTTP layer gives up after 10s; answer well before that.
        private const double kPathfindTimeoutSeconds = 6.5;
        private Stage m_Stage = Stage.Idle;
        private OperationKind m_PendingKind;
        private BridgeRequest m_PendingRequest;
        private ToolBaseSystem m_PreviousTool;
        private Entity m_PendingPrefabEntity;
        private PrefabBase m_PendingPrefab;
        private string m_PendingTypeName;
        private DateTime m_Deadline;
        private int m_DefinitionRounds;
        private ControlPoint m_StopControlPoint;
        private LineStop[] m_LineStops;
        private bool m_HasLineColor;
        private UnityEngine.Color32 m_LineColor;
        private string m_LineName;
        private int m_DepotCount;
        // Set by the Apply/WaitForPaths stage; reported by Finish once committed.
        private Entity m_ResultEntity;
        private bool m_Applied;
        private DeferredWorldWork m_LineContinuation;
        public override string toolID => "CS2MCP.Transit";
        public bool IsBusy => m_Stage != Stage.Idle;

        public override PrefabBase GetPrefab()
        {
            return m_PendingPrefab;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            // Never let the game UI select this tool via asset selection.
            return false;
        }

        /// <summary>Must be called on the simulation thread.</summary>
        public bool TryQueueStop(Entity prefabEntity, PrefabBase prefab, string typeName, ControlPoint controlPoint, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.Stop;
            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingTypeName = typeName;
            m_StopControlPoint = controlPoint;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.stop);
            Activate();
            return true;
        }

        /// <summary>Must be called on the simulation thread.</summary>
        public bool TryQueueLine(Entity prefabEntity, PrefabBase prefab, string typeName, LineStop[] stops, bool hasColor, UnityEngine.Color32 color, string name, int depotCount, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            // Capture before the native creation stages; never resample after a line has been applied.
            m_LineContinuation = new DeferredWorldWork(Mod.Worlds, request?.IngressWorld, true, "partial", request?.OperationId);
            m_PendingKind = OperationKind.Line;
            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingTypeName = typeName;
            m_LineStops = stops;
            m_HasLineColor = hasColor;
            m_LineColor = color;
            m_LineName = name;
            m_DepotCount = depotCount;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.line);
            Activate();
            return true;
        }

        private void Activate()
        {
            m_ResultEntity = Entity.Null;
            m_Applied = false;
            m_DefinitionRounds = 0;
            m_Deadline = DateTime.UtcNow.AddSeconds(kPathfindTimeoutSeconds);
            m_Stage = Stage.CreateDefinitions;
            m_PreviousTool = m_ToolSystem.activeTool;
            m_ToolSystem.activeTool = this;
        }

        [Preserve]
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            try
            {
                switch (m_Stage)
                {
                    case Stage.CreateDefinitions:
                        Trace(TracePhase.definitions);
                        applyMode = ApplyMode.Clear;
                        if (m_PendingKind == OperationKind.Stop)
                        {
                            CreateStopDefinitions();
                            m_Stage = Stage.Apply;
                        }
                        else
                        {
                            CreateLineDefinitions();
                            m_Stage = Stage.WaitForPaths;
                        }

                        break;
                    case Stage.WaitForPaths:
                        UpdateWaitForPaths();
                        break;
                    case Stage.Apply:
                        ApplyStop();
                        break;
                    case Stage.Finish:
                        Trace(TracePhase.finishObserved);
                        if (m_PendingKind == OperationKind.Line)
                        {
                            if (m_LineContinuation == null || !m_LineContinuation.TryRun(() => applyMode = ApplyMode.None, out _))
                            {
                                CompletePending(BridgeResponse.Error(409, "line finish lost its captured world/readiness; original creation may already have applied", ErrorCodes.WorldMismatch, outcome: "partial", operationId: m_PendingRequest?.OperationId));
                                Deactivate(); // Managed clearing only when the captured line context is invalid.
                                break;
                            }
                        }
                        else
                            applyMode = ApplyMode.None;
                        if (m_Applied)
                        {
                            if (m_PendingKind == OperationKind.Stop)
                            {
                                CompletePending(BuildStopResponse());
                            }
                            else
                            {
                                FinishLine();
                            }
                        }

                        Deactivate();
                        break;
                    default:
                        if (m_PendingKind == OperationKind.Line)
                        {
                            // A skipped stale restoration must not be retried unguarded on the next idle update.
                            m_LineContinuation?.TryRun(() =>
                            {
                                applyMode = ApplyMode.None;
                                if (m_ToolSystem.activeTool == this)
                                {
                                    Trace(TracePhase.restorationAttempt);
                                    m_ToolSystem.activeTool = m_DefaultToolSystem;
                                }
                            }, out _);
                            break;
                        }

                        applyMode = ApplyMode.None;
                        // Idle but active (e.g. another bridge tool restored us as its
                        // "previous tool"): hand control back to the default tool.
                        if (m_ToolSystem.activeTool == this)
                        {
                            Trace(TracePhase.restorationAttempt);
                            m_ToolSystem.activeTool = m_DefaultToolSystem;
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                Trace(TracePhase.exceptionObserved);
                Mod.Log.Warn($"BridgeTransitToolSystem error in stage {m_Stage}: {e}");
                string detail = $"{e.GetType().Name}: {e.Message}";
                CompletePending(BridgeResponse.Error(500, m_Applied ? $"the game applied the transit operation, but finishing it failed ({detail}); " + "check cs2_list_transit_lines / cs2_list_transit_stops" : $"transit operation failed: {detail}"));
                if (m_PendingKind == OperationKind.Line)
                    m_LineContinuation?.TryRun(() => applyMode = ApplyMode.Clear, out _);
                else
                    applyMode = ApplyMode.Clear;
                Deactivate();
            }

            return inputDeps;
        }

        [Preserve]
        protected override void OnStopRunning()
        {
            if (m_Stage != Stage.Idle && m_ToolSystem.activeTool != this)
            {
                Trace(TracePhase.displacedObserved);
                // Another tool took over mid-operation; fail fast instead of
                // letting the HTTP call time out.
                CompletePending(BridgeResponse.Error(409, "transit operation interrupted because another tool became active; " + "check cs2_list_transit_lines / cs2_list_transit_stops before retrying"));
                m_Stage = Stage.Idle;
                m_LineContinuation = null;
                m_PendingPrefab = null;
                m_PendingPrefabEntity = Entity.Null;
                m_LineStops = null;
                m_PreviousTool = null;
            }

            base.OnStopRunning();
        }

        private void UpdateWaitForPaths()
        {
            bool timedOut = DateTime.UtcNow > m_Deadline;
            if (!timedOut && HasTempPathUpdates())
            {
                // Mirrors RouteToolSystem.Update: a path result for a temp segment
                // re-emits the same definitions, so the regenerated temp route
                // reuses the computed paths and ValidationSystem re-checks it.
                applyMode = ApplyMode.Clear;
                CreateLineDefinitions();
                return;
            }

            if (TryGetReadyTempRoute(out Entity tempRoute))
            {
                Trace(TracePhase.validation);
                if (GetAllowApply())
                {
                    Trace(TracePhase.validationAccepted);
                    m_ResultEntity = tempRoute;
                    m_Applied = true;
                    applyMode = ApplyMode.Apply;
                    Trace(TracePhase.applyObserved);
                }
                else
                {
                    Trace(TracePhase.validationRejected);
                    string reason = DescribeLineFailure(tempRoute);
                    applyMode = ApplyMode.Clear;
                    CompletePending(BridgeResponse.Error(409, reason, ErrorCodes.NativeRejected, outcome: "notApplied"));
                }

                m_Stage = Stage.Finish;
                return;
            }

            applyMode = ApplyMode.None;
            if (timedOut)
            {
                applyMode = ApplyMode.Clear;
                CompletePending(BridgeResponse.Error(409, $"the game did not finish pathfinding the line within {kPathfindTimeoutSeconds}s " + $"({m_DefinitionRounds} definition rounds); nothing was created. Retry, or use fewer/closer stops.", ErrorCodes.NativeRejected, outcome: "notApplied"));
                m_Stage = Stage.Finish;
            }
        }

        private void ApplyStop()
        {
            Trace(TracePhase.validation);
            Entity tempStop = FindTempStop();
            if (tempStop != Entity.Null && GetAllowApply())
            {
                Trace(TracePhase.validationAccepted);
                m_ResultEntity = tempStop;
                m_Applied = true;
                applyMode = ApplyMode.Apply;
                Trace(TracePhase.applyObserved);
            }
            else
            {
                Trace(TracePhase.validationRejected);
                List<string> errors = CollectTempErrors();
                applyMode = ApplyMode.Clear;
                string detail = errors.Count > 0 ? string.Join(", ", errors) : tempStop == Entity.Null ? "the game generated no stop at that spot" : "overlap, unsuitable road, missing sidewalk, not enough money...";
                CompletePending(BridgeResponse.Error(409, $"stop placement blocked by game validation ({detail}); try another spot or road segment", ErrorCodes.NativeRejected, outcome: "notApplied"));
            }

            m_Stage = Stage.Finish;
        }

        private void FinishLine()
        {
            DeferredWorldWork work = m_LineContinuation;
            if (work == null)
            {
                CompletePending(BridgeResponse.Error(500, "line continuation context is unavailable; prior creation outcome is unknown", ErrorCodes.OutcomeUnknown, outcome: "unknown"));
                return;
            }

            if (!work.TryRun(() => PrepareLineResponse(work), out Func<string, BridgeResponse> buildResponse, out var error) || !work.TryRun(() => IsCommitted(m_ResultEntity), out bool committed, out error))
            {
                CompletePending(BridgeResponse.Error(error.httpStatus ?? 500, error.message, error.code, retryable: false, outcome: error.outcome, operationId: error.operationId));
                return;
            }

            if (!string.IsNullOrEmpty(m_LineName) && committed)
            {
                if (work.TryRun(() => base.World.GetOrCreateSystemManaged<BridgeTransitRenameSystem>().Enqueue(m_ResultEntity, m_LineName, m_PendingRequest, buildResponse, work), out error))
                    m_PendingRequest = null;
                else
                    CompletePending(BridgeResponse.Error(error.httpStatus ?? 500, error.message, error.code, retryable: false, outcome: error.outcome, operationId: error.operationId));
                return;
            }

            if (work.TryRun(() => buildResponse(null), out BridgeResponse response, out error))
                CompletePending(response);
            else
                CompletePending(BridgeResponse.Error(error.httpStatus ?? 500, error.message, error.code, retryable: false, outcome: error.outcome, operationId: error.operationId));
        }

        private void CompletePending(BridgeResponse response)
        {
            m_PendingRequest?.Complete(response);
            m_PendingRequest = null;
        }

        private void Deactivate()
        {
            Trace(TracePhase.deactivateObserved);
            DeferredWorldWork lineWork = m_LineContinuation;
            bool line = m_PendingKind == OperationKind.Line;
            ToolBaseSystem previous = m_PreviousTool;
            m_Stage = Stage.Idle;
            if (!line)
                m_LineContinuation = null;
            m_PendingRequest = null;
            m_PendingPrefab = null;
            m_PendingPrefabEntity = Entity.Null;
            m_LineStops = null;
            m_ResultEntity = Entity.Null;
            m_Applied = false;
            m_PreviousTool = null;
            if (line)
            {
                // Clearing these managed fields is not proof of native cleanup. In particular, a stale
                // line must not even read activeTool or restore an old tool in the replacement world.
                if (lineWork != null && !lineWork.TryRun(() =>
                {
                    if (m_ToolSystem.activeTool == this)
                    {
                        Trace(TracePhase.restorationAttempt);
                        m_ToolSystem.activeTool = previous != null && previous != this ? previous : m_DefaultToolSystem;
                    }
                }, out var error))
                    Mod.Log.Warn($"line tool restoration skipped/uncertain: {error.message}");
                return;
            }

            if (m_ToolSystem.activeTool == this)
            {
                Trace(TracePhase.restorationAttempt);
                m_ToolSystem.activeTool = previous != null && previous != this ? previous : m_DefaultToolSystem;
            }
        }
    }
}
