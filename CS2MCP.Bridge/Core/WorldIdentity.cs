using System;
using System.Threading;

namespace CS2MCP.Core
{
    public sealed class WorldSnapshot
    {
        public WorldRef world { get; } public bool ready { get; }
        internal WorldSnapshot(WorldRef world, bool ready) { this.world = world; this.ready = ready; }
    }
    /// <summary>Publish load boundaries on the game thread. Listener reads touch managed immutable state only.</summary>
    public sealed class WorldIdentity
    {
        private sealed class State
        {
            internal readonly WorldRef World; internal readonly bool Ready;
            internal State(WorldRef world, bool ready) { World = world; Ready = ready; }
        }
        private State state;
        private readonly object gate = new object();
        public WorldIdentity(string bootId)
        {
            if (string.IsNullOrWhiteSpace(bootId)) throw new ArgumentException("bootId required", nameof(bootId));
            state = new State(new WorldRef(bootId, 0), false);
        }
        public WorldSnapshot Snapshot { get { State value = Volatile.Read(ref state); return new WorldSnapshot(value.World, value.Ready); } }
        public WorldRef Current => Volatile.Read(ref state).World;
        public bool IsReady => Volatile.Read(ref state).Ready;
        public WorldRef BeginLoad()
        {
            lock (gate) { var next = new WorldRef(state.World.bootId, checked(state.World.epoch + 1));
                Volatile.Write(ref state, new State(next, false)); return next; }
        }
        public void CompleteLoad() { lock (gate) Volatile.Write(ref state, new State(state.World, true)); }
        public void MarkUnavailable() { lock (gate) Volatile.Write(ref state, new State(state.World, false)); }
        public void Validate(WorldRef expected)
        {
            State snapshot = Volatile.Read(ref state);
            if (!Same(snapshot.World, expected)) throw new OperationException(new BridgeErrorData(
                ErrorCodes.WorldMismatch, "operation belongs to a different world", outcome: "notApplied", httpStatus: 409));
            if (!snapshot.Ready) throw new OperationException(new BridgeErrorData(ErrorCodes.NotReady, "world is loading or unavailable", httpStatus: 503));
        }
        public static bool Same(WorldRef a, WorldRef b) => a != null && b != null && a.bootId == b.bootId && a.epoch == b.epoch;
    }
}

namespace CS2MCP.Core
{
    /// <summary>Game-thread policy for observed load sequencing. Completion is a pending signal,
    /// never readiness; subsequent loading=true observations cannot erase it for the same load.
    /// Actual callback order/thread/cardinality remains a native G1 validation requirement.</summary>
    public sealed class WorldLoadObserver
    {
        private readonly WorldIdentity identity;
        private bool inProgress, completed;
        public WorldLoadObserver(WorldIdentity identity) { this.identity = identity ?? throw new System.ArgumentNullException(nameof(identity)); }
        public void BeginLoad() { identity.BeginLoad(); inProgress = true; completed = false; }
        public void LoadingComplete()
        {
            if (!inProgress) BeginLoad();
            completed = true;
            identity.MarkUnavailable();
        }
        public void Observe(bool? loading, bool? cityLoaded)
        {
            if (loading == true && !inProgress) BeginLoad();
            if (loading == false && cityLoaded == true && completed)
            {
                identity.CompleteLoad(); inProgress = false;
            }
            else identity.MarkUnavailable();
        }
    }
}
