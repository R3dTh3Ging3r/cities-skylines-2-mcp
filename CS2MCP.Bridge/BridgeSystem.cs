using System;
using System.Diagnostics;
using System.Threading;
using CS2MCP.Core;
using Colossal.Serialization.Entities;
using Game;
using Game.Simulation;
using Game.SceneFlow;
using UnityEngine.Scripting;

namespace CS2MCP
{
    /// <summary>
    /// ECS system that owns the localhost bridge server. Requests arrive on
    /// listener threads, get queued here, and are executed in OnUpdate on the
    /// simulation main thread (the only place ECS access is safe).
    /// Registered at SystemUpdatePhase.UIUpdate so it keeps running while paused.
    /// </summary>
    public sealed partial class BridgeSystem : GameSystemBase
    {
        public const int DefaultPort = 8642;

        public static BridgeSystem Instance
        {
            get; private set;
        }

        private HttpBridgeServer m_Server;
        private RequestHandlers m_Handlers;
        private SimulationSystem m_SimulationSystem;
        private uint m_FrameIndexAtLoad;
        private ObservationScheduler m_Scheduler;
        private WorldLoadObserver m_LoadObserver;
        private OperationRegistry m_Operations;
        internal OperationRegistry Operations => Volatile.Read(ref m_Operations);

        /// <summary>
        /// False until the simulation has advanced at least one frame after the
        /// current save finished loading. While false, unlock replay (Locked
        /// component removal, tax parameter unlocks...) may not have run yet,
        /// so lock states read from ECS can be stale.
        /// </summary>
        public bool SimulationHasTickedSinceLoad { get; private set; } = true;

        internal uint CurrentFrame => m_SimulationSystem.frameIndex;

        /// <summary>Frame at which the simulation auto-pauses (0 = disabled).</summary>
        private readonly WorldBoundPause m_AutoPause = new WorldBoundPause(Mod.Worlds);
        public uint AutoPauseTargetFrame => m_AutoPause.TargetFrame;

        public void SetAutoPause(uint targetFrame, WorldRef originalWorld)
        {
            m_AutoPause.Arm(originalWorld, targetFrame);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            m_LoadObserver = new WorldLoadObserver(Mod.Worlds);
            BeginWorldLoad();
            m_Handlers = new RequestHandlers(this);
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_Scheduler = new ObservationScheduler(Mod.Worlds,
                new ObservationCatalog(m_Handlers, () => CurrentFrame, () => DateTime.UtcNow), () => CurrentFrame,
                () => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency, new ObservationWire());
            PublishRuntimeSnapshot();

            int port = DefaultPort;
            string envPort = Environment.GetEnvironmentVariable("CS2MCP_PORT");
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out int parsed) && parsed > 0 && parsed < 65536)
            {
                port = parsed;
            }

