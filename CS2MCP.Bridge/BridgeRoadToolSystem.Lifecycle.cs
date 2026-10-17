using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using CS2MCP.Core;

namespace CS2MCP
{
    // Exact production lifecycle flow, split from native definition/readback leaves for execution tests.
    public sealed partial class BridgeRoadToolSystem
    {
        /// <summary>One end of a built segment: an existing node, a split point on an existing segment, or a free point.</summary>
        public struct BuildEnd
        {
            public Entity Entity;
            public float Split;
            public float3 Position;
            public float Elevation;
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
                ConstructionLifecycleTrace.Record(m_TraceContext, TraceAdapter.road, m_TraceKind, phase, () => m_ToolSystem.activeTool == this, references: () => TraceReferences(), omitted: Math.Max(0, (m_PendingEdges?.Length ?? 0) - 3));
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
                ConstructionLifecycleTrace.Reference(m_TraceContext, TraceRole.prefab, m_PendingPrefabEntity)
            };
            if (m_Mode == Mode.Build)
            {
                refs.Add(ConstructionLifecycleTrace.Reference(m_TraceContext, TraceRole.start, m_BuildStart.Entity));
                refs.Add(ConstructionLifecycleTrace.Reference(m_TraceContext, TraceRole.end, m_BuildEnd.Entity));
            }

            if (m_PendingEdges != null)
                for (int i = 0; i < Math.Min(3, m_PendingEdges.Length); i++)
                    refs.Add(ConstructionLifecycleTrace.Reference(m_TraceContext, TraceRole.target, m_PendingEdges[i]));
            return refs.ToArray();
        }

        private void TraceBusy(BridgeRequest request)
        {
            try
            {
                ConstructionLifecycleTrace.Record(ConstructionLifecycleTrace.Start(request), TraceAdapter.road, TraceKind.direct, TracePhase.busy);
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
            Apply,
            Finish,
        }

        private enum Mode
        {
            Replace,
            Build,
        }

        private Stage m_Stage = Stage.Idle;
        private Mode m_Mode;
        private BuildEnd m_BuildStart;
        private BuildEnd m_BuildEnd;
        private Bezier4x3 m_BuildCurve;
        private Entity[] m_PendingEdges;
        private float3[] m_PendingMidpoints;
        private bool m_PendingInvert;
        private Entity m_PendingPrefabEntity;
        private PrefabBase m_PendingPrefab;
        private BridgeRequest m_PendingRequest;
        private ToolBaseSystem m_PreviousTool;
        private bool m_Applied;
        public override string toolID => "CS2MCP.Road";
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
        public bool TryQueueReplace(Entity[] edges, Entity prefabEntity, PrefabBase prefab, bool invert, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingEdges = edges;
            m_PendingMidpoints = new float3[edges.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                m_PendingMidpoints[i] = MathUtils.Position(EntityManager.GetComponentData<Curve>(edges[i]).m_Bezier, 0.5f);
            }

            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingInvert = invert;
            m_PendingRequest = request;
            m_Applied = false;
            m_Mode = Mode.Replace;
            BeginTrace(request, TraceKind.roadReplace);
            m_Stage = Stage.CreateDefinitions;
            m_PreviousTool = m_ToolSystem.activeTool;
            m_ToolSystem.activeTool = this;
            return true;
        }

        /// <summary>Must be called on the simulation thread. The curve must start at start.Position and end at end.Position.</summary>
        public bool TryQueueBuild(Entity prefabEntity, PrefabBase prefab, BuildEnd start, BuildEnd end, Bezier4x3 curve, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_BuildStart = start;
            m_BuildEnd = end;
            m_BuildCurve = curve;
            m_PendingEdges = null;
            m_PendingMidpoints = null;
            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingInvert = false;
            m_PendingRequest = request;
            m_Applied = false;
            m_Mode = Mode.Build;
            BeginTrace(request, TraceKind.roadBuild);
            m_Stage = Stage.CreateDefinitions;
            m_PreviousTool = m_ToolSystem.activeTool;
            m_ToolSystem.activeTool = this;
            return true;
        }

        private string OperationName => m_Mode == Mode.Build ? "building the connection" : "road replacement";

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
                        if (m_Mode == Mode.Build)
                        {
                            CreateBuildDefinitions();
                        }
                        else
                        {
                            CreateReplaceDefinitions();
                        }

                        m_Stage = Stage.Apply;
                        break;
                    case Stage.Apply:
                        Trace(TracePhase.validation);
                        if (GetAllowApply())
                        {
                            Trace(TracePhase.validationAccepted);
                            applyMode = ApplyMode.Apply;
                            Trace(TracePhase.applyObserved);
                            m_Applied = true;
                        }
                        else
                        {
                            Trace(TracePhase.validationRejected);
                            List<string> errors = CollectTempErrors();
                            applyMode = ApplyMode.Clear;
                            CompletePending(BridgeResponse.Error(409, OperationName + " blocked by game validation (" + (errors.Count > 0 ? string.Join(", ", errors) : "overlap, unsupported road type, protected segment...") + "); nothing was changed", ErrorCodes.NativeRejected, outcome: "notApplied"));
                        }

                        m_Stage = Stage.Finish;
                        break;
                    case Stage.Finish:
                        Trace(TracePhase.finishObserved);
                        applyMode = ApplyMode.None;
                        if (m_Applied)
                        {
                            CompletePending(m_Mode == Mode.Build ? BuildConnectResponse() : BuildReplaceResponse());
                        }

                        Deactivate();
                        break;
                    default:
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
                Mod.Log.Warn($"BridgeRoadToolSystem error in stage {m_Stage}: {e}");
                string detail = $"{e.GetType().Name}: {e.Message}";
                CompletePending(BridgeResponse.Error(500, m_Applied ? $"the game applied the change, but finishing {OperationName} failed ({detail}); check cs2_list_roads" : $"{OperationName} failed: {detail}"));
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
                CompletePending(BridgeResponse.Error(409, OperationName + " interrupted because another tool became active; check cs2_list_roads before retrying"));
                m_Stage = Stage.Idle;
                m_PendingEdges = null;
                m_PendingPrefab = null;
                m_PendingPrefabEntity = Entity.Null;
                m_PreviousTool = null;
            }

            base.OnStopRunning();
        }

        private void CompletePending(BridgeResponse response)
        {
            m_PendingRequest?.Complete(response);
            m_PendingRequest = null;
        }

        private void Deactivate()
        {
            Trace(TracePhase.deactivateObserved);
            m_Stage = Stage.Idle;
            m_PendingRequest = null;
            m_PendingEdges = null;
            m_PendingPrefab = null;
            m_PendingPrefabEntity = Entity.Null;
            m_Applied = false;
            if (m_ToolSystem.activeTool == this)
            {
                Trace(TracePhase.restorationAttempt);
                m_ToolSystem.activeTool = m_PreviousTool != null && m_PreviousTool != this ? m_PreviousTool : m_DefaultToolSystem;
            }

            m_PreviousTool = null;
        }
    }
}
