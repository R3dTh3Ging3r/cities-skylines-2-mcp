using System;
using CS2MCP.Core;
using Colossal.Mathematics;
using Game;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using AgeMask = Game.Tools.AgeMask;
using Transform = Game.Objects.Transform;

namespace CS2MCP
{
    // Exact production lifecycle flow, split from native definition/readback leaves for execution tests.
    public sealed partial class BridgeToolSystem
    {
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
                ConstructionLifecycleTrace.Record(m_TraceContext, TraceAdapter.generic, m_TraceKind, phase, () => m_ToolSystem.activeTool == this, references: () => TraceReferences(), omitted: 0);
            }
            catch
            {
                ConstructionLifecycleTrace.Loss();
            }
        }

        private LifecycleTraceReference[] TraceReferences()
        {
            bool target = m_PendingKind == OperationKind.Demolish || m_PendingKind == OperationKind.Upgrade || m_PendingKind == OperationKind.NodeUpgrade;
            return new[]
            {
                ConstructionLifecycleTrace.Reference(m_TraceContext, target ? TraceRole.target : TraceRole.prefab, target ? m_PendingTarget : m_PendingPrefabEntity)
            };
        }

        private void TraceBusy(BridgeRequest request)
        {
            try
            {
                ConstructionLifecycleTrace.Record(ConstructionLifecycleTrace.Start(request), TraceAdapter.generic, TraceKind.direct, TracePhase.busy);
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

        private enum OperationKind
        {
            Object,
            Net,
            Demolish,
            Upgrade,
            Area,
            NodeUpgrade,
        }

        private Stage m_Stage = Stage.Idle;
        private OperationKind m_PendingKind;
        private Entity m_PendingPrefabEntity;
        private Entity m_PendingTarget;
        private PrefabBase m_PendingPrefab;
        private string m_PendingLabel;
        private float3 m_PendingPosition;
        private float3 m_PendingEnd;
        private float3 m_PendingMid;
        private bool m_PendingHasMid;
        private CompositionFlags m_PendingUpgradeFlags;
        private float3[] m_PendingAreaNodes;
        private float2 m_PendingElevations;
        private quaternion m_PendingRotation;
        private BridgeRequest m_PendingRequest;
        private ToolBaseSystem m_PreviousTool;
        public override string toolID => "CS2MCP.Bridge";
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
        public bool TryQueuePlacement(Entity prefabEntity, PrefabBase prefab, float3 position, quaternion rotation, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.Object;
            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingPosition = position;
            m_PendingRotation = rotation;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.placement);
            Activate();
            return true;
        }

        /// <summary>Must be called on the simulation thread.</summary>
        public bool TryQueueRoad(Entity prefabEntity, PrefabBase prefab, float3 start, float3 end, float3 mid, bool hasMid, float2 elevations, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.Net;
            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingPosition = start;
            m_PendingEnd = end;
            m_PendingMid = mid;
            m_PendingHasMid = hasMid;
            m_PendingElevations = elevations;
            m_PendingRotation = quaternion.identity;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.net);
            Activate();
            return true;
        }

        /// <summary>Must be called on the simulation thread.</summary>
        public bool TryQueueUpgrade(Entity target, string label, CompositionFlags upgradeFlags, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.Upgrade;
            m_PendingTarget = target;
            m_PendingLabel = label;
            m_PendingUpgradeFlags = upgradeFlags;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.upgrade);
            Activate();
            return true;
        }

        /// <summary>
        /// Must be called on the simulation thread. Replaces a junction's upgrade
        /// flags (traffic lights, all-way stop...) with finalFlags, as the game's
        /// intersection tools do.
        /// </summary>
        public bool TryQueueNodeUpgrade(Entity node, string label, CompositionFlags finalFlags, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.NodeUpgrade;
            m_PendingTarget = node;
            m_PendingLabel = label;
            m_PendingUpgradeFlags = finalFlags;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.nodeUpgrade);
            Activate();
            return true;
        }

        /// <summary>Must be called on the simulation thread.</summary>
        public bool TryQueueDemolish(Entity target, string label, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.Demolish;
            m_PendingTarget = target;
            m_PendingLabel = label;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.demolish);
            Activate();
            return true;
        }

        /// <summary>Must be called on the simulation thread.</summary>
        public bool TryQueueArea(Entity prefabEntity, PrefabBase prefab, float3[] polygonNodes, BridgeRequest request)
        {
            if (m_Stage != Stage.Idle)
            {
                TraceBusy(request);
                return false;
            }

            m_PendingKind = OperationKind.Area;
            m_PendingPrefabEntity = prefabEntity;
            m_PendingPrefab = prefab;
            m_PendingAreaNodes = polygonNodes;
            m_PendingRequest = request;
            BeginTrace(request, TraceKind.area);
            Activate();
            return true;
        }

        private void Activate()
        {
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
                        switch (m_PendingKind)
                        {
                            case OperationKind.Object:
                                CreatePlacementDefinitions();
                                break;
                            case OperationKind.Net:
                                CreateRoadDefinitions();
                                break;
                            case OperationKind.Demolish:
                                CreateModifyDefinitions(CreationFlags.Delete, default);
                                break;
                            case OperationKind.Upgrade:
                                CreateModifyDefinitions(CreationFlags.Upgrade, m_PendingUpgradeFlags);
                                break;
                            case OperationKind.Area:
                                CreateAreaDefinitions();
                                break;
                            case OperationKind.NodeUpgrade:
                                CreateNodeUpgradeDefinition();
                                break;
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
                            CompletePending(BuildSuccessResponse());
                        }
                        else
                        {
                            Trace(TracePhase.validationRejected);
                            applyMode = ApplyMode.Clear;
                            CompletePending(BridgeResponse.Error(409, "operation blocked by game validation (overlap, water, steep terrain, protected entity...); " + "try a different position or target"));
                        }

                        m_Stage = Stage.Finish;
                        break;
                    case Stage.Finish:
                        Trace(TracePhase.finishObserved);
                        applyMode = ApplyMode.None;
                        Deactivate();
                        break;
                    default:
                        applyMode = ApplyMode.None;
                        break;
                }
            }
            catch (Exception e)
            {
                Trace(TracePhase.exceptionObserved);
                Mod.Log.Warn($"BridgeToolSystem error in stage {m_Stage}: {e}");
                CompletePending(BridgeResponse.Error(500, $"placement failed: {e.GetType().Name}: {e.Message}"));
                applyMode = ApplyMode.None;
                Deactivate();
            }

            return inputDeps;
        }

        [Preserve]
        protected override void OnStopRunning()
        {
            // Observe only: the legacy generic adapter has no displacement cleanup.
            Trace(TracePhase.stopObserved);
            base.OnStopRunning();
        }

        private BridgeResponse BuildSuccessResponse()
        {
            switch (m_PendingKind)
            {
                case OperationKind.Demolish:
                    return BridgeResponse.Json(new { demolished = true, prefab = m_PendingLabel, entity = new { index = m_PendingTarget.Index, version = m_PendingTarget.Version }, note = "deleted via the game's bulldoze pipeline (nodes/blocks/lanes cleaned up by the game)", });
                case OperationKind.Upgrade:
                    return BridgeResponse.Json(new { upgraded = true, prefab = m_PendingLabel, entity = new { index = m_PendingTarget.Index, version = m_PendingTarget.Version }, note = "upgrade applied via the tool pipeline; the segment is recreated with the new composition", });
                case OperationKind.NodeUpgrade:
                    return BridgeResponse.Json(new { upgraded = true, junction = new { index = m_PendingTarget.Index, version = m_PendingTarget.Version }, mode = m_PendingLabel, note = "junction upgrade applied via the tool pipeline", });
                case OperationKind.Net:
                    return BridgeResponse.Json(new { placed = true, prefab = m_PendingPrefab != null ? m_PendingPrefab.name : null, start = new { x = m_PendingPosition.x, z = m_PendingPosition.z }, end = new { x = m_PendingEnd.x, z = m_PendingEnd.z }, note = "committed this frame; verify via /city/roads or /screenshot", });
                case OperationKind.Area:
                    return BridgeResponse.Json(new { created = true, prefab = m_PendingPrefab != null ? m_PendingPrefab.name : null, nodes = m_PendingAreaNodes != null ? m_PendingAreaNodes.Length : 0, note = "area committed this frame; list districts via /districts", });
                default:
                    return BridgeResponse.Json(new { placed = true, prefab = m_PendingPrefab != null ? m_PendingPrefab.name : null, position = new { x = m_PendingPosition.x, y = m_PendingPosition.y, z = m_PendingPosition.z, }, note = "committed this frame; verify via /city/buildings or /screenshot", });
            }
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
            m_PendingPrefab = null;
            m_PendingPrefabEntity = Entity.Null;
            if (m_ToolSystem.activeTool == this)
            {
                Trace(TracePhase.restorationAttempt);
                m_ToolSystem.activeTool = m_PreviousTool != null ? m_PreviousTool : m_DefaultToolSystem;
            }

            m_PreviousTool = null;
        }
    }
}
