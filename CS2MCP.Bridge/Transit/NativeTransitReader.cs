using CS2MCP.Core;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;

namespace CS2MCP
{
    /// <summary>Exact-root game-thread reads. No query, helper aggregation, recursion or native writes.</summary>
    internal sealed class NativeTransitReader : ITransitSource
    {
        readonly EntityManager manager;
        readonly PrefabSystem prefabs;
        WorldRef world;
        DynamicBuffer<RouteWaypoint> waypoints;
        DynamicBuffer<RouteSegment> segments;
        internal NativeTransitReader(EntityManager manager, PrefabSystem prefabs)
        {
            this.manager = manager;
            this.prefabs = prefabs;
        }

        static Entity Entity(EntityRef reference) => new Entity
        {
            Index = reference.index,
            Version = reference.version
        };
        EntityRef Ref(Entity entity) => entity == Unity.Entities.Entity.Null ? null : new EntityRef
        {
            world = world,
            index = entity.Index,
            version = entity.Version
        };
        static Point3 Point(float3 point) => new Point3(point.x, point.y, point.z);
        string State(Entity entity)
        {
            if (entity == Unity.Entities.Entity.Null)
                return "nullRef";
            if (!manager.Exists(entity))
                return "missing";
            if (manager.HasComponent<Deleted>(entity))
                return "deleted";
            if (manager.HasComponent<Temp>(entity))
                return "temporary";
            return "available";
        }

        Entity Prefab(Entity entity)
        {
            if (!manager.HasComponent<PrefabRef>(entity))
                return Unity.Entities.Entity.Null;
            Entity prefab = manager.GetComponentData<PrefabRef>(entity).m_Prefab;
            return State(prefab) == "available" ? prefab : Unity.Entities.Entity.Null;
        }

        static string Mode(TransportType type)
        {
            switch (type)
            {
                case TransportType.Bus:
                    return "bus";
                case TransportType.Tram:
                    return "tram";
                case TransportType.Subway:
                    return "metro";
                case TransportType.Train:
                    return "train";
                case TransportType.Airplane:
                    return "airplane";
                default:
                    return null;
            }
        }

        public TransitRootFacts ReadRoot(TransitQuery query)
        {
            world = query.World;
            Entity line = Entity(query.Line);
            var facts = new TransitRootFacts
            {
                state = State(line)
            };
            if (facts.state != "available")
                return facts;
            if (!manager.HasComponent<Route>(line) || !manager.HasComponent<TransportLine>(line))
            {
                facts.state = "wrongKind";
                return facts;
            }

            // These are lengths only. The owning pure reader authorizes the combined cap
            // before it invokes either indexed element callback below.
            if (manager.HasBuffer<RouteWaypoint>(line))
            {
                waypoints = manager.GetBuffer<RouteWaypoint>(line, isReadOnly: true);
                facts.waypointCount = waypoints.Length;
            }

            if (manager.HasBuffer<RouteSegment>(line))
            {
                segments = manager.GetBuffer<RouteSegment>(line, isReadOnly: true);
                facts.segmentCount = segments.Length;
            }

            if (manager.HasComponent<Owner>(line))
                facts.owner = Ref(manager.GetComponentData<Owner>(line).m_Owner);
            if (manager.HasComponent<RouteNumber>(line))
                facts.routeNumber = manager.GetComponentData<RouteNumber>(line).m_Number;
            Entity prefab = Prefab(line);
            if (prefab != Unity.Entities.Entity.Null)
            {
                var asset = prefabs.GetPrefab<PrefabBase>(prefab);
                facts.assetKey = asset != null ? asset.name : null;
                if (manager.HasComponent<TransportLineData>(prefab))
                {
                    var data = manager.GetComponentData<TransportLineData>(prefab);
                    facts.mode = Mode(data.m_TransportType);
                    facts.passenger = data.m_PassengerTransport;
                    facts.cargo = data.m_CargoTransport;
                }
            }

            return facts;
        }

        public TransitWaypoint ReadWaypoint(int index)
        {
            Entity waypoint = waypoints[index].m_Waypoint;
            var facts = new TransitWaypoint
            {
                entity = Ref(waypoint),
                state = State(waypoint),
                connectionState = "connectionUnavailable"
            };
            if (facts.state != "available")
                return facts;
            if (manager.HasComponent<Game.Routes.Position>(waypoint))
                facts.position = Point(manager.GetComponentData<Game.Routes.Position>(waypoint).m_Position);
            if (manager.HasComponent<WaitingPassengers>(waypoint))
                facts.waitingPassengers = manager.GetComponentData<WaitingPassengers>(waypoint).m_Count;
            if (manager.HasComponent<Connected>(waypoint))
            {
                Entity stop = manager.GetComponentData<Connected>(waypoint).m_Connected;
                facts.connection = Ref(stop);
                facts.connectionState = State(stop);
                if (facts.connectionState == "available")
                {
                    if (manager.HasComponent<TransportStop>(stop))
                        facts.stop = ReadStop(stop);
                    else
                        facts.connectionState = "notTransportStop";
                }
            }

            return facts;
        }

        TransitStopFacts ReadStop(Entity stop)
        {
            var facts = new TransitStopFacts
            {
                entity = Ref(stop),
                ownerReason = "ownerUnavailable",
                attachedParentReason = "attachedParentUnavailable"
            };
            if (manager.HasComponent<Game.Objects.Transform>(stop))
                facts.position = Point(manager.GetComponentData<Game.Objects.Transform>(stop).m_Position);
            if (manager.HasComponent<Owner>(stop))
            {
                facts.owner = Ref(manager.GetComponentData<Owner>(stop).m_Owner);
                facts.ownerReason = facts.owner == null ? "nullOwner" : null;
            }

            if (manager.HasComponent<Game.Objects.Attached>(stop))
            {
                Entity parent = manager.GetComponentData<Game.Objects.Attached>(stop).m_Parent;
                facts.attachedParent = Ref(parent);
                facts.attachedParentReason = State(parent);
                if (facts.attachedParentReason == "available")
                {
                    facts.isAttachedToEdge = manager.HasComponent<Game.Net.Edge>(parent);
                    facts.attachedParentReason = null;
                }
            }

            facts.active = (manager.GetComponentData<TransportStop>(stop).m_Flags & StopFlags.Active) != 0;
            facts.outsideConnection = manager.HasComponent<Game.Objects.OutsideConnection>(stop);
            Entity prefab = Prefab(stop);
            if (prefab != Unity.Entities.Entity.Null && manager.HasComponent<TransportStopData>(prefab))
            {
                var data = manager.GetComponentData<TransportStopData>(prefab);
                facts.mode = Mode(data.m_TransportType);
                facts.purposeFlags = new TransitPurposeFlags
                {
                    passenger = data.m_PassengerTransport,
                    cargo = data.m_CargoTransport
                };
            }
            else
                facts.purposeFlags = new TransitPurposeFlags();
            return facts;
        }

        public TransitSegment ReadSegment(int index)
        {
            Entity segment = segments[index].m_Segment;
            var facts = new TransitSegment
            {
                entity = Ref(segment),
                state = State(segment)
            };
            if (facts.state == "available" && manager.HasComponent<PathInformation>(segment))
                facts.rawDistance = manager.GetComponentData<PathInformation>(segment).m_Distance;
            return facts;
        }
    }
}
