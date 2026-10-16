using System;
using System.Collections.Concurrent;
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

        public static BridgeSystem Instance { get; private set; }

        private HttpBridgeServer m_Server;
        private RequestHandlers m_Handlers;
        private SimulationSystem m_SimulationSystem;
        private uint m_FrameIndexAtLoad;
        private int m_PendingCount;
        private WorldLoadObserver m_LoadObserver;
        private OperationRegistry m_Operations;
        internal OperationRegistry Operations => Volatile.Read(ref m_Operations);
        private readonly ConcurrentQueue<BridgeRequest> m_Pending = new ConcurrentQueue<BridgeRequest>();

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
            int count;
            do { count = Volatile.Read(ref m_PendingCount); if (count >= OperationRegistry.PendingCapacity) return false; }
            while (Interlocked.CompareExchange(ref m_PendingCount, count + 1, count) != count);
            m_Pending.Enqueue(request); return true;
        }

        [Preserve]
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_FrameIndexAtLoad = m_SimulationSystem.frameIndex;
            SimulationHasTickedSinceLoad = false;
            WorldRef before = Mod.Worlds.Current;
            m_LoadObserver.LoadingComplete();
            if (!WorldIdentity.Same(before, Mod.Worlds.Current)) ResetOperationRegistry();
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
            if (!WorldIdentity.Same(before, Mod.Worlds.Current)) ResetOperationRegistry();
            if (!SimulationHasTickedSinceLoad && m_SimulationSystem.frameIndex != m_FrameIndexAtLoad)
            {
                SimulationHasTickedSinceLoad = true;
            }
            if (!Mod.Worlds.IsReady) m_AutoPause.Clear();
            if (m_AutoPause.Poll(m_SimulationSystem.frameIndex, () => m_SimulationSystem.selectedSpeed = 0f, out var pauseError))
                Mod.Log.Info("timed run reached its target, simulation pause requested");
            else if (pauseError != null) Mod.Log.Warn($"timed-run pause was not completed: {pauseError.message}");
            var readBudget = Stopwatch.StartNew();
            int reads = 0; bool writerStarted = false;
            while (reads < 8 && readBudget.ElapsedMilliseconds < 2 && m_Pending.TryPeek(out BridgeRequest next))
            {
                EndpointDescriptor endpoint = EndpointCatalog.Resolve(next.Path, out _);
                bool writer = endpoint == null || endpoint.access != "read";
                if (writer && writerStarted) break;
                if (!m_Pending.TryDequeue(out BridgeRequest request)) continue;
                Interlocked.Decrement(ref m_PendingCount);
                if (writer) writerStarted = true; else reads++;
                BridgeResponse response;
                try
                {
                    // A null response means the handler completes the request
                    // asynchronously itself (e.g. screenshots at end-of-frame).
                    // Legacy ingress fencing is weaker than a caller-observed WorldRef and does not
                    // fence already deferred legacy callbacks. All-writer lifecycle migration belongs to O5.
                    if (writer) Mod.Worlds.Validate(request.IngressWorld);
                    response = m_Handlers.Handle(request);
                }
                catch (OperationException e)
                {
                    response = BridgeResponse.Error(e.Error.httpStatus ?? 409, e.Error.message, e.Error.code,
                        e.Error.retryable, e.Error.outcome, e.Error.operationId);
                }
                catch (Exception e)
                {
                    Mod.Log.Warn($"error handling {request.Method} {request.Path}: {e}");
                    response = BridgeResponse.Error(500, $"{e.GetType().Name}: {e.Message}");
                }
                if (response != null)
                {
                    request.Complete(response);
                }
            }
        }

        public void StopServer()
        {
            m_AutoPause.Clear();
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
