using System;
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
    /// <summary>
    /// Headless placement tool. Activated programmatically for exactly three
    /// tool-update frames per operation:
    ///   1. CreateDefinitions — build definition entities (ported LineTool job),
    ///      applyMode=Clear lets the game generate preview Temp entities.
    ///   2. Apply — if validation passed (GetAllowApply), applyMode=Apply commits
    ///      the Temp entities to permanent ones; otherwise reject.
    ///   3. Finish — restore the previously active tool.
    /// </summary>
    public sealed partial class BridgeToolSystem : ObjectToolBaseSystem
    {
        private CityConfigurationSystem m_CityConfigurationSystem;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
        }

        /// <summary>
        /// Creates a modify definition for the pending target, faithfully
        /// mirroring BulldozeToolSystem.AddEntity (CreationDefinition with
        /// m_Original + flags plus a NetCourse/ObjectDefinition describing the
        /// original). Used for Delete (bulldoze) and Upgrade (road upgrades:
        /// grass/trees/lighting...). The game's generate/apply systems handle
        /// all related cleanup/recreation — nodes, zone blocks, lanes — which a
        /// raw component edit would skip (and skipping corrupts state).
        /// </summary>
        private void CreateModifyDefinitions(CreationFlags flags, CompositionFlags upgrades)
        {
            Entity target = m_PendingTarget;
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            Entity e = commandBuffer.CreateEntity();
            var definition = new CreationDefinition
            {
                m_Original = target,
                m_Flags = flags,
            };
            commandBuffer.AddComponent(e, default(Updated));
            if (upgrades != default(CompositionFlags))
            {
                commandBuffer.AddComponent(e, new Game.Net.Upgraded { m_Flags = upgrades });
            }

            if (EntityManager.HasComponent<Game.Net.Edge>(target))
            {
                Game.Net.Edge edge = EntityManager.GetComponentData<Game.Net.Edge>(target);
                NetCourse course = default;
                course.m_Curve = EntityManager.GetComponentData<Game.Net.Curve>(target).m_Bezier;
                course.m_Length = MathUtils.Length(course.m_Curve);
                course.m_FixedIndex = EntityManager.HasComponent<Game.Net.Fixed>(target)
                    ? EntityManager.GetComponentData<Game.Net.Fixed>(target).m_Index
                    : -1;
                course.m_StartPosition.m_Entity = edge.m_Start;
                course.m_StartPosition.m_Position = course.m_Curve.a;
                course.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(course.m_Curve));
                course.m_StartPosition.m_CourseDelta = 0f;
                course.m_EndPosition.m_Entity = edge.m_End;
                course.m_EndPosition.m_Position = course.m_Curve.d;
                course.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(course.m_Curve));
                course.m_EndPosition.m_CourseDelta = 1f;
                commandBuffer.AddComponent(e, course);
            }
            else if (EntityManager.HasComponent<Transform>(target))
            {
                Transform transform = EntityManager.GetComponentData<Transform>(target);
                var objectDefinition = new ObjectDefinition
                {
                    m_Position = transform.m_Position,
                    m_Rotation = transform.m_Rotation,
                    m_Probability = 100,
                    m_PrefabSubIndex = -1,
                    m_LocalPosition = transform.m_Position,
                    m_LocalRotation = transform.m_Rotation,
                };
                if (EntityManager.HasComponent<Game.Objects.Elevation>(target))
                {
                    Game.Objects.Elevation elevation = EntityManager.GetComponentData<Game.Objects.Elevation>(target);
                    objectDefinition.m_Elevation = elevation.m_Elevation;
                    objectDefinition.m_ParentMesh = Game.Objects.ObjectUtils.GetSubParentMesh(elevation.m_Flags);
                }
                else
                {
                    objectDefinition.m_ParentMesh = -1;
                }
                commandBuffer.AddComponent(e, objectDefinition);
            }
            else if (EntityManager.HasBuffer<Game.Areas.Node>(target))
            {
                DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(target, isReadOnly: true);
                commandBuffer.AddBuffer<Game.Areas.Node>(e).CopyFrom(nodes.AsNativeArray());
            }

            commandBuffer.AddComponent(e, definition);
        }

        /// <summary>
        /// Junction upgrade definition, mirroring NetToolSystem.CreateDefinitionsJob
        /// .CreateUpgrade for a node control point: CreationDefinition on the node
        /// (SubElevation | Align, plus Upgrade | Parent when flags change), the new
        /// Upgraded flags and a zero-length NetCourse at the node.
        /// </summary>
        private void CreateNodeUpgradeDefinition()
        {
            Entity node = m_PendingTarget;
            CompositionFlags oldFlags = EntityManager.HasComponent<Game.Net.Upgraded>(node)
                ? EntityManager.GetComponentData<Game.Net.Upgraded>(node).m_Flags
                : default;
            CompositionFlags newFlags = m_PendingUpgradeFlags;

            var definition = new CreationDefinition
            {
                m_Original = node,
                m_Prefab = EntityManager.GetComponentData<PrefabRef>(node).m_Prefab,
                m_Flags = CreationFlags.SubElevation | CreationFlags.Align,
            };
            if (newFlags != oldFlags)
            {
                definition.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
            }

            float3 position = EntityManager.GetComponentData<Game.Net.Node>(node).m_Position;
            float elevation = EntityManager.HasComponent<Game.Net.Elevation>(node)
                ? math.cmin(EntityManager.GetComponentData<Game.Net.Elevation>(node).m_Elevation)
                : 0f;
            NetCourse course = default;
            course.m_Curve = new Bezier4x3(position, position, position, position);
            course.m_Length = 0f;
            course.m_FixedIndex = -1;
            course.m_StartPosition = NodeCoursePos(node, position, elevation, 0f, CoursePosFlags.IsFirst);
            course.m_EndPosition = NodeCoursePos(node, position, elevation, 1f, CoursePosFlags.IsLast);

            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            Entity e = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(e, definition);
            commandBuffer.AddComponent(e, new Game.Net.Upgraded { m_Flags = newFlags });
            commandBuffer.AddComponent(e, default(Updated));
            commandBuffer.AddComponent(e, course);
        }

        private static CoursePos NodeCoursePos(Entity node, float3 position, float elevation, float delta, CoursePosFlags flags)
        {
            return new CoursePos
            {
                m_Entity = node,
                m_Position = position,
                m_Rotation = NetUtils.GetNodeRotation(float3.zero),
                m_Elevation = new float2(elevation),
                m_CourseDelta = delta,
                m_SplitPosition = 0f,
                m_Flags = flags,
                m_ParentMesh = -1,
            };
        }

        /// <summary>
        /// Creates an area definition (district/surface): CreationDefinition +
        /// polygon node buffer; elevation float.MinValue snaps nodes to terrain
        /// (mirrors the game's area definition flow).
        /// </summary>
        private void CreateAreaDefinitions()
        {
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            Entity e = commandBuffer.CreateEntity();
            Unity.Mathematics.Random random = RandomSeed.Next().GetRandom(0);
            commandBuffer.AddComponent(e, new CreationDefinition
            {
                m_Prefab = m_PendingPrefabEntity,
                m_RandomSeed = random.NextInt(),
            });
            commandBuffer.AddComponent(e, default(Updated));
            DynamicBuffer<Game.Areas.Node> nodes = commandBuffer.AddBuffer<Game.Areas.Node>(e);
            foreach (float3 position in m_PendingAreaNodes)
            {
                nodes.Add(new Game.Areas.Node(position, float.MinValue));
            }
        }

        /// <summary>
        /// Creates a standalone straight-road course definition from the pending
        /// start to end position, terrain-following (mirrors the standalone-net
        /// branch of the game's net definition flow).
        /// </summary>
        private void CreateRoadDefinitions()
        {
            TerrainHeightData terrainHeight = m_TerrainSystem.GetHeightData();

            Curve rawCurve = default;
            if (m_PendingHasMid)
            {
                // Quadratic bezier through the mid control point, elevated to cubic.
                float3 a = m_PendingPosition;
                float3 d = m_PendingEnd;
                float3 m = m_PendingMid;
                rawCurve.m_Bezier = new Bezier4x3(a, a + (m - a) * (2f / 3f), d + (m - d) * (2f / 3f), d);
            }
            else
            {
                rawCurve.m_Bezier = NetUtils.StraightCurve(m_PendingPosition, m_PendingEnd);
            }
            Bezier4x3 adjusted = NetUtils.AdjustPosition(
                rawCurve, fixedStart: false, linearMiddle: false, fixedEnd: false, ref terrainHeight).m_Bezier;

            float e1 = m_PendingElevations.x;
            float e2 = m_PendingElevations.y;
            if (e1 != 0f || e2 != 0f)
            {
                // Lift the terrain-following curve by linearly interpolated
                // elevation; the pipeline turns nonzero course elevations into
                // bridge/elevated segments with pillars.
                adjusted.a.y += e1;
                adjusted.b.y += math.lerp(e1, e2, 1f / 3f);
                adjusted.c.y += math.lerp(e1, e2, 2f / 3f);
                adjusted.d.y += e2;
            }

            NetCourse course = default;
            course.m_Curve = adjusted;
            course.m_Elevation = new float2(math.min(e1, e2), math.max(e1, e2));
            course.m_StartPosition.m_Position = course.m_Curve.a;
            course.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(course.m_Curve));
            course.m_StartPosition.m_CourseDelta = 0f;
            course.m_StartPosition.m_ParentMesh = -1;
            course.m_StartPosition.m_Elevation = e1;
            course.m_StartPosition.m_Flags = CoursePosFlags.IsFirst | CoursePosFlags.FreeHeight;
            course.m_EndPosition.m_Position = course.m_Curve.d;
            course.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(course.m_Curve));
            course.m_EndPosition.m_CourseDelta = 1f;
            course.m_EndPosition.m_ParentMesh = -1;
            course.m_EndPosition.m_Elevation = e2;
            course.m_EndPosition.m_Flags = CoursePosFlags.IsLast | CoursePosFlags.FreeHeight;
            course.m_Length = MathUtils.Length(course.m_Curve);
            course.m_FixedIndex = -1;

            Unity.Mathematics.Random random = RandomSeed.Next().GetRandom(0);
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
            Entity entity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(entity, new CreationDefinition
            {
                m_Prefab = m_PendingPrefabEntity,
                m_RandomSeed = random.NextInt(),
            });
            commandBuffer.AddComponent(entity, default(Updated));
            commandBuffer.AddComponent(entity, course);
        }

        /// <summary>
        /// Fills and synchronously executes the ported LineTool definition job
        /// for a single object at the pending position/rotation.
        /// </summary>
        private void CreatePlacementDefinitions()
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
            definitions.m_ControlPoint = new ControlPoint
            {
                m_Position = m_PendingPosition,
                m_Rotation = m_PendingRotation,
            };
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
