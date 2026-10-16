using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class TransitReadModelTests
    {
        sealed class Source : ITransitSource
        {
            internal TransitRootFacts Root = new TransitRootFacts
            {
                state = "available",
                mode = "train",
                passenger = false,
                cargo = true,
                waypointCount = 1,
                segmentCount = 1
            };
            internal int Reads;
            internal Action During;
            public TransitRootFacts ReadRoot(TransitQuery query) => Root;
            public TransitWaypoint ReadWaypoint(int index)
            {
                Reads++;
                During?.Invoke();
                return new TransitWaypoint
                {
                    entity = Ref(20 + index),
                    state = "available",
                    connection = Ref(30 + index),
                    connectionState = "available",
                    position = new Point3(1, 2, 3)
                };
            }

            public TransitSegment ReadSegment(int index)
            {
                Reads++;
                return new TransitSegment
                {
                    entity = Ref(40 + index),
                    state = "available",
                    rawDistance = 7
                };
            }
        }

        static WorldRef World = new WorldRef("boot", 1);
        static EntityRef Ref(int index) => new EntityRef
        {
            world = World,
            index = index,
            version = 1
        };
        static TransitQuery Query(string extra = "") => TransitQuery.ParseArguments(CommandJson.Parse("{\"line\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":12,\"version\":2}" + extra + "}"));
        static Observation<TransitPage> Read(Source source, TransitQuery query = null, WorldIdentity worlds = null)
        {
            if (worlds == null)
            {
                worlds = new WorldIdentity("boot");
                worlds.BeginLoad();
                worlds.CompleteLoad();
            }

            return TransitReader.Read(query ?? Query(), worlds, () => 10, source);
        }

        public static void Run()
        {
            Program.Check("actual transit GET ingress decoder and manifest preserve bounded partial routing", () =>
            {
                bool accepted=false;
                using(var stream=new MemoryStream(Encoding.ASCII.GetBytes("GET /v2/transit/status?bootId=boot&epoch=1&line=12%3A2 HTTP/1.1\r\n\r\n")))
                    HttpIngress.ReadAndDispatch(stream,r=>{accepted=true;Program.Equal("/v2/transit/status",r.Path);});
                Program.Equal(true,accepted);
                var parse=typeof(CS2MCP.HttpBridgeServer).GetMethod("ParseUrl",BindingFlags.Static|BindingFlags.NonPublic);
                var request=new CS2MCP.BridgeRequest();parse.Invoke(null,new object[]{"/v2/transit/status?bootId=boot&epoch=1&line=12%3A2",request});Program.Equal("12:2",request.Query["line"]);
                foreach(string url in new[]{"/v2/transit/status?bootId=boot&epoch=1&line=12:2&line=13:2","/v2/transit/capabilities?x=1"})
                {
                    try{parse.Invoke(null,new object[]{url,new CS2MCP.BridgeRequest()});throw new Exception("strict transit URL accepted");}
                    catch(TargetInvocationException e){Program.Equal(true,e.InnerException is OperationException);}
                }
                var manifest=CS2MCP.EndpointCatalog.CreateManifest(new BuildIdentity("test","test",null,null),new RuntimeSnapshotStore().Read(DateTime.UtcNow));
                foreach(string capability in new[]{"cs2_transit_status","cs2_transit_capabilities"})
                {
                    Program.Equal("read",CS2MCP.EndpointCatalog.All.Single(e=>e.capability==capability).access);
                    Program.Equal("degraded",manifest.capabilities.Single(e=>e.name==capability).support);
                    Program.Equal("unverified",manifest.nativeSupport.Single(e=>e.name==capability).support);
                }
            });
            Program.Check("combined buffer cap is enforced before any element reads", () =>
            {
                var source = new Source();
                source.Root.waypointCount = 65;
                source.Root.segmentCount = 64;
                var data = Read(source).data.items.Single();
                Program.Equal(0, source.Reads);
                Program.Equal("incomplete", data.completeness.state);
                Program.Equal(null, data.health.rawLength);
                source.Root.waypointCount = 64;
                Read(source);
                Program.Equal(128, source.Reads);
            });
            Program.Check("complete structural facts retain raw unknown-unit length and never prove native validity", () =>
            {
                var data = Read(new Source()).data.items.Single();
                Program.Equal(7d, data.health.rawLength);
                Program.Equal(null, data.health.lengthUnit);
                Program.Equal("unknown", data.health.nativeValidity);
                Program.Equal("unknown", data.health.state);
                Program.Equal("cargo", data.purpose);
                Program.Equal(null, data.load.value);
            });
            Program.Check("unavailable filter evidence retains selected item and null matched even when another filter mismatches", () =>
            {
                var source = new Source();
                source.Root.cargo = null;
                var page = Read(source, Query(",\"mode\":\"airplane\",\"purpose\":\"cargo\"")).data;
                Program.Equal(null, page.matched);
                Program.Equal(1, page.returned);
                Program.Equal("unverifiable", page.filter.state);
                source.Root.cargo = true;
                page = Read(source, Query(",\"mode\":\"airplane\"")).data;
                Program.Equal(0, page.matched);
                Program.Equal(0, page.returned);
            });
            Program.Check("negative missing and nonfinite segment facts do not become usable length", () =>
            {
                var root = new Source().Root;
                foreach (var segment in new[]
                {
                    new TransitSegment
                    {
                        entity = Ref(40),
                        state = "available",
                        rawDistance = -1
                    },
                    new TransitSegment
                    {
                        entity = Ref(40),
                        state = "missing"
                    },
                    new TransitSegment
                    {
                        entity = Ref(40),
                        state = "available",
                        rawDistance = double.NaN
                    }
                }

                )
                {
                    var view = TransitReadModel.Project(Query(), root, new[] { new TransitWaypoint { entity = Ref(20), state = "available", connection = Ref(30), connectionState = "available", position = new Point3(1, 2, 3) } }, new[] { segment }).items.Single();
                    Program.Equal(null, view.health.rawLength);
                    Program.Equal("unknown", view.health.nativeValidity);
                    if (segment.rawDistance.HasValue && double.IsNaN(segment.rawDistance.Value))
                        Program.Equal(null, view.segments[0].rawDistance);
                }
            });
            Program.Check("missing stop and cardinality mismatch invalidate raw length without asserting native parity", () =>
            {
                var root = new Source().Root;
                var waypoint = new TransitWaypoint
                {
                    entity = Ref(20),
                    state = "available",
                    connectionState = "missing"
                };
                var view = TransitReadModel.Project(Query(), root, new[] { waypoint }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = 3 } }).items.Single();
                Program.Equal(null, view.health.rawLength);
                root.segmentCount = 0;
                view = TransitReadModel.Project(Query(), root, new[] { waypoint }, new TransitSegment[0]).items.Single();
                Program.Equal(null, view.health.rawLength);
            });
            Program.Check("missing optional position and path information are uncertainty rather than known faults", () =>
            {
                var root = new Source().Root;
                var view = TransitReadModel.Project(Query(), root, new[] { new TransitWaypoint { entity = Ref(20), state = "available", connection = Ref(30), connectionState = "available" } }, new[] { new TransitSegment { entity = Ref(40), state = "available" } }).items.Single();
                Program.Equal("unknown", view.health.state);
                Program.Equal(0, view.health.brokenWaypoints.Length);
                Program.Equal(0, view.health.invalidSegments.Length);
                Program.Equal(null, view.health.rawLength);
                view = TransitReadModel.Project(Query(), root, new[] { new TransitWaypoint { entity = Ref(20), state = "available", connection = Ref(30), connectionState = "notTransportStop", position = new Point3(1, 2, 3) } }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = 7 } }).items.Single();
                Program.Equal("unknown", view.health.state);
                Program.Equal(0, view.health.brokenWaypoints.Length);
                Program.Equal(null, view.health.rawLength);
            });
            Program.Check("selected transit catalog validates historical full refs before admission and uses real binding", () =>
            {
                string json = "{\"items\":[{\"id\":\"line\",\"name\":\"transit.selectedLine\",\"args\":{\"line\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":12,\"version\":2}}}]}";
                var worlds = new WorldIdentity("boot");
                worlds.BeginLoad();
                worlds.CompleteLoad();
                var readers = new CS2MCP.RequestHandlers();
                var scheduler = new ObservationScheduler(worlds, new ObservationCatalog(readers, () => 10, () => DateTime.UtcNow), () => 10, () => 0, new Encoder());
                Program.Throws<OperationException>(() => scheduler.Admit(ObservationCatalog.Parse(json.Replace("\"epoch\":1", "\"epoch\":0")), _ =>
                {
                }));
                Program.Equal(0, scheduler.Pending);
                Program.Equal(0, scheduler.ActiveGroups);
                Program.Equal(0, readers.BoundCalls.Count);
                var group = scheduler.Admit(ObservationCatalog.Parse(json), _ =>
                {
                });
                scheduler.Pump();
                Program.Equal("transit.selectedLine", readers.BoundCalls.Single());
                Program.Equal("explicitLine", ((TransitPage)group.Result.results[0].observation.data).scope);
                Program.Equal(0, scheduler.Pending);
                group.Detach();
            });
            Program.Check("mixed purpose and inactive stop do not invent cargo units or broken line", () =>
            {
                var root = new Source().Root;
                root.passenger = true;
                var waypoint = new TransitWaypoint
                {
                    entity = Ref(20),
                    state = "available",
                    connection = Ref(30),
                    connectionState = "available",
                    position = new Point3(1, 2, 3),
                    stop = new TransitStopFacts
                    {
                        entity = Ref(30),
                        active = false
                    }
                };
                var view = TransitReadModel.Project(Query(), root, new[] { waypoint }, new[] { new TransitSegment { entity = Ref(40), state = "available", rawDistance = 3 } }).items.Single();
                Program.Equal("mixed", view.purpose);
                Program.Equal(null, view.load.kind);
                Program.Equal("unknown", view.health.state);
            });
            Program.Check("null required slots are known faults without fabricated EntityRefs", () =>
            {
                var root = new Source().Root;
                var view = TransitReadModel.Project(Query(), root, new[] { new TransitWaypoint { state = "nullRef", connectionState = "connectionUnavailable" } }, new[] { new TransitSegment { state = "nullRef" } }).items.Single();
                Program.Equal("invalid", view.health.state);
                Program.Equal(0, view.health.brokenWaypoints.Length);
                Program.Equal(0, view.health.invalidSegments.Length);
                Program.Equal(null, view.health.rawLength);
            });
            Program.Check("full caller world is required and strict GET preserves it", () =>
            {
                foreach (string json in new[]
                {
                    "{\"line\":{\"index\":1,\"version\":1}}",
                    "{\"line\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":1,\"version\":1},\"cursor\":\"x\"}"
                }

                )
                    Program.Throws<OperationException>(() => TransitQuery.ParseArguments(CommandJson.Parse(json)));
                Program.Throws<OperationException>(() => TransitQuery.DecodeQuery("bootId=boot&epoch=1&line=1:1&line=2:1"));
                Program.Throws<OperationException>(() => TransitQuery.DecodeQuery("bootId=%GG&epoch=1&line=1:1"));
                var query = TransitQuery.DecodeQuery("bootId=old%20world&epoch=7&line=12:2");
                Program.Equal("old world", query.World.bootId);
                Program.Equal(7L, query.World.epoch);
            });
            Program.Check("intervening load fences transit publication and immutable query cannot be retargeted", () =>
            {
                var worlds = new WorldIdentity("boot");
                worlds.BeginLoad();
                worlds.CompleteLoad();
                var source = new Source();
                source.During = () => worlds.BeginLoad();
                Program.Throws<OperationException>(() => Read(source, null, worlds));
                var query = Query();
                var detached = query.Line;
                detached.index = 99;
                detached.world = new WorldRef("other", 8);
                Program.Equal(12, query.Line.index);
                Program.Equal("boot", query.World.bootId);
            });
        }

        sealed class Encoder : IObservationEncoder
        {
            public byte[] Item(ObservationItemResult item) => new byte[1];
            public byte[] Batch(ObservationBatchResult batch) => new byte[1];
        }
    }
}
