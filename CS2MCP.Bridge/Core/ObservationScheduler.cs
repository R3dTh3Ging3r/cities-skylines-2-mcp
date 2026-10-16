using System;
using System.Collections.Generic;
using System.Linq;
namespace CS2MCP.Core
{
    public interface IObservationEncoder
    {
        byte[] Item(ObservationItemResult item); byte[] Batch(ObservationBatchResult batch);
    }
    public sealed class ObservationGroup
    {
        internal readonly ObservationScheduler Owner; internal readonly ObservationRead[] Items; internal readonly Action<byte[]> Complete;
        internal int Next, Bytes; internal bool Detached, Finished; public ObservationBatchResult Result
        {
            get;
        }
        internal ObservationGroup(ObservationScheduler owner, ObservationRead[] items, WorldRef world, Action<byte[]> complete)
        {
            Owner = owner;
            Items = items;
            Complete = complete;
            Result = new ObservationBatchResult { world = world, results = new ObservationItemResult[items.Length] };
        }
        public void Detach() => Owner.Detach(this);
    }
    /// <summary>One bounded admission/dispatch budget for ordinary requests and read groups. Native callbacks run only in Pump, outside the admission lock.</summary>
    public sealed class ObservationScheduler
    {
        public const int PendingCapacity = 256, GroupCapacity = 8, MaxItemBytes = 262144, MaxGroupBytes = 4194304;
        public const int MaxReadsPerUpdate = 8;
        public const double MaxAdditionalMilliseconds = 2;
        // Reserves UTF8 space for every future error (128-char ID + fixed bounded error) and group envelope.
        const int ErrorReserve = 4096, EnvelopeReserve = 4096;
        readonly object gate = new object(); readonly Queue<Ordinary> ordinary = new Queue<Ordinary>(); readonly List<ObservationGroup> groups = new List<ObservationGroup>();
        readonly WorldIdentity worlds; readonly ObservationCatalog catalog; readonly Func<long> frame; readonly Func<double> millis; readonly IObservationEncoder encoder;
        int pending, cursor;
        sealed class Ordinary
        {
            internal bool Writer; internal Action Run;
        }
        public ObservationScheduler(WorldIdentity worlds, ObservationCatalog catalog, Func<long> frame, Func<double> millis, IObservationEncoder encoder)
        {
            this.worlds = worlds;
            this.catalog = catalog;
            this.frame = frame;
            this.millis = millis;
            this.encoder = encoder;
        }
        public int Pending
        {
            get
            {
                lock (gate)
                    return pending;
            }
        }
        public int ActiveGroups
        {
            get
            {
                lock (gate)
                    return groups.Count;
            }
        }
        public bool TryOrdinary(bool writer, Action run)
        {
            lock (gate)
            {
                if (pending >= PendingCapacity)
                    return false;
                ordinary.Enqueue(new Ordinary { Writer = writer, Run = run });
                pending++;
                return true;
            }
        }
        public ObservationGroup Admit(ValidatedObservationBatch request, Action<byte[]> complete)
        {
            // The request owns new immutable strings/private argument copies from strict parsing, not caller containers.
            var snapshot = worlds.Snapshot;
            if (!snapshot.ready)
                throw Failure(ErrorCodes.NotReady, "observation world is not ready", 503);
            if (request.ExpectedWorld != null && !WorldIdentity.Same(request.ExpectedWorld, snapshot.world))
                throw Failure(ErrorCodes.WorldMismatch, "observation expected a different world", 409);
            worlds.Validate(snapshot.world);
            foreach (var item in request.Items)
                if (item.InventoryWorld != null && !WorldIdentity.Same(item.InventoryWorld, snapshot.world))
                    throw Failure(ErrorCodes.WorldMismatch, "inventory belongs to a different world", 409);
            lock (gate)
            {
                worlds.Validate(snapshot.world);
                if (groups.Count >= GroupCapacity || pending + request.Items.Length > PendingCapacity)
                    throw Failure(ErrorCodes.BridgeBusy, "observation capacity unavailable", 409);
                var group = new ObservationGroup(this, (ObservationRead[])request.Items.Clone(), snapshot.world, complete);
                // This transport slot remains held after completion until HTTP write/finally detach,
                // bounding completed-but-unsent response ownership as well as in-progress groups.
                groups.Add(group);
                pending += request.Items.Length;
                return group;
            }
        }
        static OperationException Failure(string code, string message, int status) => new OperationException(new BridgeErrorData(code, message, httpStatus: status));
        static ObservationItemResult Error(string id, string code, string message, int status = 409) => new ObservationItemResult { id = id, error = new BridgeErrorData(code, message, httpStatus: status) };
        internal void Detach(ObservationGroup group)
        {
            lock (gate)
            {
                if (group.Detached)
                    return;
                group.Detached = true;
                pending -= group.Items.Length - group.Next;
                group.Next = group.Items.Length;
                for (int i = 0; i < group.Items.Length; i++)
                    if (group.Result.results[i] == null)
                        group.Result.results[i] = Error(group.Items[i].Id, "READ_CANCELLED", "observation transport detached");
                // Remove the scheduler reference: no durable result cache or waiter list survives transport detach.
                groups.Remove(group);
                group.Finished = true;
            }
        }
        public void DetachAll()
        {
            lock (gate)
            {
                foreach (var group in groups.ToArray())
                    Detach(group);
            }
        }
        // Called only by BridgeSystem.OnUpdate. A current native call/encoding cannot be preempted;
        // the global clock is rechecked before selecting any additional work.
        public void Pump()
        {
            double started = millis();
            int reads = 0;
            bool wrote = false;
            while (millis() - started < MaxAdditionalMilliseconds)
            {
                Ordinary work = null;
                ObservationGroup selected = null;
                int index = -1;
                lock (gate)
                {
                    int lanes = groups.Count + 1;
                    for (int attempt = 0; attempt < lanes; attempt++)
                    {
                        int lane = cursor % lanes;
                        cursor = (lane + 1) % lanes;
                        if (lane == 0)
                        {
                            if (ordinary.Count == 0)
                                continue;
                            var head = ordinary.Peek();
                            if (head.Writer ? wrote : reads >= MaxReadsPerUpdate)
                                continue;
                            work = ordinary.Dequeue();
                            pending--;
                            if (work.Writer)
                                wrote = true;
                            else
                                reads++;
                            break;
                        }
                        var group = groups[lane - 1];
                        if (group.Detached || group.Finished || group.Next >= group.Items.Length || reads >= MaxReadsPerUpdate)
                            continue;
                        selected = group;
                        index = group.Next++;
                        pending--;
                        reads++;
                        break;
                    }
                }
                if (work != null)
                {
                    work.Run();
                    continue;
                }
                if (selected == null)
                    return;
                Read(selected, index);
            }
        }
        void Read(ObservationGroup group, int index)
        {
            ObservationItemResult result;
            long? began = null, ended = null;
            BridgeErrorData interrupted = null;
            lock (gate)
            {
                if (group.Detached)
                    return;
            }
            try
            {
                worlds.Validate(group.Result.world);
                began = frame();
                lock (gate)
                {
                    if (group.Detached)
                        return;
                }
                var observation = catalog.Read(group.Items[index]);
                worlds.Validate(group.Result.world);
                ended = frame();
                observation.world = group.Result.world;
                result = new ObservationItemResult { id = group.Items[index].Id, observation = observation };
            }
            catch (OperationException error)
            {
                interrupted = BoundaryError(group.Result.world);
                result = new ObservationItemResult { id = group.Items[index].Id, error = Bounded(error.Error) };
                if (began.HasValue && interrupted == null)
                    ended = frame();
            }
            catch (Exception)
            {
                interrupted = BoundaryError(group.Result.world);
                result = Error(group.Items[index].Id, ErrorCodes.NativeRejected, "observation reader failed", 500);
                if (began.HasValue && interrupted == null)
                    ended = frame();
            }
            interrupted = BoundaryError(group.Result.world) ?? interrupted;
            lock (gate)
            {
                if (group.Detached)
                    return;
                if (began.HasValue && !group.Result.startFrame.HasValue)
                    group.Result.startFrame = began;
                if (interrupted != null)
                {
                    result = new ObservationItemResult { id = group.Items[index].Id, error = interrupted };
                    group.Result.worldChanged = !WorldIdentity.Same(group.Result.world, worlds.Current);
                    group.Result.endFrame = null;
                    Store(group, index, result);
                    pending -= group.Items.Length - group.Next;
                    for (int i = group.Next; i < group.Items.Length; i++)
                        Store(group, i, new ObservationItemResult { id = group.Items[i].Id, error = interrupted });
                    group.Next = group.Items.Length;
                }
                else
                {
                    if (ended.HasValue)
                        group.Result.endFrame = ended;
                    Store(group, index, result);
                }
                if (group.Next == group.Items.Length)
                    Finish(group);
            }
        }
        BridgeErrorData BoundaryError(WorldRef world)
        {
            try
            {
                worlds.Validate(world);
                return null;
            }
            catch (OperationException e)
            {
                return e.Error;
            }
        }
        static BridgeErrorData Bounded(BridgeErrorData error) => new BridgeErrorData(error.code, "observation reader rejected: " + error.code, httpStatus: error.httpStatus ?? 400);
        void Store(ObservationGroup group, int index, ObservationItemResult result)
        {
            byte[] bytes;
            try
            {
                bytes = encoder.Item(result);
            }
            catch
            {
                bytes = null;
            }
            int remaining = group.Items.Length - index - 1;
            if (bytes == null || bytes.Length > MaxItemBytes || group.Bytes + (long)bytes.Length + remaining * ErrorReserve + EnvelopeReserve > MaxGroupBytes)
            {
                result = Error(group.Items[index].Id, "RESULT_TOO_LARGE", "observation exceeds bounded response budget", 413);
                bytes = encoder.Item(result);
            }
            group.Bytes += bytes.Length;
            group.Result.results[index] = result;
        }
        void Finish(ObservationGroup group)
        {
            if (group.Finished || group.Detached)
                return;
            group.Finished = true;
            byte[] bytes = encoder.Batch(group.Result);
            if (bytes.Length > MaxGroupBytes)
                throw new InvalidOperationException("group encoder violated output bound");
            group.Complete(bytes);
        }
    }
}
