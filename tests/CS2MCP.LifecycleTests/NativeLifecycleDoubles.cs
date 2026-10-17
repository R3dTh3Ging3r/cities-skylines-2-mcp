// Leaf/native boundary doubles only. Lifecycle methods are exact linked production partials.
using System;
using System.Collections.Generic;
using CS2MCP.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Jobs
{
    public struct JobHandle
    {
        public int Value;
    }
}

namespace UnityEngine
{
    public struct Color32
    {
        public byte r, g, b, a;
    }
}

namespace UnityEngine.Scripting
{
    public sealed class PreserveAttribute : Attribute
    {
    }
}

namespace Unity.Mathematics
{
    public struct float2
    {
        public float x, y;
        public float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct quaternion
    {
        public static quaternion identity => default;
    }
}

namespace Colossal.Mathematics
{
    public struct Bezier4x3
    {
        public float3 a, b, c, d;
    }

    public static class MathUtils
    {
        public static float3 Position(Game.Net.Bezier curve, double t) => curve.a;
    }
}

namespace Game.Common
{
    public enum CreationFlags
    {
        Delete,
        Upgrade
    }
}

namespace Game.Net
{
    public struct CompositionFlags
    {
    }
}

namespace Game.City
{
    public sealed class CityConfigurationSystem
    {
    }
}

namespace Game.Simulation
{
    public sealed class SimulationSystem
    {
    }
}

namespace Game.Pathfind
{
    public sealed class PathDummy
    {
    }
}

namespace Game.Prefabs
{
    public class PrefabBase
    {
        public string name = "controlled";
    }
}

namespace Game.Routes
{
    public sealed class RoutesDummy
    {
    }
}

namespace Game.UI
{
    public sealed class NameSystem
    {
        public int Calls;
        public void SetCustomName(Entity line, string name)
        {
            Calls++;
        }
    }
}

namespace Game.UI.InGame
{
    public sealed class TransportationOverviewUISystem
    {
        public int Calls;
        public void RequestUpdate()
        {
            Calls++;
        }
    }
}

namespace Game
{
    public class TestWorld
    {
        readonly Dictionary<Type, object> systems = new Dictionary<Type, object>();
        public T GetOrCreateSystemManaged<T>()
            where T : new()
        {
            if (!systems.ContainsKey(typeof(T)))
            {
                var value = new T();
                if (value is GameSystemBase system)
                    system.World = this;
                systems[typeof(T)] = value;
            }

            return (T)systems[typeof(T)];
        }

        public T GetExistingSystemManaged<T>()
            where T : class, new() => GetOrCreateSystemManaged<T>();
    }

    public class GameSystemBase
    {
        public TestWorld World = new TestWorld();
        protected virtual void OnCreate()
        {
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnDestroy()
        {
        }
    }
}

namespace Game.Tools
{
    public enum AgeMask
    {
        None
    }

    public enum ApplyMode
    {
        None,
        Clear,
        Apply
    }

    public struct ControlPoint
    {
    }

    public class ToolBaseSystem : Game.GameSystemBase
    {
        public virtual string toolID => "controlled-other";

        public virtual Game.Prefabs.PrefabBase GetPrefab() => null;
        public virtual bool TrySetPrefab(Game.Prefabs.PrefabBase value) => false;
        protected virtual Unity.Jobs.JobHandle OnUpdate(Unity.Jobs.JobHandle value) => value;
        protected virtual void OnStopRunning()
        {
        }
    }

    public sealed class TestToolSystem
    {
        private ToolBaseSystem active;
        public int Reads;
        public ToolBaseSystem activeTool
        {
            get
            {
                Reads++;
                return active;
            }

            set
            {
                active = value;
            }
        }
    }

    public class ObjectToolBaseSystem : ToolBaseSystem
    {
        protected readonly TestToolSystem m_ToolSystem = new TestToolSystem();
        protected readonly ToolBaseSystem m_DefaultToolSystem = new ToolBaseSystem();
        protected readonly EntityManager EntityManager = new EntityManager();
        public ApplyMode applyMode;
        public bool Allow = true;
        public string ThrowAt;
        public readonly List<string> Calls = new List<string>();
        protected bool GetAllowApply()
        {
            Leaf("validate");
            return Allow;
        }

        protected void Leaf(string name)
        {
            Calls.Add(name);
            if (ThrowAt == name)
                throw new InvalidOperationException("controlled-leaf-failure");
        }

