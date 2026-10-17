using System;
using System.Collections.Generic;
using CS2MCP.Core;
using Unity.Entities;

namespace CS2MCP
{
    /// <summary>Best-effort observer. Every entry point isolates diagnostics from original control flow.</summary>
    internal static class ConstructionLifecycleTrace
    {
        private static LifecycleTraceJournal journal;
        private static readonly object gate = new object();
        internal static LifecycleTraceJournal Journal
        {
            get
            {
                lock (gate)
                    return journal ?? (journal = new LifecycleTraceJournal(Mod.Worlds.Current.bootId, entry => ObservationWire.TraceEvent(entry).Length));
            }

            set
            {
                lock (gate)
                    journal = value;
            }
        }

        internal static LifecycleTraceContext Start(BridgeRequest request)
        {
            try
            {
                if (request == null)
                    return null;
                return request.LifecycleContext ?? (request.LifecycleContext = new LifecycleTraceContext(
                    request.IngressWorld, Reply,
                    LifecycleTraceRoutes.ForPath(EndpointCatalog.Resolve(request.Path, out _)?.path)));
            }
            catch
            {
                Loss();
                return null;
            }
        }

        internal static void Loss()
        {
            try
            {
                Journal.NoteLoss();
            }
            catch
            {
            }
        }

        internal static void Record(LifecycleTraceContext context, TraceAdapter adapter, TraceKind kind, TracePhase phase,
            Func<bool?> ownership = null, int? status = null, bool? accepted = null,
            Func<LifecycleTraceReference[]> references = null, int omitted = 0, bool sampleFrame = true)
        {
            try
            {
                WorldSnapshot observed = Mod.Worlds.Snapshot;
                // Old-world diagnostic callbacks must not inspect replacement-world tools or frames.
                bool nativeSample = sampleFrame && observed.ready && WorldIdentity.Same(context?.ingressWorld, observed.world);
                Journal.Observe(context, adapter, kind, phase, () => observed,
                    () => nativeSample ? BridgeSystem.Instance?.CurrentFrame : (long?)null,
                    () => DateTime.UtcNow, nativeSample ? ownership : null,
                    status, accepted, references?.Invoke(), omitted);
            }
            catch
            {
                Loss();
            }
        }

        // Complete may execute on a listener thread. No native frame, ECS or tool getter is reached here.
        internal static void Reply(LifecycleTraceContext context, int? status, bool accepted) => Record(context, TraceAdapter.envelope, TraceKind.direct, TracePhase.replyAttempt, status: status, accepted: accepted, sampleFrame: false);
        internal static LifecycleTraceReference Reference(LifecycleTraceContext context, TraceRole role, Entity entity) => new LifecycleTraceReference(role, context?.ingressWorld, entity.Index, entity.Version);
        internal static LifecycleTracePage Read(IDictionary<string, string> query) => Journal.Read(LifecycleTraceQuery.Parse(query), Mod.Worlds.Snapshot, DateTime.UtcNow);
    }
}
