using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CS2MCP.Core
{
    public enum TraceRoute
    {
        unknown,
        cityTaxesSet,
        cityPoliciesSet,
        cityServiceBudgetsSet,
        buildPlace,
        buildRoad,
        buildUpgrade,
        buildJunction,
        buildZone,
        buildDemolish,
        cityLoanSet,
        cityFeesSet,
        cameraSet,
        simControl,
        simRun,
        gameSave,
        buildDistrict,
        districtPoliciesSet,
        transitStopsPlace,
        transitLinesCreate,
        transitLinesDelete,
        cityTilesBuy,
        buildRoadReplace,
        buildRoadConnect,
        buildPlaceShoreline,
        buildPlaceRoadside,
        transitLinesPoliciesSet,
        transitLinesVehiclesSet
    }

    public static class LifecycleTraceRoutes
    {
        public static TraceRoute ForPath(string fixedPath)
        {
            switch (fixedPath)
            {
                case "/city/taxes/set":
                    return TraceRoute.cityTaxesSet;
                case "/city/policies/set":
                    return TraceRoute.cityPoliciesSet;
                case "/city/service-budgets/set":
                    return TraceRoute.cityServiceBudgetsSet;
                case "/build/place":
                    return TraceRoute.buildPlace;
                case "/build/road":
                    return TraceRoute.buildRoad;
                case "/build/upgrade":
                    return TraceRoute.buildUpgrade;
                case "/build/junction":
                    return TraceRoute.buildJunction;
                case "/build/zone":
                    return TraceRoute.buildZone;
                case "/build/demolish":
                    return TraceRoute.buildDemolish;
                case "/city/loan/set":
                    return TraceRoute.cityLoanSet;
                case "/city/fees/set":
                    return TraceRoute.cityFeesSet;
                case "/camera/set":
                    return TraceRoute.cameraSet;
                case "/sim/control":
                    return TraceRoute.simControl;
                case "/sim/run":
                    return TraceRoute.simRun;
                case "/game/save":
                    return TraceRoute.gameSave;
                case "/build/district":
                    return TraceRoute.buildDistrict;
                case "/district/policies/set":
                    return TraceRoute.districtPoliciesSet;
                case "/transit/stops/place":
                    return TraceRoute.transitStopsPlace;
                case "/transit/lines/create":
                    return TraceRoute.transitLinesCreate;
                case "/transit/lines/delete":
                    return TraceRoute.transitLinesDelete;
                case "/city/tiles/buy":
                    return TraceRoute.cityTilesBuy;
                case "/build/road/replace":
                    return TraceRoute.buildRoadReplace;
                case "/build/road/connect":
                    return TraceRoute.buildRoadConnect;
                case "/build/place/shoreline":
                    return TraceRoute.buildPlaceShoreline;
                case "/build/place/roadside":
                    return TraceRoute.buildPlaceRoadside;
                case "/transit/lines/policies/set":
                    return TraceRoute.transitLinesPoliciesSet;
                case "/transit/lines/vehicles/set":
                    return TraceRoute.transitLinesVehiclesSet;
                default:
                    return TraceRoute.unknown;
            }
        }
    }

    public enum TraceAdapter
    {
        envelope,
        generic,
        road,
        transit,
        rename
    }

    public enum TraceKind
    {
        direct,
        placement,
        net,
        demolish,
        upgrade,
        area,
        nodeUpgrade,
        roadBuild,
        roadReplace,
        stop,
        line
    }

    public enum TracePhase
    {
        dispatch,
        queued,
        busy,
        definitions,
        validation,
        validationAccepted,
        validationRejected,
        applyObserved,
        finishObserved,
        replyAttempt,
        deactivateObserved,
        restorationAttempt,
        stopObserved,
        displacedObserved,
        exceptionObserved,
        renameQueued,
        renameDequeued,
        setterAttempt,
        responseBuilderAttempt,
        guardedWorkRefused,
        destroyedObserved
    }

    public enum TraceRole
    {
        prefab,
        target,
        start,
        end,
        stop,
        line
    }

    /// <summary>Immutable diagnostic identity, never an admitted operation or completion receipt.</summary>
    public sealed class LifecycleTraceContext
    {
        public string traceId { get; }
        public WorldRef ingressWorld { get; }
        public string route { get; }

        private readonly Action<LifecycleTraceContext, int?, bool> reply;
        public LifecycleTraceContext(WorldRef world, Action<LifecycleTraceContext, int?, bool> reply = null, TraceRoute route = TraceRoute.unknown)
        {
            traceId = "legacy:" + Guid.NewGuid().ToString("N");
            ingressWorld = world;
            this.reply = reply;
            this.route = Enum.IsDefined(typeof(TraceRoute), route) ? route.ToString() : "unknown";
        }

        internal void Reply(int? status, bool accepted)
        {
            try
            {
                reply?.Invoke(this, status, accepted);
            }
            catch
            {
            }
        }
    }

    public sealed class LifecycleTraceReference
    {
        public string role { get; }
        public WorldRef world { get; }
        public int index { get; }
        public int version { get; }

        public LifecycleTraceReference(TraceRole role, WorldRef world, int index, int version)
        {
            this.role = role.ToString();
            this.world = world;
            this.index = index;
            this.version = version;
        }
    }

    public sealed class LifecycleTraceEvent
    {
        public long sequence { get; }
        public string traceId { get; }
        public string route { get; }
        public string identityKind => "legacyDiagnosticOnly";
        public WorldRef ingressWorld { get; }
        public WorldRef observedWorld { get; }
        public bool observedReady { get; }
        public long? frame { get; }
        public string frameReason => frame.HasValue ? null : "notSampled";
        public DateTime recordedAtUtc { get; }
        public string adapter { get; }
        public string kind { get; }
        public string phase { get; }
        public int? status { get; }
        public bool? ownsActiveTool { get; }
        public bool? completionAccepted { get; }
        public IReadOnlyList<LifecycleTraceReference> references { get; }
        public int omittedReferences { get; }
        public string effects => "unknown";
        public string cleanup => "unverified";
        public string authority => "unverified";

        internal LifecycleTraceEvent(long seq, LifecycleTraceContext context, WorldSnapshot observed, long? frame, DateTime utc, TraceAdapter adapter, TraceKind kind, TracePhase phase, int? status, bool? ownership, bool? accepted, LifecycleTraceReference[] references, int omitted)
        {
            sequence = seq;
            traceId = context.traceId;
            route = context.route;
            ingressWorld = context.ingressWorld;
            observedWorld = observed.world;
            observedReady = observed.ready;
            this.frame = frame;
            recordedAtUtc = utc;
            this.adapter = adapter.ToString();
            this.kind = kind.ToString();
            this.phase = phase.ToString();
            this.status = status;
            ownsActiveTool = ownership;
            completionAccepted = accepted;
            this.references = Array.AsReadOnly(references);
            omittedReferences = omitted;
        }
    }

    public sealed class LifecycleTraceQuery
    {
        public string BootId { get; }
        public long? AfterSequence { get; }
        public int Limit { get; }

        private LifecycleTraceQuery(string boot, long? after, int limit)
        {
            BootId = boot;
            AfterSequence = after;
            Limit = limit;
        }

        public static LifecycleTraceQuery Parse(IDictionary<string, string> query)
        {
            string boot = null;
            long? after = null;
            int limit = 16;
            foreach (var pair in query)
            {
                switch (pair.Key)
                {
                    case "bootId":
                        if (string.IsNullOrWhiteSpace(pair.Value) || pair.Value.Length > 128)
                            throw CommandJson.Invalid("invalid trace bootId");
                        boot = pair.Value;
                        break;
                    case "afterSequence":
                        if (!long.TryParse(pair.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long seq) || seq < 0 || seq > LifecycleTraceJournal.MaxSafe)
                            throw CommandJson.Invalid("invalid trace afterSequence");
                        after = seq;
                        break;
                    case "limit":
                        if (!int.TryParse(pair.Value, NumberStyles.None, CultureInfo.InvariantCulture, out limit) || limit < 1 || limit > 32)
                            throw CommandJson.Invalid("invalid trace limit");
                        break;
                    default:
                        throw CommandJson.Invalid("unknown trace query field");
                }
            }

            if (after.HasValue && boot == null)
                throw CommandJson.Invalid("trace cursor requires bootId");
            return new LifecycleTraceQuery(boot, after, limit);
        }

        public static Dictionary<string, string> DecodeQuery(string raw)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (raw.Length == 0)
                return result;
            if (raw.Length > 2048)
                throw CommandJson.Invalid("trace query too large");
            foreach (string part in raw.Split('&'))
            {
                int equals = part.IndexOf('=');
                if (equals < 1)
                    throw CommandJson.Invalid("invalid trace query");
                string key = Uri.UnescapeDataString(part.Substring(0, equals));
                if (result.ContainsKey(key))
                    throw CommandJson.Invalid("duplicate trace query field");
                result.Add(key, Uri.UnescapeDataString(part.Substring(equals + 1)));
            }

            Parse(result);
            return result;
        }
    }

    public sealed class LifecycleTracePage
    {
        public string provenance => "managedPassiveHistory";
        public string consistency => "independentPageNotSnapshot";
        public string completion => "notEstablished";
        public string bootId { get; internal set; }
        public WorldRef readWorld { get; internal set; }
        public bool readReady { get; internal set; }
        public DateTime readAtUtc { get; internal set; }
        public long highWater { get; internal set; }
        public long? retainedFloor { get; internal set; }
        public int retainedCount { get; internal set; }
        public long retainedBytes { get; internal set; }
        public long evictedEvents { get; internal set; }
        public long lostObservations { get; internal set; }
        public bool saturated { get; internal set; }
        public long tailOmitted { get; internal set; }
        public long? evictedFrom { get; internal set; }
        public long? evictedThrough { get; internal set; }
        public long? nextSequence { get; internal set; }
        public bool hasMore { get; internal set; }
        public int limit { get; internal set; }
        public LifecycleTraceEvent[] events { get; internal set; }
    }

    /// <summary>Only fixed immutable events are retained; byte caps are serialized UTF8, not CLR heap promises.</summary>
    public sealed class LifecycleTraceJournal
    {
        public const int Capacity = 256, MaxEventBytes = 8192, MaxJournalBytes = 2097152;
        public const long MaxSafe = 9007199254740991;
        private readonly object gate = new object();
        private readonly Queue<Tuple<LifecycleTraceEvent, int>> retained = new Queue<Tuple<LifecycleTraceEvent, int>>();
        private readonly Func<LifecycleTraceEvent, int> measure;
        private readonly string boot;
        private long sequence, bytes, evicted, lost;
        private bool saturated;
        public LifecycleTraceJournal(string bootId, Func<LifecycleTraceEvent, int> measure)
        {
            boot = bootId;
            this.measure = measure;
        }

        private void Increment(ref long value)
        {
            if (value < MaxSafe)
                value++;
            else
                saturated = true;
        }

        public void NoteLoss()
        {
            lock (gate)
                Increment(ref lost);
        }

        public void Observe(LifecycleTraceContext context, TraceAdapter adapter, TraceKind kind, TracePhase phase,
            Func<WorldSnapshot> world, Func<long?> frame, Func<DateTime> utc, Func<bool?> ownership = null,
            int? status = null, bool? accepted = null, LifecycleTraceReference[] references = null, int omitted = 0)
        {
            if (context == null)
                return;
            try
            {
                if (!Enum.IsDefined(typeof(TraceAdapter), adapter) || !Enum.IsDefined(typeof(TraceKind), kind) || !Enum.IsDefined(typeof(TracePhase), phase))
                    throw new ArgumentException("invalid fixed trace value");
                WorldSnapshot observed = world();
                long? sampledFrame = frame();
                DateTime time = utc();
                bool? owner = ownership?.Invoke();
                var refs = references == null ? Array.Empty<LifecycleTraceReference>() : references.Take(4).ToArray();
                int omittedCount = (int)Math.Min(int.MaxValue, (long)Math.Max(0, omitted) + Math.Max(0, (references?.Length ?? 0) - 4));
                lock (gate)
                {
                    if (sequence == MaxSafe)
                    {
                        saturated = true;
                        Increment(ref lost);
                        return;
                    }

                    var entry = new LifecycleTraceEvent(sequence + 1, context, observed, sampledFrame, time, adapter, kind, phase, status, owner, accepted, refs, omittedCount);
                    int size = measure(entry);
                    if (size < 1 || size > MaxEventBytes)
                    {
                        Increment(ref lost);
                        return;
                    }

                    // Reserve array delimiters and one comma per additional retained event.
                    // retainedBytes reports event bytes; the journal framing is reserved separately.
                    while (retained.Count >= Capacity || bytes + size + retained.Count + 2 > MaxJournalBytes)
                    {
                        bytes -= retained.Dequeue().Item2;
                        Increment(ref evicted);
                    }

                    sequence++;
                    retained.Enqueue(Tuple.Create(entry, size));
                    bytes += size;
                }
            }
            catch
            {
                NoteLoss();
            }
        }

        public LifecycleTracePage Read(LifecycleTraceQuery query, WorldSnapshot observed, DateTime utc)
        {
            lock (gate)
            {
                if (query.BootId != null && query.BootId != boot)
                    throw new OperationException(new BridgeErrorData(ErrorCodes.WorldMismatch, "trace cursor boot mismatch", httpStatus: 409));
                if (query.AfterSequence > sequence)
                    throw CommandJson.Invalid("trace cursor is ahead of journal");
                long floor = retained.Count == 0 ? sequence + 1 : retained.Peek().Item1.sequence;
                var all = retained.Select(row => row.Item1).ToArray();
                var rows = query.AfterSequence.HasValue ? all.Where(row => row.sequence > query.AfterSequence.Value).Take(query.Limit).ToArray() : all.Skip(Math.Max(0, all.Length - query.Limit)).ToArray();
                long? next = rows.Length > 0 ? rows[rows.Length - 1].sequence : query.AfterSequence;
                bool gap = query.AfterSequence.HasValue && query.AfterSequence.Value < floor - 1;
                return new LifecycleTracePage
                {
                    bootId = boot,
                    readWorld = observed.world,
                    readReady = observed.ready,
                    readAtUtc = utc,
                    highWater = sequence,
                    retainedFloor = all.Length == 0 ? (long?)null : floor,
                    retainedCount = all.Length,
                    retainedBytes = bytes,
                    evictedEvents = evicted,
                    lostObservations = lost,
                    saturated = saturated,
                    tailOmitted = query.AfterSequence.HasValue ? 0 : Math.Max(0, all.Length - rows.Length),
                    evictedFrom = gap ? query.AfterSequence.Value + 1 : (long?)null,
                    evictedThrough = gap ? floor - 1 : (long?)null,
                    nextSequence = next,
                    hasMore = next.HasValue && next.Value < sequence,
                    limit = query.Limit,
                    events = rows
                };
            }
        }
    }
}
