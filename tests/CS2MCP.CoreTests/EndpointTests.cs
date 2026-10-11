using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class EndpointTests
    {
        private static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        private static Type CoreType(string name) => typeof(Capability).Assembly.GetType("CS2MCP.Core." + name)
            ?? throw new Exception(name + " is missing");

        public static void Run()
        {
            Program.Check("embedded build revision is available without reading the checkout", () =>
            {
                const string revision = "0123456789abcdef0123456789abcdef01234567";
                var identity = new BuildIdentity("0.4.0", "0.9.0", "0.4.0+" + revision, null);
                Program.Equal(revision, identity.sourceRevision);
                Program.Equal(null, identity.sourceRevisionReason);
                Program.Equal(null, new BuildIdentity("0.4.0", "0.9.0", "0.4.0+unverified-label", null).sourceRevision);
                Program.Equal("explicit-revision", new BuildIdentity("0.4.0", "0.9.0", "0.4.0+" + revision, "explicit-revision").sourceRevision);
            });
            Program.Check("manifest separates bridge implementation from transient readiness and native validation", () =>
            {
                var type = typeof(Capability).Assembly.GetType("CS2MCP.EndpointCatalog")
                    ?? throw new Exception("endpoint catalog is missing");
                var store = new RuntimeSnapshotStore();
                var now = DateTime.UtcNow;
                foreach (var observation in new[] { new[] { "MainMenu", "False", "False", "gameNotLoaded" },
                    new[] { "Game", "True", "False", "gameLoading" }, new[] { "Game", "False", "True", "nativeFixtureNotRun" } })
                {
                    store.Publish(now, observation[0], bool.Parse(observation[1]), bool.Parse(observation[2]), null);
                    var manifest = type.GetMethod("CreateManifest").Invoke(null, new object[] {
                        new BuildIdentity("0.4.0", "0.9.0", null, null), store.Read(now) });
                    Program.Equal(1, Property(manifest, "protocolVersion"));
                    Program.Equal(1, Property(manifest, "schemaVersion"));
                    var identity = Property(manifest, "identity");
                    Program.Equal("0.4.0", Property(identity, "assemblyVersion"));
                    Program.Equal("0.9.0", Property(identity, "modVersion"));
                    Program.Equal(null, Property(identity, "gameVersion"));
                    Program.Equal("gameVersionNotObserved", Property(identity, "gameVersionReason"));
                    Program.Equal(null, Property(identity, "sourceRevision"));
                    Program.Equal("sourceRevisionUnavailable", Property(identity, "sourceRevisionReason"));
                    var bridge = ((IEnumerable)Property(manifest, "capabilities")).Cast<Capability>().ToArray();
                    Program.Equal(66, bridge.Length);
                    Program.Equal("supported", bridge.Single(item => item.name == "cs2_build_road").support);
                    var native = ((IEnumerable)Property(manifest, "nativeSupport")).Cast<Capability>().ToArray();
                    Program.Equal("unverified", native.Single(item => item.name == "cs2_build_road").support);
                    Program.Equal("unsupported", native.Single(item => item.name == "transitPassengerAirCreation").support);
                    if (!((IEnumerable<string>)Property(manifest, "degradedReasons")).Contains(observation[3]))
                        throw new Exception("missing readiness/validation reason: " + observation[3]);
                }
            });
            Program.Check("listener snapshot has null native evidence before a main-thread observation", () =>
            {
                var type = CoreType("RuntimeSnapshotStore");
                var store = Activator.CreateInstance(type);
                var evidence = type.GetMethod("Read").Invoke(store, new object[] { DateTime.UtcNow });
                Program.Equal(null, Property(evidence, "gameMode"));
                Program.Equal(null, Property(evidence, "isLoading"));
                Program.Equal(null, Property(evidence, "cityLoaded"));
                Program.Equal("noMainThreadObservation", Property(evidence, "reason"));
            });
            Program.Check("snapshot expires when main-thread updates stop and clears native facts", () =>
            {
                var type = CoreType("RuntimeSnapshotStore");
                var store = Activator.CreateInstance(type);
                var now = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
                type.GetMethod("Publish").Invoke(store, new object[] { now, "Game", false, true, null });
                var fresh = type.GetMethod("Read").Invoke(store, new object[] { now.AddSeconds(1) });
                Program.Equal("Game", Property(fresh, "gameMode"));
                Program.Equal(false, Property(fresh, "isLoading"));
                Program.Equal(true, Property(fresh, "cityLoaded"));
                var boundary = type.GetMethod("Read").Invoke(store, new object[] { now.AddMilliseconds(5000) });
                Program.Equal("Game", Property(boundary, "gameMode"));
                var backwardsClock = type.GetMethod("Read").Invoke(store, new object[] { now.AddMilliseconds(-1) });
                Program.Equal("mainThreadObservationStale", Property(backwardsClock, "reason"));
                Program.Equal(null, Property(backwardsClock, "gameMode"));
                var stale = type.GetMethod("Read").Invoke(store, new object[] { now.AddSeconds(6) });
                Program.Equal("mainThreadObservationStale", Property(stale, "reason"));
                Program.Equal(null, Property(stale, "gameMode"));
                Program.Equal(null, Property(stale, "isLoading"));
                Program.Equal(null, Property(stale, "cityLoaded"));
                Program.Equal(6000d, Property(stale, "ageMilliseconds"));
                type.GetMethod("Publish").Invoke(store, new object[] { now.AddSeconds(7), "MainMenu", false, false, null });
                var unloaded = type.GetMethod("Read").Invoke(store, new object[] { now.AddSeconds(7) });
                Program.Equal("MainMenu", Property(unloaded, "gameMode"));
                Program.Equal(false, Property(unloaded, "cityLoaded"));
            });
            Program.Check("unavailable native observation never substitutes false loading evidence", () =>
            {
                var type = CoreType("RuntimeSnapshotStore");
                var store = Activator.CreateInstance(type);
                var now = DateTime.UtcNow;
                type.GetMethod("Publish").Invoke(store, new object[] { now, null, null, null, "gameManagerUnavailable" });
                var evidence = type.GetMethod("Read").Invoke(store, new object[] { now });
                Program.Equal(null, Property(evidence, "isLoading"));
                Program.Equal("gameManagerUnavailable", Property(evidence, "reason"));
            });
            Program.Check("every dispatch route has exactly one catalog classification", () =>
            {
                var type = typeof(Capability).Assembly.GetType("CS2MCP.EndpointCatalog");
                if (type == null) throw new Exception("endpoint catalog is missing");
                var all = ((IEnumerable)type.GetProperty("All").GetValue(null)).Cast<object>().ToArray();
                var byPath = all.ToDictionary(item => (string)Property(item, "path"));
                string source = File.ReadAllText("CS2MCP.Bridge/RequestHandlers.cs");
                var routes = Regex.Matches(source, "case \"([^\"]+)\":").Cast<Match>().Select(match => match.Groups[1].Value)
                    .Concat(Regex.Matches(File.ReadAllText("CS2MCP.Bridge/HttpBridgeServer.cs"), "request.Path == \"([^\"]+)\"")
                        .Cast<Match>().Select(match => match.Groups[1].Value)).Distinct().ToArray();
                Program.Equal(66, routes.Length);
                Program.Equal(66, byPath.Count);
                foreach (string path in routes)
                {
                    if (!byPath.ContainsKey(path)) throw new Exception("missing classification: " + path);
                    var endpoint = byPath[path];
                    if (!new[] { "read", "cityWrite", "uiWrite", "nativePreview", "control" }.Contains((string)Property(endpoint, "access")))
                        throw new Exception("invalid access classification: " + path);
                    Program.Equal("GET", string.Join(",", (IEnumerable<string>)Property(endpoint, "legacyHttpVerbs")));
                    if (string.IsNullOrEmpty((string)Property(endpoint, "capability"))) throw new Exception("missing capability: " + path);
                    if (string.IsNullOrEmpty((string)Property(endpoint, "completionMechanism"))) throw new Exception("missing completion: " + path);
                }
                foreach (var expected in new[] {
                    new[] { "/city/loan/set", "cityWrite", "sync-set" },
                    new[] { "/camera/set", "uiWrite", "sync-set" },
                    new[] { "/game/save", "cityWrite", "save-started" },
                    new[] { "/transit/lines/policies/set", "cityWrite", "end-frame-ack" },
                    new[] { "/city/tiles/buy", "cityWrite", "purchase-readback+refresh-queued" },
                    new[] { "/build/place/roadside", "cityWrite", "tool-apply-ack" },
                    new[] { "/build/place/shoreline", "cityWrite", "tool-apply-ack" },
                    new[] { "/sim/run", "control", "run-scheduled" },
                    new[] { "/capabilities", "read", "off-thread-snapshot" },
                })
                {
                    Program.Equal(expected[1], (string)Property(byPath[expected[0]], "access"));
                    Program.Equal(expected[2], (string)Property(byPath[expected[0]], "completionMechanism"));
                }
            });
        }
    }
}
