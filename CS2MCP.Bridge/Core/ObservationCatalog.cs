using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace CS2MCP.Core
{
    public sealed class ObservationItemResult
    {
        public string id { get; set; }
        public Observation<object> observation { get; set; }
        public BridgeErrorData error { get; set; }
    }
    public sealed class ObservationBatchResult
    {
        public WorldRef world { get; set; }
        public long? startFrame { get; set; }
        public long? endFrame { get; set; }
        public bool worldChanged { get; set; }
        public ObservationItemResult[] results { get; set; }
    }
    public sealed class ObservationRead
    {
        public string Id
        {
            get;
        }
        public string Name
        {
            get;
        }
        private readonly Dictionary<string, string> inventory;
        internal ObservationRead(string id, string name, Dictionary<string, string> inventory = null)
        {
            Id = id;
            Name = name;
            this.inventory = inventory;
        }
        internal IDictionary<string, string> Inventory() => inventory == null ? null : new Dictionary<string, string>(inventory, StringComparer.Ordinal);
        internal WorldRef InventoryWorld => inventory == null ? null : ConstructionInventory.Parse(inventory).world;
    }
    public sealed class ValidatedObservationBatch
    {
        internal readonly ObservationRead[] Items; internal readonly WorldRef ExpectedWorld;
        internal ValidatedObservationBatch(ObservationRead[] items, WorldRef expected)
        {
            Items = items;
            ExpectedWorld = expected;
        }
    }
    /// <summary>Only bridge-owned fixed adapters are bound here. No URL/EndpointCatalog dispatch.</summary>
    public interface IObservationReaders
    {
        bool Warming
        {
            get;
        }
        object State(); object Overview(); object Services(); object Labor(); object Budget();
        Observation<ConstructionSnapshot> Inventory(IDictionary<string, string> query);
    }
    public interface IObservationDataWarnings
    {
        string[] ObservationWarnings
        {
            get;
        }
    }
    public sealed class ObservationCatalog
    {
        private readonly IObservationReaders readers; private readonly Func<long> frame; private readonly Func<DateTime> utc;
        public ObservationCatalog(IObservationReaders readers, Func<long> frame, Func<DateTime> utc)
        {
            this.readers = readers;
            this.frame = frame;
            this.utc = utc;
        }
        public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[] { "state", "city.overview", "city.services", "city.labor", "city.budget", "construction.inventory" });
        internal static Dictionary<string, object> Map(object value) => value as Dictionary<string, object> ?? throw CommandJson.Invalid("expected object");
        static void Keys(Dictionary<string, object> value, params string[] allowed)
        {
            if (value.Keys.Any(k => !allowed.Contains(k)))
                throw CommandJson.Invalid("unknown observation field");
        }
        internal static WorldRef World(object value)
        {
            var map = Map(value);
            Keys(map, "bootId", "epoch");
            if (map.Count != 2 || !map.TryGetValue("bootId", out var boot) || !(boot is string text) || string.IsNullOrWhiteSpace(text) || text.Length > 128 ||
                !map.TryGetValue("epoch", out var epoch) || !long.TryParse(CommandJson.Stringify(epoch), NumberStyles.None, CultureInfo.InvariantCulture, out long number) || number > ConstructionInventory.MaxSafeEpoch)
                throw CommandJson.Invalid("invalid world");
            return new WorldRef(text, number);
        }
        static int Integer(object value)
        {
            if (!int.TryParse(CommandJson.Stringify(value), NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number <= 0)
                throw CommandJson.Invalid("invalid entity index/version");
            return number;
        }
        public static ValidatedObservationBatch Parse(string json)
        {
            var root = Map(CommandJson.Parse(json));
            Keys(root, "expectedWorld", "items");
            var expected = root.TryGetValue("expectedWorld", out var world) ? World(world) : null;
            if (!root.TryGetValue("items", out var raw) || !(raw is List<object> list) || list.Count < 1 || list.Count > 64)
                throw CommandJson.Invalid("require 1–64 observation items");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var items = new ObservationRead[list.Count];
            for (int i = 0; i < items.Length; i++)
            {
                var item = Map(list[i]);
                Keys(item, "id", "name", "args");
                if (item.Count != 3 || !item.TryGetValue("id", out var id) || !(id is string text) || text.Length < 1 || text.Length > 128 || !seen.Add(text) ||
                    !item.TryGetValue("name", out var name) || !(name is string key) || !Names.Contains(key) || !item.TryGetValue("args", out var args))
                    throw CommandJson.Invalid("invalid/duplicate observation item identity/name");
                var map = Map(args);
                Dictionary<string, string> compact = null;
                if (key == "construction.inventory")
                {
                    Keys(map, "world", "entities");
                    if (map.Count != 2 || !map.TryGetValue("world", out var entityWorld) || !map.TryGetValue("entities", out var entities) || !(entities is List<object> refs) || refs.Count < 1 || refs.Count > 32)
                        throw CommandJson.Invalid("invalid inventory arguments");
                    var observed = World(entityWorld);
                    var encoded = new List<string>();
                    foreach (var value in refs)
                    {
                        var reference = Map(value);
                        Keys(reference, "world", "index", "version");
                        if (reference.Count != 3 || !reference.TryGetValue("world", out var own) || !WorldIdentity.Same(observed, World(own)) || !reference.TryGetValue("index", out var index) || !reference.TryGetValue("version", out var version))
                            throw CommandJson.Invalid("invalid/mixed entity world");
                        encoded.Add(Integer(index).ToString(CultureInfo.InvariantCulture) + ":" + Integer(version).ToString(CultureInfo.InvariantCulture));
                    }
                    compact = new Dictionary<string, string> { { "bootId", observed.bootId }, { "epoch", observed.epoch.ToString(CultureInfo.InvariantCulture) }, { "entities", string.Join(",", encoded) } };
                    ConstructionInventory.Parse(compact);
                }
                else if (map.Count != 0)
                    throw CommandJson.Invalid("context observation args must be empty");
                items[i] = new ObservationRead(text, key, compact);
            }
            return new ValidatedObservationBatch(items, expected);
        }
        internal Observation<object> Read(ObservationRead item)
        {
            if (item.Name == "construction.inventory")
            {
                var value = readers.Inventory(item.Inventory());
                return new Observation<object> { data = value.data, world = value.world, startFrame = value.startFrame, endFrame = value.endFrame, sampledAtUtc = value.sampledAtUtc, freshness = value.freshness, warnings = value.warnings };
            }
            long start = frame();
            object data;
            switch (item.Name)
            {
                case "state":
                    data = readers.State();
                    break;
                case "city.overview":
                    data = readers.Overview();
                    break;
                case "city.services":
                    data = readers.Services();
                    break;
                case "city.labor":
                    data = readers.Labor();
                    break;
                case "city.budget":
                    data = readers.Budget();
                    break;
                default:
                    throw CommandJson.Invalid("unknown fixed read");
            }
            if (data is IBoundedObservationData bounded)
                bounded.ValidateObservationBounds();
            long end = frame();
            var warnings = new List<string> { "nativeObservationParityNotRun" };
            if (item.Name != "state")
                warnings.Add("cachedSourceRefreshTimeUnknown");
            if (start != end)
                warnings.Add("multiFrameReadNotAtomic");
            if (data is IObservationDataWarnings evidence)
                warnings.AddRange(evidence.ObservationWarnings);
            return new Observation<object> { data = data, startFrame = start, endFrame = end, sampledAtUtc = utc(), freshness = readers.Warming ? "warming" : "current", warnings = warnings.ToArray() };
        }
    }
}
