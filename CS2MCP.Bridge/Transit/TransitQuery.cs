using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CS2MCP.Core
{
    /// <summary>Immutable caller-observed identity; GET is only its compact transport encoding.</summary>
    public sealed class TransitQuery
    {
        public WorldRef World { get; }
        public int Index { get; }
        public int Version { get; }
        public string Mode { get; }
        public string Purpose { get; }
        public EntityRef Line => new EntityRef
        {
            world = World,
            index = Index,
            version = Version
        };

        private TransitQuery(WorldRef world, int index, int version, string mode, string purpose)
        {
            World = world;
            Index = index;
            Version = version;
            Mode = mode;
            Purpose = purpose;
        }

        static Dictionary<string, object> Map(object value) => value as Dictionary<string, object> ?? throw CommandJson.Invalid("expected transit object");
        static void Keys(Dictionary<string, object> value, params string[] allowed)
        {
            if (value.Keys.Any(key => !allowed.Contains(key)))
                throw CommandJson.Invalid("unknown transit field");
        }

        static int Positive(object value)
        {
            if (!int.TryParse(CommandJson.Stringify(value), NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number <= 0)
                throw CommandJson.Invalid("invalid transit entity index/version");
            return number;
        }

        static string Filter(Dictionary<string, object> map, string key, string[] allowed)
        {
            if (!map.TryGetValue(key, out object raw))
                return null;
            if (!(raw is string text) || !allowed.Contains(text))
                throw CommandJson.Invalid("invalid transit " + key);
            return text;
        }

        public static TransitQuery ParseArguments(object value)
        {
            var map = Map(value);
            Keys(map, "line", "mode", "purpose");
            if (!map.TryGetValue("line", out object raw))
                throw CommandJson.Invalid("full observed line EntityRef required");
            var line = Map(raw);
            Keys(line, "world", "index", "version");
            if (line.Count != 3 || !line.TryGetValue("world", out object world) || !line.TryGetValue("index", out object index) || !line.TryGetValue("version", out object version))
                throw CommandJson.Invalid("full observed line EntityRef required");
            return new TransitQuery(ObservationCatalog.World(world), Positive(index), Positive(version), Filter(map, "mode", new[] { "bus", "tram", "metro", "train", "airplane" }), Filter(map, "purpose", new[] { "passenger", "cargo" }));
        }

        public static TransitQuery Parse(IDictionary<string, string> query)
        {
            if (query == null || query.Keys.Any(k => k != "bootId" && k != "epoch" && k != "line" && k != "mode" && k != "purpose") || !query.TryGetValue("bootId", out string boot) || !query.TryGetValue("epoch", out string epoch) || !query.TryGetValue("line", out string line))
                throw CommandJson.Invalid("require observed bootId/epoch and exact line");
            var pair = line.Split(':');
            if (pair.Length != 2 || !long.TryParse(epoch, NumberStyles.None, CultureInfo.InvariantCulture, out long number))
                throw CommandJson.Invalid("invalid compact transit identity");
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
                                    boot
                                },
                                {
                                    "epoch",
                                    number
                                }
                            }
                        },
                        {
                            "index",
                            ParseInt(pair[0])
                        },
                        {
                            "version",
                            ParseInt(pair[1])
                        }
                    }
                }
            };
            if (query.TryGetValue("mode", out string mode))
                args.Add("mode", mode);
            if (query.TryGetValue("purpose", out string purpose))
                args.Add("purpose", purpose);
            return ParseArguments(args);
        }

        static int ParseInt(string text)
        {
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value <= 0)
                throw CommandJson.Invalid("invalid compact transit entity");
            return value;
        }

        public static TransitQuery DecodeQuery(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 4096)
                throw CommandJson.Invalid("missing or oversized transit query");
            var query = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string part in text.Split('&'))
            {
                int split = part.IndexOf('=');
                if (split <= 0)
                    throw CommandJson.Invalid("malformed transit query");
                for (int i = 0; i < part.Length; i++)
                {
                    if (part[i] != '%')
                        continue;
                    if (i + 2 >= part.Length || !Uri.IsHexDigit(part[i + 1]) || !Uri.IsHexDigit(part[i + 2]))
                        throw CommandJson.Invalid("malformed transit escape");
                    i += 2;
                }

                string key = Uri.UnescapeDataString(part.Substring(0, split));
                if (query.ContainsKey(key))
                    throw CommandJson.Invalid("duplicate transit query field");
                query.Add(key, Uri.UnescapeDataString(part.Substring(split + 1)));
            }

            return Parse(query);
        }

        public Dictionary<string, string> Compact()
        {
            var query = new Dictionary<string, string>
            {
                {
                    "bootId",
                    World.bootId
                },
                {
                    "epoch",
                    World.epoch.ToString(CultureInfo.InvariantCulture)
                },
                {
                    "line",
                    Index.ToString(CultureInfo.InvariantCulture) + ":" + Version.ToString(CultureInfo.InvariantCulture)
                }
            };
            if (Mode != null)
                query.Add("mode", Mode);
            if (Purpose != null)
                query.Add("purpose", Purpose);
            return query;
        }
    }
}
