using System;
using System.Collections.Generic;
using System.Linq;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class DiagnosticMetricTests
    {
        static WorldIdentity Ready()
        {
            var w = new WorldIdentity("boot");
            w.BeginLoad();
            w.CompleteLoad();
            return w;
        }

        static EntityRef Ref(int index = 10) => new EntityRef
        {
            world = new WorldRef("boot", 1),
            index = index,
            version = 1
        };
        internal static MetricReadRequest Request(string[] keys, bool transit = false)
        {
            object world = new Dictionary<string, object>
            {
                {
                    "bootId",
                    "boot"
                },
                {
                    "epoch",
                    1
                }
            };
            var scope = new Dictionary<string, object>
            {
                {
                    "kind",
                    transit ? "selectedLine" : "city"
                }
            };
            if (transit)
                scope.Add("line", new Dictionary<string, object> { { "world", world }, { "index", 10 }, { "version", 1 } });
            return MetricReadRequest.ParseArguments(new Dictionary<string, object> { { "world", world }, { "scope", scope }, { "queries", keys.Select(k => (object)new Dictionary<string, object> { { "key", k }, { "parameters", new Dictionary<string, object>() } }).ToList() } });
        }

        sealed class Sources : IDiagnosticMetricSources
        {
            internal readonly List<string> Calls = new List<string>();
            internal Action During;
            internal CityOverviewData OverviewData = new CityOverviewData
            {
                PopulationAvailable = true,
                population = 0,
                populationWithMoveIn = 8,
                money = -12,
                xp = 9,
                calendarMetadata = MetricContracts.DescribeCalendar(.2f, .3f, 2026, 12)
            };
            internal CityStateData StateData = new CityStateData
            {
                simulation = new CitySimulationData
                {
                    frameIndex = 7,
                    calendarMetadata = MetricContracts.DescribeCalendar(.2f, .3f, 2026, 12)
                }
            };
            internal CityLaborData LaborData = new CityLaborData
            {
                employed = 3,
                homelessCitizens = 4,
                jobs = new CityJobsData
                {
                    total = 8,
                    free = 2
                }
            };
            internal CityBudgetData BudgetData = new CityBudgetData
            {
                totalIncome = 100,
                totalExpenses = -60,
                totalTaxIncome = 90,
                balance = 41
            };
            internal TransitPage Page;
            public bool Warming { get; set; }

            T Record<T>(string key, T value)
            {
                Calls.Add(key);
                During?.Invoke();
                return value;
            }

            public CityStateData State() => Record("state", StateData);
            public CityOverviewData Overview() => Record("overview", OverviewData);
            public CityLaborData Labor() => Record("labor", LaborData);
            public CityBudgetData Budget() => Record("budget", BudgetData);
            public Observation<TransitPage> SelectedLine(TransitQuery query) => Record("transit", new Observation<TransitPage> { data = Page, world = query.World, startFrame = 12, endFrame = 12, sampledAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), freshness = "warming", warnings = new[] { "nativeTransitFixtureNotRun", "multiFrameReadNotAtomic" } });
        }

        static Observation<MetricBatch> Read(Sources sources, string[] keys, bool transit = false, WorldIdentity world = null, Func<long> frame = null) => new DiagnosticMetricReader(sources, world ?? Ready(), frame ?? (() => 12), () => new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc)).Read(Request(keys, transit));
        static TransitPage Page(TransitWaypoint[] waypoints, TransitSegment[] segments) => TransitReadModel.Project(TransitQuery.ParseArguments(new Dictionary<string, object> { { "line", new Dictionary<string, object> { { "world", new Dictionary<string, object> { { "bootId", "boot" }, { "epoch", 1 } } }, { "index", 10 }, { "version", 1 } } } }), new TransitRootFacts { state = "available", mode = "bus", passenger = true, cargo = false, waypointCount = waypoints.Length, segmentCount = segments.Length }, waypoints, segments);
        static TransitWaypoint Waypoint(bool position = true) => new TransitWaypoint
        {
            entity = Ref(20),
            state = "available",
            connection = Ref(30),
            connectionState = "available",
            position = position ? new Point3(1, 2, 3) : null
        };
        internal static void Run()
        {
            Program.Check("actual native metric interface binding uses original typed overview once", () =>
            {
                var handlers = new CS2MCP.RequestHandlers();
                var result = ((IObservationReaders)handlers).Metrics(Request(new[] { "city.money", "city.population" }));
                Program.Equal(9d, result.data.values["city.money"].value);
                Program.Equal("city.overview", string.Join(",", handlers.BoundCalls));
            });
            Program.Check("catalog exposes exactly sixteen real definitions with no invented units or stock periods", () =>
            {
                var definitions = DiagnosticMetricCatalog.Read();
                Program.Equal(16, definitions.Length);
                Program.Equal(4, definitions.Count(d => d.kind == "rate" && d.period == "month"));
                Program.Equal(true, definitions.Where(d => d.kind != "rate").All(d => d.period == null));
                Program.Equal(false, definitions.Any(d => d.key.Contains("length") || d.unit == "percent"));
                Program.Throws<OperationException>(() => DiagnosticMetricCatalog.Read("*"));
            });
            Program.Check("population presence distinguishes supported zero from legacy absent default without losing money", () =>
            {
                var s = new Sources();
                var keys = new[]
                {
                    "city.population",
                    "city.populationWithMoveIn",
                    "city.money",
                    "city.xp"
                };
                var present = Read(s, keys);
                Program.Equal(0d, present.data.values["city.population"].value);
                Program.Equal("supported", present.data.values["city.population"].support);
                s.OverviewData.PopulationAvailable = false;
                var missing = Read(s, keys);
                Program.Equal(null, missing.data.values["city.population"].value);
                Program.Equal(-12d, missing.data.values["city.money"].value);
                Program.Equal(9d, missing.data.values["city.xp"].value);
                Program.Equal(true, missing.data.sources.Single().warnings.Contains("populationComponentUnavailableLegacyDefaults"));
            });
            Program.Check("each fixed source is called once and monthly native signs and balance survive", () =>
            {
                var s = new Sources();
                var result = Read(s, DiagnosticMetricCatalog.Read().Where(d => !d.key.StartsWith("transit.")).Select(d => d.key).ToArray());
                Program.Equal("overview,labor,budget", string.Join(",", s.Calls));
                Program.Equal(-60d, result.data.values["budget.expenses"].value);
                Program.Equal(41d, result.data.values["budget.balance"].value);
                Program.Equal("month", result.data.values["budget.balance"].period);
                Program.Equal(3d, result.data.values["labor.employed"].value);
                Program.Equal(2d, result.data.values["labor.jobs.free"].value);
                Program.Equal(0, result.data.members.Length);
                Program.Equal(true, result.warnings.Contains("multiSourceReadNotAtomic"));
            });
            Program.Check("missing overview clock falls back to one actual state source", () =>
            {
                var s = new Sources();
                s.OverviewData.calendarMetadata = null;
                var result = Read(s, new[] { "city.money" });
                Program.Equal("overview,state", string.Join(",", s.Calls));
                Program.Equal(true, result.data.clock != null);
            });
            Program.Check("clock and source frames are local and native refresh time remains unknown", () =>
            {
                long next = 10;
                var s = new Sources
                {
                    Warming = true
                };
                var result = Read(s, new[] { "city.money", "budget.income" }, frame: () => next++);
                var source = result.data.sources.First();
                Program.Equal(source.endFrame, result.data.clock.frameIndex);
                Program.Equal(source.startFrame, result.data.clock.startFrame);
                Program.Equal(null, source.refreshTimeUtc);
                Program.Equal("warming", result.freshness);
                Program.Equal("sourceObservationEnd", result.data.clock.frameEvidence);
            });
            Program.Check("nonfinite clock and metric values are unavailable and invalid count never clamps", () =>
            {
                var s = new Sources();
                s.OverviewData.calendarMetadata = MetricContracts.DescribeCalendar(float.NaN, .3f, 2026, 12);
                s.OverviewData.money = double.PositiveInfinity;
                s.OverviewData.population = -1;
                var result = Read(s, new[] { "city.money", "city.population" });
                Program.Equal(null, result.data.clock);
                Program.Equal(null, result.data.values["city.money"].value);
                Program.Equal("invalidNativeCount", result.data.values["city.population"].reason);
            });
            Program.Check("strict scopes keys parameters and duplicates reject before native callbacks", () =>
            {
                Program.Throws<OperationException>(() => Request(new[] { "city.money", "city.money" }));
                Program.Throws<OperationException>(() => Request(new[] { "transit.segmentCount" }));
                Program.Throws<OperationException>(() => Request(new[] { "city.money" }, true));
                Program.Throws<OperationException>(() => Request(new[] { "city.length" }));
                var valid = "{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"scope\":{\"kind\":\"city\"},\"queries\":[{\"key\":\"city.money\",\"parameters\":{}}]}";
                foreach (string input in new[]
                {
                    valid.Replace("\"parameters\":{}", "\"parameters\":{\"unknown\":1}"),
                    valid.Replace("\"kind\":\"city\"", "\"kind\":\"city\",\"area\":{}")
                }

                )
                    Program.Throws<OperationException>(() => MetricReadRequest.ParseArguments(CommandJson.Parse(input)));
                Program.Throws<OperationException>(() => MetricReadRequest.DecodeQuery("query=" + Uri.EscapeDataString(valid) + "&query=x"));
            });
            Program.Check("original request keys are immutable and stale world prevents all source calls", () =>
            {
                var request = Request(new[] { "city.money" });
                request.Keys[0] = "budget.income";
                Program.Equal("city.money", request.Keys[0]);
                var s = new Sources();
                var w = Ready();
                w.BeginLoad();
                w.CompleteLoad();
                Program.Throws<OperationException>(() => Read(s, new[] { "city.money" }, world: w));
                Program.Equal(0, s.Calls.Count);
            });
            Program.Check("mid source load prevents publication rather than converting world failure to unavailable", () =>
            {
                var w = Ready();
                var s = new Sources();
                s.During = () => w.BeginLoad();
                Program.Throws<OperationException>(() => Read(s, new[] { "city.money" }, world: w));
            });
            Program.Check("complete traversal missing PathInformation has no exact invalid segment count", () =>
            {
                var s = new Sources
                {
                    Page = Page(new[] { Waypoint() }, new[] { new TransitSegment { entity = Ref(40), state = "available" } })
                };
                var result = Read(s, new[] { "transit.segmentCount", "transit.invalidSegmentCount" }, true);
                Program.Equal(1d, result.data.values["transit.segmentCount"].value);
                Program.Equal(null, result.data.values["transit.invalidSegmentCount"].value);
                Program.Equal("segmentAssessmentIncomplete", result.data.values["transit.invalidSegmentCount"].reason);
            });
            Program.Check("known invalid plus unassessed distance does not manufacture an exact total", () =>
            {
                var s = new Sources
                {
                    Page = Page(new[] { Waypoint() }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = -1 }, new TransitSegment { entity = Ref(41), state = "available" } })
                };
                Program.Equal(null, Read(s, new[] { "transit.invalidSegmentCount" }, true).data.values["transit.invalidSegmentCount"].value);
            });
            Program.Check("optional positions are not broken references and null fault slots still count", () =>
            {
                var s = new Sources
                {
                    Page = Page(new[] { Waypoint(false) }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = 7 } })
                };
                var noPosition = Read(s, new[] { "transit.brokenWaypointCount" }, true);
                Program.Equal(0d, noPosition.data.values["transit.brokenWaypointCount"].value);
                Program.Equal(null, noPosition.data.memberEvidence.Single().fingerprint);
                s.Page = Page(new[] { new TransitWaypoint { state = "nullRef" } }, new[] { new TransitSegment { state = "nullRef" } });
                var nullSlots = Read(s, new[] { "transit.brokenWaypointCount", "transit.invalidSegmentCount" }, true);
                Program.Equal(1d, nullSlots.data.values["transit.brokenWaypointCount"].value);
                Program.Equal(1d, nullSlots.data.values["transit.invalidSegmentCount"].value);
            });
            Program.Check("nonfinite sanitized distance remains known fault and unknown future assessment refuses", () =>
            {
                var s = new Sources
                {
                    Page = Page(new[] { Waypoint() }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = double.NaN } })
                };
                Program.Equal(1d, Read(s, new[] { "transit.invalidSegmentCount" }, true).data.values["transit.invalidSegmentCount"].value);
                s.Page.items[0].segments[0].structuralAssessment = "futureUnknown";
                Program.Equal(null, Read(s, new[] { "transit.invalidSegmentCount" }, true).data.values["transit.invalidSegmentCount"].value);
            });
            Program.Check("missing mode and purpose prevent a complete structural fingerprint", () =>
            {
                var s = new Sources
                {
                    Page = Page(new[] { Waypoint() }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = 7 } })
                };
                s.Page.items[0].mode = null;
                Program.Equal(null, Read(s, new[] { "transit.segmentCount" }, true).data.memberEvidence.Single().fingerprint);
            });
            Program.Check("oversized typed source is unavailable while independent counters survive", () =>
            {
                var s = new Sources();
                s.OverviewData.cityName = new string ('x', 257);
                var result = Read(s, new[] { "city.money", "labor.employed" });
                Program.Equal(null, result.data.values["city.money"].value);
                Program.Equal(3d, result.data.values["labor.employed"].value);
                Program.Equal("incomplete", result.data.sources.First().completeness);
            });
            Program.Check("nonfinite native count has invalid count reason", () =>
            {
                var s = new Sources();
                s.OverviewData.population = double.NaN;
                Program.Equal("invalidNativeCount", Read(s, new[] { "city.population" }).data.values["city.population"].reason);
            });
            Program.Check("selected line preserves original observation and deterministic qualified fingerprint", () =>
            {
                var s = new Sources
                {
                    Page = Page(new[] { Waypoint() }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = 7 } })
                };
                var result = Read(s, new[] { "transit.segmentCount" }, true);
                var source = result.data.sources.Single(m => m.id == "transit.selectedLine");
                Program.Equal(12L, source.startFrame);
                Program.Equal(12L, source.endFrame);
                Program.Equal("warming", source.freshness);
                Program.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), source.sampledAtUtc);
                Program.Equal(10, result.data.perMember.Single().member.index);
                Program.Equal(64, result.data.memberEvidence.Single().fingerprint.Length);
                Program.Equal("structuralSampleOnly", result.data.memberEvidence.Single().coverage);
                Program.Equal(result.data.memberEvidence.Single().fingerprint, Read(s, new[] { "transit.segmentCount" }, true).data.memberEvidence.Single().fingerprint);
            });
        }
    }
}
