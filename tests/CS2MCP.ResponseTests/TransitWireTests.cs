using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;

internal static class TransitWireTests
{
    static TransitQuery Query(WorldRef world) => TransitQuery.ParseArguments(new Dictionary<string, object> { { "line", new Dictionary<string, object> { { "world", new Dictionary<string, object> { { "bootId", world.bootId }, { "epoch", world.epoch } } }, { "index", 1 }, { "version", 1 } } } });
    static Observation<TransitPage> Sample(WorldRef world, int waypointCount = 64, int segmentCount = 64)
    {
        EntityRef Ref(int index) => new EntityRef
        {
            world = world,
            index = index,
            version = 1
        };
        var root = new TransitRootFacts
        {
            state = "available",
            mode = "train",
            passenger = true,
            cargo = false,
            waypointCount = waypointCount,
            segmentCount = segmentCount,
            owner = Ref(2),
            assetKey = new string ('雪', 256)
        };
        var waypoints = Enumerable.Range(0, waypointCount).Select(i => new TransitWaypoint { entity = Ref(10 + i), state = "available", connection = Ref(100 + i), connectionState = "available", position = new Point3(1, 2, 3), stop = new TransitStopFacts { entity = Ref(100 + i), owner = Ref(3), attachedParent = Ref(4), purposeFlags = new TransitPurposeFlags(), position = new Point3(1, 2, 3) } }).ToArray();
        var segments = Enumerable.Range(0, segmentCount).Select(i => new TransitSegment { entity = Ref(200 + i), state = "available", rawDistance = -1 }).ToArray();
        return new Observation<TransitPage>
        {
            world = world,
            startFrame = 10,
            endFrame = 10,
            sampledAtUtc = DateTime.UtcNow,
            freshness = "current",
            warnings = new[]
            {
                "nativeTransitFixtureNotRun"
            },
            data = TransitReadModel.Project(Query(world), root, waypoints, segments)
        };
    }

    sealed class Readers : IObservationReaders
    {
        internal Observation<TransitPage> Value;
        public bool Warming => false;

        public object State() => throw new Exception("unexpected state reader");
        public object Overview() => State();
        public object Services() => State();
        public object Labor() => State();
        public object Budget() => State();
        public Observation<ConstructionSnapshot> Inventory(IDictionary<string, string> query) => throw new Exception("unexpected inventory");
        public Observation<TransitPage> Transit(TransitQuery query) => Value;
    }

    internal static void Run(Action<string, Action> check)
    {
        check("real128-entry transit response preserves raw unknown units capabilities and full world refs", () =>
        {
            var world = new WorldRef("3cc6a1d0-50ad-4cb8-9cd2-5e6442df3bdb", 1);
            var value = Sample(world);
            byte[] bytes = ObservationWire.Standalone(value);
            if (bytes.Length > 262144)
                throw new Exception("realistic selected transit exceeds wire cap");
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));
            if ((int)json["data"]["items"][0]["completeness"]["readWaypoints"] != 64 || json["data"]["items"][0]["health"]["lengthUnit"].Type != JTokenType.Null || (string)json["data"]["items"][0]["health"]["nativeValidity"] != "unknown" || (string)json["data"]["capabilities"]["support"] != "partial")
                throw new Exception("transit qualification lost");
            if ((string)json["data"]["items"][0]["waypoints"][0]["stop"]["owner"]["world"]["bootId"] != world.bootId)
                throw new Exception("nested full world lost");
        });
        check("adversarial escaped worlds refuse oversized GET and produce one bounded O7 item error", () =>
        {
            var worlds = new WorldIdentity(new string ('\u0001', 128));
            worlds.BeginLoad();
            worlds.CompleteLoad();
            var value = Sample(worlds.Current);
            bool refused = false;
            try
            {
                ObservationWire.Standalone(value);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            if (!refused)
                throw new Exception("oversized transit response escaped GET byte cap");
            var args = new Dictionary<string, object>
            {
                {
                    "line",
                    new Dictionary<string, object>
                    {
                        {
                            "world",
                            new Dictionary<string, object>
                            {
                                {
                                    "bootId",
                                    worlds.Current.bootId
                                },
                                {
                                    "epoch",
                                    worlds.Current.epoch
                                }
                            }
                        },
                        {
                            "index",
                            1
                        },
                        {
                            "version",
                            1
                        }
                    }
                }
            };
            string body = CommandJson.Stringify(new Dictionary<string, object> { { "items", new object[] { new Dictionary<string, object> { { "id", "route" }, { "name", "transit.selectedLine" }, { "args", args } } } } });
            byte[] result = null;
            var readers = new Readers
            {
                Value = value
            };
            var wire = new ObservationWire();
            var scheduler = new ObservationScheduler(worlds, new ObservationCatalog(readers, () => 10, () => DateTime.UtcNow), () => 10, () => 0, wire);
            var group = scheduler.Admit(ObservationCatalog.Parse(body), bytes => result = bytes);
            scheduler.Pump();
            if (group.Result.results.Length != 1 || group.Result.results[0].error?.code != "RESULT_TOO_LARGE" || result.Length > 262144)
                throw new Exception("oversized selected transit was not an explicit bounded error");
            group.Detach();
        });
        check("skewed128-waypoint responses retain independent extraction and actual UTF8 limits", () =>
        {
            var ordinary = Sample(new WorldRef("3cc6a1d0-50ad-4cb8-9cd2-5e6442df3bdb", 1), 128, 0);
            byte[] bytes = ObservationWire.Standalone(ordinary);
            if (bytes.Length > 262144 || ordinary.data.items[0].waypoints.Length != 128)
                throw new Exception("skewed normal response failed");
            bool refused = false;
            try
            {
                ObservationWire.Standalone(Sample(new WorldRef(new string ('\u0001', 128), 1), 128, 0));
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            if (!refused)
                throw new Exception("skewed escaped response exceeded actual byte cap");
        });
    }
}
