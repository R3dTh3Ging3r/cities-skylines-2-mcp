using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CS2MCP.Core
{
    public sealed class MetricValue
    {
        public double? value { get; set; }
        public string unit { get; set; }
        public string period { get; set; }
        public string source { get; set; }
        public string support { get; set; }
        public string reason { get; set; }
    }

    public sealed class MetricDefinition
    {
        public string key { get; set; }
        public string label { get; set; }
        public string kind { get; set; }
        public string unit { get; set; }
        public string period { get; set; }
        public string source { get; set; }
        public string[] parameters => Array.Empty<string>();
        public object[] parameterDefinitions => Array.Empty<object>();
        public string refresh { get; set; }
        public Capability support => new Capability
        {
            name = key,
            support = "unverified",
            reason = "nativeDiagnosticMetricParityNotRun"
        };
    }

    /// <summary>Only fixed, actually consumed raw metric meanings. No paths, formulas or native unit guesses.</summary>
    public static class DiagnosticMetricCatalog
    {
        public const string Revision = "diagnosticMetrics.v1";
        static MetricDefinition Define(string key, string label, string kind, string unit, string source, string refresh, string period = null) => new MetricDefinition
        {
            key = key,
            label = label,
            kind = kind,
            unit = unit,
            source = source,
            refresh = refresh,
            period = period
        };
        public static MetricDefinition[] Read(string family = "all")
        {
            if (!new[]
            {
                "all",
                "city",
                "labor",
                "budget",
                "transit"
            }.Contains(family))
                throw CommandJson.Invalid("unknown metric family");
            var definitions = new[]
            {
                Define("city.population", "Native population", "count", "citizens", "Population.m_Population", "liveComponentSnapshot"),
                Define("city.populationWithMoveIn", "Native population including move-in", "count", "citizens", "Population.m_PopulationWithMoveIn", "liveComponentSnapshot"),
                Define("city.money", "Raw treasury money", "stock", "nativeMoney", "CitySystem.moneyAmount", "liveComponentSnapshot"),
                Define("city.xp", "City XP", "stock", "XP", "CitySystem.XP", "liveComponentSnapshot"),
                Define("labor.employed", "City worker count", "count", "workers", "CountHouseholdDataSystem.CityWorkerCount", "cachedSimulationCounterRefreshTimeUnknown"),
                Define("labor.homelessCitizens", "Homeless citizen count", "count", "citizens", "CountHouseholdDataSystem.HomelessCitizenCount", "cachedSimulationCounterRefreshTimeUnknown"),
                Define("labor.jobs.total", "Total workplace slots", "count", "workplaceSlots", "CountWorkplacesSystem.GetTotalWorkplaces.TotalCount", "cachedSimulationCounterRefreshTimeUnknown"),
                Define("labor.jobs.free", "Free workplace slots", "count", "workplaceSlots", "CountWorkplacesSystem.GetFreeWorkplaces.TotalCount", "cachedSimulationCounterRefreshTimeUnknown"),
                Define("budget.income", "Monthly income rate", "rate", "nativeMoney", "CityServiceBudgetSystem.GetTotalIncome", "gameHour", "month"),
                Define("budget.expenses", "Signed monthly expenses rate", "rate", "nativeMoney", "CityServiceBudgetSystem.GetTotalExpenses", "gameHour", "month"),
                Define("budget.taxIncome", "Monthly tax income rate", "rate", "nativeMoney", "CityServiceBudgetSystem.GetTotalTaxIncome", "gameHour", "month"),
                Define("budget.balance", "Native monthly balance rate", "rate", "nativeMoney", "CityServiceBudgetSystem.GetBalance", "gameHour", "month"),
                Define("transit.waypointCount", "Inspected waypoint slots", "count", "waypoints", "TransitCompleteness.waypointCount", "selectedLineStructuralSnapshot"),
                Define("transit.segmentCount", "Inspected segment slots", "count", "segments", "TransitCompleteness.segmentCount", "selectedLineStructuralSnapshot"),
                Define("transit.brokenWaypointCount", "Exact structurally faulty waypoint slots", "count", "waypoints", "TransitWaypoint.structuralAssessment", "selectedLineStructuralSnapshot"),
                Define("transit.invalidSegmentCount", "Exact structurally faulty segment slots", "count", "segments", "TransitSegment.structuralAssessment", "selectedLineStructuralSnapshot")
            };
            return family == "all" ? definitions : definitions.Where(d => d.key.StartsWith(family + ".", StringComparison.Ordinal)).ToArray();
        }

        internal static MetricDefinition Find(string key) => Read().SingleOrDefault(d => d.key == key) ?? throw CommandJson.Invalid("unknown metric key");
    }

    public sealed class MetricScope
    {
        public string kind { get; }

        readonly TransitQuery transit;
        public EntityRef line => transit?.Line;
        internal TransitQuery Transit => transit;

        internal MetricScope(string kind, TransitQuery transit = null)
        {
            this.kind = kind;
            this.transit = transit;
        }
    }

    /// <summary>Copied, immutable normalized arguments. Original caller world survives deferred dispatch.</summary>
    public sealed class MetricReadRequest
    {
        public WorldRef World { get; }
        public MetricScope Scope { get; }

        readonly string[] keys;
        public string[] Keys => (string[])keys.Clone();

        MetricReadRequest(WorldRef world, MetricScope scope, string[] keys)
        {
            World = world;
            Scope = scope;
            this.keys = keys;
        }

        static Dictionary<string, object> Map(object value) => value as Dictionary<string, object> ?? throw CommandJson.Invalid("expected metric object");
        static void Fields(Dictionary<string, object> map, params string[] fields)
        {
            if (map.Keys.Any(k => !fields.Contains(k)))
                throw CommandJson.Invalid("unknown metric field");
        }

        public static MetricReadRequest ParseArguments(object value)
        {
            var map = Map(value);
            Fields(map, "world", "scope", "queries");
            if (map.Count != 3 || !map.TryGetValue("world", out var rawWorld) || !map.TryGetValue("scope", out var rawScope) || !map.TryGetValue("queries", out var rawQueries))
                throw CommandJson.Invalid("require metric world, scope and queries");
            var world = ObservationCatalog.World(rawWorld);
            var scope = Map(rawScope);
            Fields(scope, "kind", "line");
            if (!scope.TryGetValue("kind", out var rawKind) || !(rawKind is string kind))
                throw CommandJson.Invalid("explicit metric scope required");
            MetricScope normalized;
            if (kind == "city" && scope.Count == 1)
                normalized = new MetricScope(kind);
            else if (kind == "selectedLine" && scope.Count == 2 && scope.TryGetValue("line", out var line))
            {
                var transit = TransitQuery.ParseArguments(new Dictionary<string, object> { { "line", line } });
                if (!WorldIdentity.Same(world, transit.World))
                    throw CommandJson.Invalid("mixed metric worlds");
                normalized = new MetricScope(kind, transit);
            }
            else
                throw CommandJson.Invalid("unsupported metric scope");
            if (!(rawQueries is List<object> list) || list.Count < 1 || list.Count > 64)
                throw CommandJson.Invalid("require 1–64 metric queries");
            var keys = new List<string>();
            foreach (var item in list)
            {
                var query = Map(item);
                Fields(query, "key", "parameters");
                if (query.Count != 2 || !query.TryGetValue("key", out var rawKey) || !(rawKey is string key) || key.Length < 1 || key.Length > 128 || keys.Contains(key) || !query.TryGetValue("parameters", out var rawParameters))
                    throw CommandJson.Invalid("invalid or duplicate metric query");
                // All sixteen v1 definitions are parameterless: reject rather than ignore any scalar or nested parameter.
                if (Map(rawParameters).Count != 0)
                    throw CommandJson.Invalid("metric definition accepts no parameters");
                DiagnosticMetricCatalog.Find(key);
                if (key.StartsWith("transit.", StringComparison.Ordinal) != (kind == "selectedLine"))
                    throw CommandJson.Invalid("metric key requires different scope");
                keys.Add(key);
            }

            return new MetricReadRequest(world, normalized, keys.ToArray());
        }

        public static Dictionary<string, string> DecodeFields(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 12000)
                throw CommandJson.Invalid("missing or oversized metric query");
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string part in text.Split('&'))
            {
                int split = part.IndexOf('=');
                if (split <= 0)
                    throw CommandJson.Invalid("malformed metric query");
                for (int i = 0; i < part.Length; i++)
                    if (part[i] == '%')
                    {
                        if (i + 2 >= part.Length || !Uri.IsHexDigit(part[i + 1]) || !Uri.IsHexDigit(part[i + 2]))
                            throw CommandJson.Invalid("malformed metric escape");
                        i += 2;
                    }

                string key = Uri.UnescapeDataString(part.Substring(0, split));
                if (result.ContainsKey(key))
                    throw CommandJson.Invalid("duplicate metric query field");
                result.Add(key, Uri.UnescapeDataString(part.Substring(split + 1)));
            }

            return result;
        }

        public static MetricReadRequest DecodeQuery(string text)
        {
            var fields = DecodeFields(text);
            if (fields.Count != 1 || !fields.TryGetValue("query", out string json))
                throw CommandJson.Invalid("require one encoded metric query");
            return ParseArguments(CommandJson.Parse(json));
        }
    }

    public sealed class MetricSourceObservation
    {
        public string id { get; set; }
        public WorldRef world { get; set; }
        public long startFrame { get; set; }
        public long endFrame { get; set; }
        public DateTime sampledAtUtc { get; set; }
        public string freshness { get; set; }
        public string[] warnings { get; set; }
        public string completeness { get; set; }
        public DateTime? refreshTimeUtc => null;
    }

    public sealed class ObservedMetricClock
    {
        public string source => "TimeSystem";
        public float rawNormalizedTime { get; set; }
        public float rawNormalizedDate { get; set; }
        public int rawYear { get; set; }
        public int rawDaysPerYear { get; set; }
        public long frameIndex { get; set; }
        public long startFrame { get; set; }
        public long endFrame { get; set; }
        public string sourceObservationId { get; set; }
        public string frameEvidence => "sourceObservationEnd";
        public string conversionStatus => "unverified";
        public string reason => "nativeCalendarParityNotRun";
    }

    public sealed class MetricMemberValues
    {
        public EntityRef member { get; set; }
        public Dictionary<string, MetricValue> metrics { get; set; }
    }

    public sealed class MetricMemberEvidence
    {
        public EntityRef member { get; set; }
        public string fingerprint { get; set; }
        public string coverage => "structuralSampleOnly";
        public bool complete { get; set; }
        public string[] reasons { get; set; }
    }

    public sealed class MetricBatch
    {
        public int schemaVersion => 1;
        public string definitionRevision => DiagnosticMetricCatalog.Revision;
        public MetricScope scope { get; set; }
        public Dictionary<string, MetricValue> values { get; set; }
        public EntityRef[] members { get; set; }
        public MetricMemberValues[] perMember { get; set; }
        public MetricSourceObservation[] sources { get; set; }
        public ObservedMetricClock clock { get; set; }
        public MetricMemberEvidence[] memberEvidence { get; set; }
    }

    public interface IDiagnosticMetricSources
    {
        bool Warming { get; }

        CityStateData State();
        CityOverviewData Overview();
        CityLaborData Labor();
        CityBudgetData Budget();
        Observation<TransitPage> SelectedLine(TransitQuery query);
    }

    /// <summary>Actual bounded producer used by both GET and O7. No persistent native cache.</summary>
    public sealed class DiagnosticMetricReader
    {
        readonly IDiagnosticMetricSources sources;
        readonly WorldIdentity worlds;
        readonly Func<long> frame;
        readonly Func<DateTime> utc;
        public DiagnosticMetricReader(IDiagnosticMetricSources sources, WorldIdentity worlds, Func<long> frame, Func<DateTime> utc)
        {
            this.sources = sources;
            this.worlds = worlds;
            this.frame = frame;
            this.utc = utc;
        }

        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        static bool Same(EntityRef a, EntityRef b) => a != null && b != null && WorldIdentity.Same(a.world, b.world) && a.index == b.index && a.version == b.version;
        static bool Complete(TransitLineView line) => line?.completeness?.state == "complete" && line.waypoints != null && line.segments != null && line.completeness.waypointCount == line.waypoints.Length && line.completeness.segmentCount == line.segments.Length && line.completeness.readWaypoints == line.waypoints.Length && line.completeness.readSegments == line.segments.Length && (long)line.waypoints.Length + line.segments.Length <= TransitReader.MaxRouteEntries;
        static double? Faults(IEnumerable<string> assessments)
        {
            var values = assessments.ToArray();
            return values.Any(a => a != "knownFault" && a != "noKnownFault") ? (double? )null : values.Count(a => a == "knownFault");
        }

        static MetricValue Value(MetricDefinition definition, double? value, string reason)
        {
            if (value.HasValue && !Finite(value.Value))
            {
                value = null;
                reason = definition.kind == "count" ? "invalidNativeCount" : "nonfiniteNativeValue";
            }

            if (value.HasValue && definition.kind == "count" && (value < 0 || value > 9007199254740991d || Math.Truncate(value.Value) != value.Value))
            {
                value = null;
                reason = "invalidNativeCount";
            }

            return new MetricValue
            {
                value = value,
                unit = definition.unit,
                period = definition.period,
                source = definition.source,
                support = value.HasValue ? "supported" : "unavailable",
                reason = value.HasValue ? null : reason ?? "nativeFieldUnavailable"
            };
        }

        public Observation<MetricBatch> Read(MetricReadRequest request)
        {
            worlds.Validate(request.World);
            long start = frame();
            var metadata = new List<MetricSourceObservation>();
            T ReadSource<T>(string id, Func<T> read)
                where T : class
            {
                worlds.Validate(request.World);
                long begin = frame();
                T data = null;
                var warnings = new List<string>
                {
                    "nativeObservationParityNotRun",
                    "cachedSourceRefreshTimeUnknown"
                };
                try
                {
                    data = read();
                    if (data is IBoundedObservationData bounded)
                        bounded.ValidateObservationBounds();
                }
                catch (Exception error)
                {
                    worlds.Validate(request.World);
                    if (error is OperationException operation && (operation.Error.code == ErrorCodes.WorldMismatch || operation.Error.code == ErrorCodes.NotReady))
                        throw;
                    data = null;
                    warnings.Add("nativeSourceReadFailed");
                }

                worlds.Validate(request.World);
                long end = frame();
                if (end != begin)
                    warnings.Add("multiFrameReadNotAtomic");
                if (data is IObservationDataWarnings additional)
                    warnings.AddRange(additional.ObservationWarnings);
                metadata.Add(new MetricSourceObservation { id = id, world = request.World, startFrame = begin, endFrame = end, sampledAtUtc = utc(), freshness = data == null ? "unavailable" : sources.Warming ? "warming" : "current", warnings = warnings.ToArray(), completeness = data == null ? "incomplete" : "complete" });
                return data;
            }

            var keys = request.Keys;
            CityOverviewData overview = null;
            CityLaborData labor = null;
            CityBudgetData budget = null;
            CityStateData state = null;
            if (keys.Any(k => k.StartsWith("city.", StringComparison.Ordinal)))
                overview = ReadSource("city.overview", sources.Overview);
            if (keys.Any(k => k.StartsWith("labor.", StringComparison.Ordinal)))
                labor = ReadSource("city.labor", sources.Labor);
            if (keys.Any(k => k.StartsWith("budget.", StringComparison.Ordinal)))
                budget = ReadSource("city.budget", sources.Budget);
            if (overview?.calendarMetadata == null)
                state = ReadSource("state", sources.State);
            TransitLineView line = null;
            if (request.Scope.kind == "selectedLine")
            {
                worlds.Validate(request.World);
                long begin = frame();
                Observation<TransitPage> observation = null;
                try
                {
                    observation = sources.SelectedLine(request.Scope.Transit);
                }
                catch (Exception error)
                {
                    worlds.Validate(request.World);
                    if (error is OperationException operation && (operation.Error.code == ErrorCodes.WorldMismatch || operation.Error.code == ErrorCodes.NotReady))
                        throw;
                }

                worlds.Validate(request.World);
                if (observation != null && !WorldIdentity.Same(observation.world, request.World))
                    throw new OperationException(new BridgeErrorData(ErrorCodes.WorldMismatch, "transit metric source world mismatch", httpStatus: 409));
                if (observation?.data?.items?.Length == 1 && Same(observation.data.requestedLine, request.Scope.line) && Same(observation.data.items[0].entity, request.Scope.line))
                    line = observation.data.items[0];
                metadata.Add(new MetricSourceObservation { id = "transit.selectedLine", world = request.World, startFrame = observation?.startFrame ?? begin, endFrame = observation?.endFrame ?? frame(), sampledAtUtc = observation?.sampledAtUtc ?? utc(), freshness = observation?.freshness ?? "unavailable", warnings = observation?.warnings ?? new[] { "nativeSourceReadFailed" }, completeness = Complete(line) ? "complete" : "incomplete" });
            }

            var values = new Dictionary<string, MetricValue>(StringComparer.Ordinal);
            foreach (string key in keys)
            {
                double? number = null;
                string reason = "sourceReadUnavailable";
                switch (key)
                {
                    case "city.population":
                        number = overview?.PopulationAvailable == true ? overview.population : (double? )null;
                        reason = "populationComponentUnavailable";
                        break;
                    case "city.populationWithMoveIn":
                        number = overview?.PopulationAvailable == true ? overview.populationWithMoveIn : (double? )null;
                        reason = "populationComponentUnavailable";
                        break;
                    case "city.money":
                        number = overview?.money;
                        break;
                    case "city.xp":
                        number = overview?.xp;
                        break;
                    case "labor.employed":
                        number = labor?.employed;
                        break;
                    case "labor.homelessCitizens":
                        number = labor?.homelessCitizens;
                        break;
                    case "labor.jobs.total":
                        number = labor?.jobs?.total;
                        break;
                    case "labor.jobs.free":
                        number = labor?.jobs?.free;
                        break;
                    case "budget.income":
                        number = budget?.totalIncome;
                        break;
                    case "budget.expenses":
                        number = budget?.totalExpenses;
                        break;
                    case "budget.taxIncome":
                        number = budget?.totalTaxIncome;
                        break;
                    case "budget.balance":
                        number = budget?.balance;
                        break;
                    case "transit.waypointCount":
                        number = Complete(line) ? line.completeness.waypointCount : (double? )null;
                        reason = "routeTraversalIncomplete";
                        break;
                    case "transit.segmentCount":
                        number = Complete(line) ? line.completeness.segmentCount : (double? )null;
                        reason = "routeTraversalIncomplete";
                        break;
                    case "transit.brokenWaypointCount":
                        number = Complete(line) ? Faults(line.waypoints.Select(w => w?.structuralAssessment)) : (double? )null;
                        reason = Complete(line) ? "waypointAssessmentIncomplete" : "routeTraversalIncomplete";
                        break;
                    case "transit.invalidSegmentCount":
                        number = Complete(line) ? Faults(line.segments.Select(s => s?.structuralAssessment)) : (double? )null;
                        reason = Complete(line) ? "segmentAssessmentIncomplete" : "routeTraversalIncomplete";
                        break;
                }

                values.Add(key, Value(DiagnosticMetricCatalog.Find(key), number, reason));
            }

            CalendarMetadata calendar = overview?.calendarMetadata ?? state?.simulation?.calendarMetadata;
            var clockSource = metadata.FirstOrDefault(m => m.id == (overview?.calendarMetadata != null ? "city.overview" : "state"));
            ObservedMetricClock clock = null;
            if (calendar != null && Finite(calendar.rawNormalizedTime) && Finite(calendar.rawNormalizedDate) && calendar.rawNormalizedTime >= 0 && calendar.rawNormalizedTime < 1 && calendar.rawNormalizedDate >= 0 && calendar.rawNormalizedDate < 1 && calendar.rawDaysPerYear > 0 && clockSource != null)
                clock = new ObservedMetricClock
                {
                    sourceObservationId = clockSource.id,
                    rawNormalizedTime = calendar.rawNormalizedTime,
                    rawNormalizedDate = calendar.rawNormalizedDate,
                    rawYear = calendar.rawYear,
                    rawDaysPerYear = calendar.rawDaysPerYear,
                    frameIndex = clockSource.endFrame,
                    startFrame = clockSource.startFrame,
                    endFrame = clockSource.endFrame
                };
            var warningsOuter = new List<string>
            {
                "nativeDiagnosticMetricParityNotRun",
                "sourceRefreshTimesUnknown",
                "observedSamplesNotNativeHistory"
            };
            if (metadata.Count > 1)
                warningsOuter.Add("multiSourceReadNotAtomic");
            if (clock == null)
                warningsOuter.Add("rawNormalizedClockUnavailable");
            if (budget?.totalExpenses > 0)
                warningsOuter.Add("nativeExpenseSignConventionUnexpected");
            foreach (var source in metadata)
                warningsOuter.AddRange(source.warnings);
            warningsOuter = warningsOuter.Distinct(StringComparer.Ordinal).ToList();
            long finish = frame();
            if (start != finish)
                warningsOuter.Add("multiFrameReadNotAtomic");
            worlds.Validate(request.World);
            var members = request.Scope.kind == "city" ? Array.Empty<EntityRef>() : new[]
            {
                request.Scope.line
            };
            var evidence = members.Select(member => Evidence(member, line)).ToArray();
            return new Observation<MetricBatch>
            {
                world = request.World,
                startFrame = start,
                endFrame = finish,
                sampledAtUtc = utc(),
                freshness = metadata.Any(m => m.freshness == "warming") ? "warming" : "current",
                warnings = warningsOuter.ToArray(),
                data = new MetricBatch
                {
                    scope = request.Scope,
                    values = values,
                    members = members,
                    perMember = members.Select(member => new MetricMemberValues { member = member, metrics = values }).ToArray(),
                    sources = metadata.ToArray(),
                    clock = clock,
                    memberEvidence = evidence
                }
            };
        }

        static object Ref(EntityRef entity) => entity == null ? null : new object[]
        {
            entity.world.bootId,
            entity.world.epoch,
            entity.index,
            entity.version
        };
        static MetricMemberEvidence Evidence(EntityRef member, TransitLineView line)
        {
            bool complete = Complete(line) && line.mode != null && new[]
            {
                "passenger",
                "cargo",
                "mixed"
            }.Contains(line.purpose) && line.waypoints.All(w => w != null && w.entity != null && w.connection != null && w.position != null && Finite(w.position.x) && Finite(w.position.y) && Finite(w.position.z)) && line.segments.All(s => s?.entity != null && s.rawDistance.HasValue && Finite(s.rawDistance.Value));
            string fingerprint = null;
            if (complete)
            {
                object canonical = new object[]
                {
                    "transitStructuralSample.v1",
                    Ref(member),
                    line.mode,
                    line.purpose,
                    line.completeness.state,
                    line.waypoints.Select(w => (object)new object[] { Ref(w.entity), Ref(w.connection), w.position.x, w.position.y, w.position.z }).ToArray(),
                    line.segments.Select(s => (object)new object[] { Ref(s.entity), s.rawDistance.Value }).ToArray()
                };
                using (var hash = SHA256.Create())
                    fingerprint = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(CommandJson.Stringify(canonical)))).Replace("-", "").ToLowerInvariant();
            }

            return new MetricMemberEvidence
            {
                member = member,
                fingerprint = fingerprint,
                complete = complete,
                reasons = complete ? new[]
                {
                    "structuralSampleNotCompleteRouteGeometry"
                }

                : new[]
                {
                    "structuralFingerprintEvidenceIncomplete"
                }
            };
        }
    }
}