        public int ActiveReads => m_ToolSystem.Reads;
        public ToolBaseSystem Active { get => m_ToolSystem.activeTool; set => m_ToolSystem.activeTool = value; }
    }
}

namespace CS2MCP
{
    public sealed partial class BridgeToolSystem : Game.Tools.ObjectToolBaseSystem
    {
        void CreatePlacementDefinitions() => Leaf("definitions");
        void CreateRoadDefinitions() => Leaf("definitions");
        void CreateModifyDefinitions(Game.Common.CreationFlags flags, Game.Net.CompositionFlags upgrades) => Leaf("definitions");
        void CreateNodeUpgradeDefinition() => Leaf("definitions");
        void CreateAreaDefinitions() => Leaf("definitions");
        internal void Step() => OnUpdate(default);
        internal void Stop() => OnStopRunning();
    }

    public sealed partial class BridgeRoadToolSystem : Game.Tools.ObjectToolBaseSystem
    {
        void CreateBuildDefinitions() => Leaf("definitions");
        void CreateReplaceDefinitions() => Leaf("definitions");
        BridgeResponse BuildConnectResponse()
        {
            Leaf("readback");
            return BridgeResponse.Json(new { kind = "road-build" });
        }

        BridgeResponse BuildReplaceResponse()
        {
            Leaf("readback");
            return BridgeResponse.Json(new { kind = "road-replace" });
        }

        List<string> CollectTempErrors()
        {
            Leaf("validationErrors");
            return new List<string>();
        }

        internal void Step() => OnUpdate(default);
        internal void Stop() => OnStopRunning();
    }

    public sealed partial class BridgeTransitToolSystem : Game.Tools.ObjectToolBaseSystem
    {
        internal bool Ready = true, PathUpdate, Committed = true;
        void CreateStopDefinitions() => Leaf("definitions");
        void CreateLineDefinitions()
        {
            Leaf("definitions");
            m_DefinitionRounds++;
        }

        bool HasTempPathUpdates()
        {
            Leaf("pathPoll");
            return PathUpdate;
        }

        bool TryGetReadyTempRoute(out Entity entity)
        {
            Leaf("pathReady");
            entity = new Entity
            {
                Index = 7,
                Version = 1
            };
            return Ready;
        }

        string DescribeLineFailure(Entity entity)
        {
            Leaf("validationErrors");
            return "controlled validation";
        }

        Entity FindTempStop()
        {
            Leaf("findStop");
            return new Entity
            {
                Index = 7,
                Version = 1
            };
        }

        List<string> CollectTempErrors()
        {
            Leaf("validationErrors");
            return new List<string>();
        }

        BridgeResponse BuildStopResponse()
        {
            Leaf("readback");
            return BridgeResponse.Json(new { kind = "stop" });
        }

        Func<string, BridgeResponse> PrepareLineResponse(DeferredWorldWork work)
        {
            Leaf("readback");
            return name =>
            {
                Leaf("responseBuilder");
                return BridgeResponse.Json(new { kind = "line" });
            };
        }

        bool IsCommitted(Entity entity)
        {
            Leaf("committedCheck");
            return Committed;
        }

        internal void Step() => OnUpdate(default);
        internal void Stop() => OnStopRunning();
        internal void Expire()
        {
            m_Deadline = DateTime.MinValue;
        }
    }
}

namespace Unity.Collections
{
    public sealed class LifecycleNamespace
    {
    }
}

namespace CS2MCP
{
    public sealed partial class BridgeSystem
    {
        private readonly RequestHandlers m_Handlers = new RequestHandlers();
        private readonly ObservationScheduler m_Scheduler;
        public BridgeSystem()
        {
            m_Scheduler = new ObservationScheduler(Mod.Worlds, new ObservationCatalog(m_Handlers, () => 42, () => DateTime.UtcNow), () => 42, () => 0, new ObservationWire());
        }

        internal bool EnqueueAndPump(BridgeRequest request)
        {
            bool accepted = TryEnqueue(request);
            m_Scheduler.Pump();
            return accepted;
        }

        internal void Dispatch(BridgeRequest r, bool writer) => DispatchOrdinary(r, writer);
        internal static BridgeSystem Instance = new BridgeSystem();
        internal static bool FrameFault;
        internal long CurrentFrame
        {
            get
            {
                if (FrameFault)
                    throw new Exception("private frame failure");
                return 42;
            }
        }
    }
}

namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        internal static Func<BridgeRequest, BridgeResponse> NativeHandle;
        private BridgeResponse HandleCore(BridgeRequest request) => NativeHandle?.Invoke(request) ?? BridgeResponse.Error(404, "test-native-leaf");
    }
}