            m_Server = new HttpBridgeServer(port, TryEnqueue, m_Handlers.HandleOperationRequest, Mod.Worlds);
            try
            {
                m_Server.Start();
                Mod.Log.Info($"bridge listening on http://127.0.0.1:{port}");
            }
            catch (Exception e)
            {
                Mod.Log.Error(e, $"failed to start bridge server on port {port}");
                m_Server = null;
            }
        }

        private void BeginWorldLoad()
        {
            m_AutoPause.Clear();
            m_LoadObserver.BeginLoad();
            ResetOperationRegistry();
        }

        private void ResetOperationRegistry()
        {
            m_AutoPause.Clear();
            // Completion/lease producers are deliberately unavailable; no descriptor enables dispatch.
            Volatile.Write(ref m_Operations, new OperationRegistry(Mod.Worlds, _ => new BridgeErrorData(
                ErrorCodes.Unsupported, "native operation completion/lease guards are not implemented", httpStatus: 501)));
        }

        [Preserve]
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            // Retained native metadata places this before load cleanup. Runtime timing still needs G1.
            BeginWorldLoad();
            base.OnGamePreload(purpose, mode);
        }

        private bool TryEnqueue(BridgeRequest request)
        {
            if (request.Observations != null)
            {
                var group = m_Scheduler.Admit(request.Observations, bytes => request.Complete(new BridgeResponse { Body = bytes }));
                request.AttachObservation(group.Detach);
                return true;
            }
            EndpointDescriptor endpoint = EndpointCatalog.Resolve(request.Path, out _);
            bool writer = endpoint == null || endpoint.access != "read";
            return m_Scheduler.TryOrdinary(writer, () => DispatchOrdinary(request, writer));
        }

        private void DispatchOrdinary(BridgeRequest request, bool writer)
        {
            BridgeResponse response;
            try
            {
                // Legacy ingress fencing is weaker than caller-observed world/lease authority.
                // O5 still owns all-writer admission; ordinary mutation timeouts remain unknown outcomes.
                if (writer)
                    Mod.Worlds.Validate(request.IngressWorld);
                response = m_Handlers.Handle(request);
            }
            catch (OperationException error)
            {
                var e = error.Error;
                response = BridgeResponse.Error(e.httpStatus ?? 409, e.message, e.code, e.retryable, e.outcome, e.operationId);
            }
            catch (Exception error)
            {
                Mod.Log.Warn($"error handling {request.Method} {request.Path}: {error}");
                response = BridgeResponse.Error(500, $"{error.GetType().Name}: {error.Message}");
            }
            if (response != null)
                request.Complete(response);
        }

        [Preserve]
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_FrameIndexAtLoad = m_SimulationSystem.frameIndex;
            SimulationHasTickedSinceLoad = false;
            WorldRef before = Mod.Worlds.Current;
            m_LoadObserver.LoadingComplete();
            if (!WorldIdentity.Same(before, Mod.Worlds.Current))
                ResetOperationRegistry();
            // Loading-complete occurs before manager ready; readiness waits for a later safe OnUpdate.
            PublishRuntimeSnapshot();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            PublishRuntimeSnapshot();
            RuntimeEvidence runtime = Mod.RuntimeSnapshots.Read(DateTime.UtcNow);
            WorldRef before = Mod.Worlds.Current;
            m_LoadObserver.Observe(runtime.isLoading, runtime.cityLoaded);
            if (!WorldIdentity.Same(before, Mod.Worlds.Current))
                ResetOperationRegistry();
            if (!SimulationHasTickedSinceLoad && m_SimulationSystem.frameIndex != m_FrameIndexAtLoad)
            {
                SimulationHasTickedSinceLoad = true;
            }
            if (!Mod.Worlds.IsReady)
                m_AutoPause.Clear();
            if (m_AutoPause.Poll(m_SimulationSystem.frameIndex, () => m_SimulationSystem.selectedSpeed = 0f, out var pauseError))
                Mod.Log.Info("timed run reached its target, simulation pause requested");
            else if (pauseError != null)
                Mod.Log.Warn($"timed-run pause was not completed: {pauseError.message}");
            m_Scheduler.Pump();
        }

        public void StopServer()
        {
            m_AutoPause.Clear();
            m_Scheduler?.DetachAll();
            m_Server?.Stop();
            m_Server = null;
        }

        private void PublishRuntimeSnapshot()
        {
            try
            {
                GameManager manager = GameManager.instance;
                if (manager == null)
                    Mod.RuntimeSnapshots.Publish(DateTime.UtcNow, null, null, null, "gameManagerUnavailable");
                else
                {
                    bool loading = manager.isGameLoading;
                    Mod.RuntimeSnapshots.Publish(DateTime.UtcNow, manager.gameMode.ToString(), loading,
                        !loading && manager.gameMode == GameMode.Game, null);
                }
            }
            catch (Exception)
            {
                Mod.RuntimeSnapshots.Publish(DateTime.UtcNow, null, null, null, "nativeObservationUnavailable");
            }
        }

        [Preserve]
        protected override void OnDestroy()
        {
            m_AutoPause.Clear();
            Mod.Worlds.MarkUnavailable();
            StopServer();
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
