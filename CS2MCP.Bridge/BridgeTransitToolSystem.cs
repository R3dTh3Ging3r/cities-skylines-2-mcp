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
    /// <summary>
    /// Headless public-transport tool, separate from BridgeToolSystem so the
    /// existing construction code stays untouched. It drives the same native
    /// pipelines the vanilla tools use:
    ///   Stop: ported object definitions with a road-edge control point (as
    ///         ObjectToolSystem produces for net objects); the game attaches
    ///         and snaps the stop, validates it and applies it.
    ///   Line: CreationDefinition + WaypointDefinition buffer, exactly like
    ///         RouteToolSystem. Segment paths are computed asynchronously, so
    ///         the tool waits for them, re-emitting the definitions whenever a
    ///         path result arrives (as the vanilla tool does) so validation
    ///         re-runs with real path data, before checking GetAllowApply.
    /// </summary>
    public sealed partial class BridgeTransitToolSystem : ObjectToolBaseSystem
    {
        private CityConfigurationSystem m_CityConfigurationSystem;
        private NameSystem m_NameSystem;
        private EntityQuery m_TempRouteQuery;
        private EntityQuery m_PathUpdatedQuery;
        private EntityQuery m_TempStopQuery;
        private EntityQuery m_IconQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
            m_TempRouteQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Route>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<RouteWaypoint>(),
                    ComponentType.ReadOnly<RouteSegment>(),
                },
                None = new[] { ComponentType.ReadOnly<Deleted>() },
            });
            m_PathUpdatedQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Game.Common.Event>(),
                ComponentType.ReadOnly<PathUpdated>());
            m_TempStopQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Game.Routes.TransportStop>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Transform>(),
                },
                None = new[] { ComponentType.ReadOnly<Deleted>() },
            });
            m_IconQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Game.Notifications.Icon>(),
                ComponentType.ReadOnly<Owner>(),
                ComponentType.ReadOnly<PrefabRef>());
        }

        private BridgeResponse BuildStopResponse()
        {
            Entity stop = m_ResultEntity;
            if (!IsCommitted(stop))
            {
                return BridgeResponse.Error(409, "the game did not commit the stop (it may have been rejected while applying)");
            }
            Transform transform = EntityManager.GetComponentData<Transform>(stop);
            object road = null;
            if (EntityManager.HasComponent<Game.Objects.Attached>(stop))
            {
                Entity parent = EntityManager.GetComponentData<Game.Objects.Attached>(stop).m_Parent;
                if (parent != Entity.Null)
                {
                    road = new { index = parent.Index, version = parent.Version };
                }
            }
            string prefabName = null;
            if (EntityManager.HasComponent<PrefabRef>(stop))
            {
                PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(EntityManager.GetComponentData<PrefabRef>(stop).m_Prefab);
                prefabName = prefab != null ? prefab.name : null;
            }
            return BridgeResponse.Json(new
            {
                placed = true,
                type = m_PendingTypeName,
                prefab = prefabName,
                stop = new { index = stop.Index, version = stop.Version },
                name = SafeLabel(stop),
                position = new { x = transform.m_Position.x, y = transform.m_Position.y, z = transform.m_Position.z },
                road,
                note = "placed through the game's object tool pipeline (attached, snapped and validated by the game); " +
                       "the road segment is regenerated to show the stop, so its entity id may change. " +
                       "Use the stop id with cs2_create_transit_line.",
            });
        }

        /// <summary>
        /// Captures the committed line's reply data now (Deactivate clears the
        /// tool state). The returned builder reads the line's name when invoked,
        /// so a rename done later in the UI phase is reflected.
        /// </summary>
        private Func<string, BridgeResponse> PrepareLineResponse(DeferredWorldWork work)
        {
            work.ThrowIfInvalid();
            Entity line = m_ResultEntity;
            if (!IsCommitted(line))
            {
                BridgeResponse error = BridgeResponse.Error(409, "the game did not commit the line (it may have been rejected while applying)");
                return _ => error;
            }
            int number = EntityManager.HasComponent<RouteNumber>(line)
                ? EntityManager.GetComponentData<RouteNumber>(line).m_Number
                : 0;
            string color = EntityManager.HasComponent<Game.Routes.Color>(line)
                ? ToHex(EntityManager.GetComponentData<Game.Routes.Color>(line).m_Color)
                : null;
            var stops = new List<object>();
            for (int i = 0; i < m_LineStops.Length; i++)
            {
                Entity stop = m_LineStops[i].Stop;
                stops.Add(new { order = i + 1, stop = new { index = stop.Index, version = stop.Version }, name = ResponseLabel(stop, work) });
            }
            string type = m_PendingTypeName;
            string prefab = m_PendingPrefab != null ? m_PendingPrefab.name : null;
            int depots = m_DepotCount;
            return nameWarning => {
                // The response closure is deferred too; never read an old line through the new world's NameSystem.
                work.ThrowIfInvalid();
                return BridgeResponse.Json(new
            {
                created = true,
                type,
                prefab,
                line = new { index = line.Index, version = line.Version },
                number = number > 0 ? (int?)number : null,
                name = ResponseLabel(line, work),
                color,
                stopCount = stops.Count,
                stops,
                depotsOfThisType = depots,
                warning = depots == 0
                    ? $"no {type} depot exists: the line is valid but gets no vehicles until one is built"
                    : null,
                nameWarning,
                note = "created through the game's route pipeline (definitions, pathfinding, validation, apply). " +
                       "Vehicles are dispatched only while the simulation runs. Verify with cs2_list_transit_lines.",
            });
            };
        }

        private string ResponseLabel(Entity entity, DeferredWorldWork work)
        {
            work.ThrowIfInvalid();
            // Unlike optional labels on unrelated legacy paths, a failed delayed read must reach TryRun.
            string label = m_NameSystem.GetRenderedLabelName(entity);
            work.ThrowIfInvalid();
            return label;
        }

        private bool IsCommitted(Entity entity)
        {
            return entity != Entity.Null
                && EntityManager.Exists(entity)
                && !EntityManager.HasComponent<Temp>(entity)
                && !EntityManager.HasComponent<Deleted>(entity);
        }

        private string SafeLabel(Entity entity)
        {
            try
            {
                return m_NameSystem.GetRenderedLabelName(entity);
            }
            catch
            {
                return null;
            }
        }

        internal static string ToHex(UnityEngine.Color32 color)
        {
            return $"#{color.r:X2}{color.g:X2}{color.b:X2}";
        }

        /// <summary>
        /// Mirrors RouteToolSystem.CreateDefinitionsJob for a new route: one
        /// CreationDefinition with a WaypointDefinition per stop (connection =
        /// stop entity), closed by repeating the first waypoint at the exact
        /// same position, which is what makes GenerateRoutesSystem flag the
        /// route Complete.
        /// </summary>
        private void CreateLineDefinitions()
        {
            m_DefinitionRounds++;
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            Entity entity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(entity, new CreationDefinition { m_Prefab = m_PendingPrefabEntity });
            if (m_HasLineColor)
            {
                commandBuffer.AddComponent(entity, new ColorDefinition { m_Color = m_LineColor });
            }
            DynamicBuffer<WaypointDefinition> waypoints = commandBuffer.AddBuffer<WaypointDefinition>(entity);
            for (int i = 0; i <= m_LineStops.Length; i++)
            {
                LineStop stop = m_LineStops[i % m_LineStops.Length];
                waypoints.Add(new WaypointDefinition(stop.Position) { m_Connection = stop.Stop });
            }
            commandBuffer.AddComponent(entity, default(Updated));
        }

        /// <summary>
        /// Mirrors RouteToolSystem.CheckPathUpdates: true when a path result
        /// arrived for a temp entity this frame.
        /// </summary>
        private bool HasTempPathUpdates()
        {
            if (m_PathUpdatedQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }
            using (NativeArray<PathUpdated> events = m_PathUpdatedQuery.ToComponentDataArray<PathUpdated>(Allocator.Temp))
            {
                for (int i = 0; i < events.Length; i++)
                {
                    Entity owner = events[i].m_Owner;
                    if (EntityManager.Exists(owner) && EntityManager.HasComponent<Temp>(owner))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Mirrors RouteToolSystem.GetPathfindCompleted (every segment's ready
        /// positions match its waypoints), plus a guard that each segment's
        /// path information has actually been filled in.
        /// </summary>
        private bool TryGetReadyTempRoute(out Entity tempRoute)
        {
            tempRoute = Entity.Null;
            using (NativeArray<Entity> routes = m_TempRouteQuery.ToEntityArray(Allocator.Temp))
            {
                if (routes.Length == 0)
                {
                    return false;
                }
                for (int r = 0; r < routes.Length; r++)
                {
                    DynamicBuffer<RouteWaypoint> waypoints = EntityManager.GetBuffer<RouteWaypoint>(routes[r], isReadOnly: true);
                    DynamicBuffer<RouteSegment> segments = EntityManager.GetBuffer<RouteSegment>(routes[r], isReadOnly: true);
                    if (segments.Length == 0 || waypoints.Length == 0)
                    {
                        return false;
                    }
                    for (int i = 0; i < segments.Length; i++)
                    {
                        Entity segment = segments[i].m_Segment;
                        if (!EntityManager.HasComponent<PathTargets>(segment))
                        {
                            continue;
                        }
                        PathTargets targets = EntityManager.GetComponentData<PathTargets>(segment);
                        Entity from = waypoints[i].m_Waypoint;
                        Entity to = waypoints[i + 1 >= waypoints.Length ? 0 : i + 1].m_Waypoint;
                        if (EntityManager.HasComponent<Game.Routes.Position>(from)
                            && !(math.distancesq(targets.m_ReadyStartPosition, EntityManager.GetComponentData<Game.Routes.Position>(from).m_Position) < 1f))
                        {
                            return false;
                        }
                        if (EntityManager.HasComponent<Game.Routes.Position>(to)
                            && !(math.distancesq(targets.m_ReadyEndPosition, EntityManager.GetComponentData<Game.Routes.Position>(to).m_Position) < 1f))
                        {
                            return false;
                        }
                        if (EntityManager.HasComponent<PathInformation>(segment))
                        {
                            PathInformation info = EntityManager.GetComponentData<PathInformation>(segment);
                            bool hasElements = EntityManager.HasBuffer<PathElement>(segment)
                                && EntityManager.GetBuffer<PathElement>(segment, isReadOnly: true).Length > 0;
                            if (info.m_Distance == 0f && !hasElements)
                            {
                                return false;
                            }
                        }
                    }
                }
                tempRoute = routes[0];
            }
            return true;
        }

        private string DescribeLineFailure(Entity tempRoute)
        {
            var problems = new List<string>();
            if (EntityManager.HasBuffer<RouteSegment>(tempRoute))
            {
                DynamicBuffer<RouteSegment> segments = EntityManager.GetBuffer<RouteSegment>(tempRoute, isReadOnly: true);
                for (int i = 0; i < segments.Length; i++)
                {
                    Entity segment = segments[i].m_Segment;
                    if (EntityManager.HasComponent<PathInformation>(segment)
                        && EntityManager.GetComponentData<PathInformation>(segment).m_Distance < 0f)
                    {
                        int to = m_LineStops != null && m_LineStops.Length > 0 ? (i + 1) % m_LineStops.Length : i + 1;
                        problems.Add($"no {m_PendingTypeName} path from stop #{i + 1} to stop #{to + 1}");
                    }
                }
            }
            foreach (string error in CollectTempErrors())
            {
                problems.Add(error);
            }
            string detail = problems.Count > 0 ? string.Join("; ", problems) : "stops unreachable or wrong stop type";
            return $"line blocked by game validation: {detail}. Nothing was created. " +
                   "Stops must be connected by roads/tracks usable by this transport type.";
        }

        /// <summary>
        /// Names of the validation errors the game raised on temp entities
        /// (it shows them as error icons whose prefab carries ToolErrorData).
        /// </summary>
        private List<string> CollectTempErrors()
        {
            var names = new List<string>();
            using (NativeArray<Entity> icons = m_IconQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    Entity owner = EntityManager.GetComponentData<Owner>(icons[i]).m_Owner;
                    if (!EntityManager.Exists(owner) || !EntityManager.HasComponent<Temp>(owner))
                    {
                        continue;
                    }
                    Entity prefab = EntityManager.GetComponentData<PrefabRef>(icons[i]).m_Prefab;
                    if (!EntityManager.HasComponent<ToolErrorData>(prefab))
                    {
                        continue;
                    }
                    string name = EntityManager.GetComponentData<ToolErrorData>(prefab).m_Error.ToString();
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
            }
            return names;
        }

        private Entity FindTempStop()
        {
            Entity best = Entity.Null;
            float bestDistance = 30f;
            using (NativeArray<Entity> stops = m_TempStopQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < stops.Length; i++)
                {
                    if (EntityManager.GetComponentData<Temp>(stops[i]).m_Original != Entity.Null)
                    {
                        continue;
                    }
                    float distance = math.distance(
                        EntityManager.GetComponentData<Transform>(stops[i]).m_Position.xz,
                        m_StopControlPoint.m_Position.xz);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = stops[i];
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// Same as BridgeToolSystem's placement definitions, but with a control
        /// point that names the road edge, which makes the ported
        /// CreateDefinitions attach the net object (stop) to that road.
        /// </summary>
        private void CreateStopDefinitions()
        {
            CreateDefinitions definitions = default;
            definitions.m_RandomizationEnabled = false;
            definitions.m_FixedRandomSeed = 0;
            definitions.m_EditorMode = m_ToolSystem.actionMode.IsEditor();
            definitions.m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic;
            definitions.m_ObjectPrefab = m_PendingPrefabEntity;
            definitions.m_Theme = m_CityConfigurationSystem.defaultTheme;
            definitions.m_RandomSeed = RandomSeed.Next();
            definitions.m_AgeMask = AgeMask.Mature;
            definitions.m_ControlPoint = m_StopControlPoint;
            definitions.m_AttachmentPrefab = default;
            definitions.m_OwnerData = GetComponentLookup<Owner>(true);
            definitions.m_TransformData = GetComponentLookup<Transform>(true);
            definitions.m_AttachedData = GetComponentLookup<Game.Objects.Attached>(true);
            definitions.m_LocalTransformCacheData = GetComponentLookup<LocalTransformCache>(true);
            definitions.m_ElevationData = GetComponentLookup<Game.Objects.Elevation>(true);
            definitions.m_BuildingData = GetComponentLookup<Game.Buildings.Building>(true);
            definitions.m_LotData = GetComponentLookup<Game.Buildings.Lot>(true);
            definitions.m_EdgeData = GetComponentLookup<Game.Net.Edge>(true);
            definitions.m_NodeData = GetComponentLookup<Game.Net.Node>(true);
            definitions.m_CurveData = GetComponentLookup<Game.Net.Curve>(true);
            definitions.m_NetElevationData = GetComponentLookup<Game.Net.Elevation>(true);
            definitions.m_OrphanData = GetComponentLookup<Game.Net.Orphan>(true);
            definitions.m_UpgradedData = GetComponentLookup<Game.Net.Upgraded>(true);
            definitions.m_CompositionData = GetComponentLookup<Game.Net.Composition>(true);
            definitions.m_AreaClearData = GetComponentLookup<Game.Areas.Clear>(true);
            definitions.m_AreaSpaceData = GetComponentLookup<Game.Areas.Space>(true);
            definitions.m_AreaLotData = GetComponentLookup<Game.Areas.Lot>(true);
            definitions.m_EditorContainerData = GetComponentLookup<Game.Tools.EditorContainer>(true);
            definitions.m_PrefabRefData = GetComponentLookup<PrefabRef>(true);
            definitions.m_PrefabNetObjectData = GetComponentLookup<NetObjectData>(true);
            definitions.m_PrefabBuildingData = GetComponentLookup<BuildingData>(true);
            definitions.m_PrefabAssetStampData = GetComponentLookup<AssetStampData>(true);
            definitions.m_PrefabBuildingExtensionData = GetComponentLookup<BuildingExtensionData>(true);
            definitions.m_PrefabSpawnableObjectData = GetComponentLookup<SpawnableObjectData>(true);
            definitions.m_PrefabObjectGeometryData = GetComponentLookup<ObjectGeometryData>(true);
            definitions.m_PrefabPlaceableObjectData = GetComponentLookup<PlaceableObjectData>(true);
            definitions.m_PrefabAreaGeometryData = GetComponentLookup<AreaGeometryData>(true);
            definitions.m_PrefabBuildingTerraformData = GetComponentLookup<BuildingTerraformData>(true);
            definitions.m_PrefabCreatureSpawnData = GetComponentLookup<CreatureSpawnData>(true);
            definitions.m_PlaceholderBuildingData = GetComponentLookup<PlaceholderBuildingData>(true);
            definitions.m_PrefabNetGeometryData = GetComponentLookup<NetGeometryData>(true);
            definitions.m_PrefabCompositionData = GetComponentLookup<NetCompositionData>(true);
            definitions.m_SubObjects = GetBufferLookup<Game.Objects.SubObject>(true);
            definitions.m_CachedNodes = GetBufferLookup<LocalNodeCache>(true);
            definitions.m_InstalledUpgrades = GetBufferLookup<Game.Buildings.InstalledUpgrade>(true);
            definitions.m_SubNets = GetBufferLookup<Game.Net.SubNet>(true);
            definitions.m_ConnectedEdges = GetBufferLookup<Game.Net.ConnectedEdge>(true);
            definitions.m_SubAreas = GetBufferLookup<Game.Areas.SubArea>(true);
            definitions.m_AreaNodes = GetBufferLookup<Game.Areas.Node>(true);
            definitions.m_AreaTriangles = GetBufferLookup<Game.Areas.Triangle>(true);
            definitions.m_PrefabSubObjects = GetBufferLookup<Game.Prefabs.SubObject>(true);
            definitions.m_PrefabSubNets = GetBufferLookup<Game.Prefabs.SubNet>(true);
            definitions.m_PrefabSubLanes = GetBufferLookup<Game.Prefabs.SubLane>(true);
            definitions.m_PrefabSubAreas = GetBufferLookup<Game.Prefabs.SubArea>(true);
            definitions.m_PrefabSubAreaNodes = GetBufferLookup<SubAreaNode>(true);
            definitions.m_PrefabPlaceholderElements = GetBufferLookup<PlaceholderObjectElement>(true);
            definitions.m_PrefabRequirementElements = GetBufferLookup<ObjectRequirementElement>(true);
            definitions.m_PrefabServiceUpgradeBuilding = GetBufferLookup<ServiceUpgradeBuilding>(true);
            definitions.m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out _);
            definitions.m_TerrainHeightData = m_TerrainSystem.GetHeightData();
            definitions.m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            definitions.Execute();
        }
    }
}
