using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;

internal static class DiagnosticMetricWireTests
{
    static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual))
            throw new Exception($"expected {expected}, got {actual}");
    }

    static readonly string City = "{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"scope\":{\"kind\":\"city\"},\"queries\":[{\"key\":\"city.money\",\"parameters\":{}},{\"key\":\"city.population\",\"parameters\":{}}]}";
    static readonly string Line = "{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"scope\":{\"kind\":\"selectedLine\",\"line\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":10,\"version\":1}},\"queries\":[{\"key\":\"transit.segmentCount\",\"parameters\":{}},{\"key\":\"transit.invalidSegmentCount\",\"parameters\":{}}]}";
    static IngressRequest Get(string path)
    {
        IngressRequest parsed = null;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("GET " + path + " HTTP/1.1\r\nHost: localhost\r\n\r\n")))
            HttpIngress.ReadAndDispatch(stream, p => parsed = p);
        return parsed;
    }

    static BridgeResponse Invoke(RequestHandlers handler, string method, BridgeRequest request) => (BridgeResponse)typeof(RequestHandlers).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(handler, new object[] { request });
    static void Emit(string name, object value)
    {
        string directory = Environment.GetEnvironmentVariable("M4_WIRE_DIR");
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".json"), ObservationWire.Standalone(value));
        }
    }

    sealed class ClockSources : IDiagnosticMetricSources
    {
        internal long Frame = 10;
        internal bool BadOverview, InvalidClock;
        public bool Warming => false;

        public CityOverviewData Overview()
        {
            Frame++;
            return new CityOverviewData
            {
                PopulationAvailable = true,
                money = 2,
                cityName = BadOverview ? new string ('x', 257) : null
            };
        }

        public CityStateData State()
        {
            Frame++;
            return new CityStateData
            {
                simulation = new CitySimulationData
                {
                    calendarMetadata = MetricContracts.DescribeCalendar(InvalidClock ? float.NaN : .2f, .3f, 2026, 12)
                }
            };
        }

        public CityLaborData Labor() => throw new Exception("unrequested labor read");
        public CityBudgetData Budget() => throw new Exception("unrequested budget read");
        public Observation<TransitPage> SelectedLine(TransitQuery query) => throw new Exception("unrequested transit read");
    }

    internal static void Run(Action<string, Action> check)
    {
        check("metric ingress validates GET and fixed O7 args before callback", () =>
        {
            foreach (string bad in new[]
            {
                "/v2/metrics",
                "/v2/metrics?unknown=x",
                "/v2/metrics?query=%xx",
                "/v2/metrics?query=" + Uri.EscapeDataString(City) + "&query=x",
                "/v2/metrics/catalog?family=unknown",
                "/v2/metrics/catalog?envelope=1"
            }

            )
            {
                bool rejected = false;
                try
                {
                    Get(bad);
                }
                catch (OperationException)
                {
                    rejected = true;
                }

                Equal(true, rejected);
            }

            var parsed = Get("/v2/metrics?query=" + Uri.EscapeDataString(City));
            Equal("city.money", parsed.Metrics.Keys[0]);
            foreach (string method in new[]
            {
                "POST",
                "DELETE"
            }

            )
            {
                bool rejected = false;
                try
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(method + " /v2/metrics HTTP/1.1\r\nContent-Length: 0\r\n\r\n")))
                        HttpIngress.ReadAndDispatch(stream, _ => throw new Exception("enqueued invalid metric verb"));
                }
                catch (OperationException)
                {
                    rejected = true;
                }

                Equal(true, rejected);
            }

            string body = "{\"items\":[{\"id\":\"m\",\"name\":\"metrics.read\",\"args\":" + City + "}]}";
            Equal(true, ObservationCatalog.Parse(body) != null);
            bool invalid = false;
            try
            {
                ObservationCatalog.Parse(body.Replace("\"parameters\":{}", "\"parameters\":{\"field\":1}"));
            }
            catch (OperationException)
            {
                invalid = true;
            }

            Equal(true, invalid);
        });
        check("actual GET facade and native O7 binding share one producer and serializer", () =>
        {
            foreach (var pair in new[]
            {
                new[]
                {
                    "city",
                    City
                },
                new[]
                {
                    "line",
                    Line
                }
            }

            )
            {
                var parsed = Get("/v2/metrics?query=" + Uri.EscapeDataString(pair[1]));
                var handlers = new RequestHandlers();
                var response = Invoke(handlers, "MetricsRequest", new BridgeRequest { Metrics = parsed.Metrics });
                Equal(200, response.Status);
                var direct = JObject.Parse(Encoding.UTF8.GetString(response.Body));
                var batch = ((IObservationReaders)new RequestHandlers()).Metrics(parsed.Metrics);
                var wire = JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(batch)));
                Equal(true, JToken.DeepEquals(direct["data"], wire["data"]["values"]));
                Equal(JTokenType.Integer, wire["data"]["schemaVersion"].Type);
                Equal(JTokenType.Null, wire["data"]["sources"][0]["refreshTimeUtc"].Type);
                if (pair[0] == "city")
                {
                    Equal(JTokenType.Null, wire["data"]["scope"]["line"].Type);
                    Equal(9d, (double)wire["data"]["values"]["city.money"]["value"]);
                    Equal("sourceObservationEnd", (string)wire["data"]["clock"]["frameEvidence"]);
                }

                Emit(pair[0], batch);
                Emit(pair[0] + "-request", JObject.Parse(pair[1]));
                Emit(pair[0] + "-direct", JObject.Parse(Encoding.UTF8.GetString(response.Body)));
            }
        });
        check("actual serialized fallback failed-source and invalid clocks retain explicit origin and null evidence", () =>
        {
            foreach (string name in new[]
            {
                "fallback",
                "failed-source",
                "invalid-clock"
            }

            )
            {
                var sources = new ClockSources
                {
                    BadOverview = name == "failed-source",
                    InvalidClock = name == "invalid-clock"
                };
                var request = MetricReadRequest.ParseArguments(CommandJson.Parse(City));
                var result = new DiagnosticMetricReader(sources, Mod.Worlds, () => sources.Frame, () => new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc)).Read(request);
                var wire = JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(result)));
                if (name == "invalid-clock")
                    Equal(JTokenType.Null, wire["data"]["clock"].Type);
                else
                {
                    Equal("state", (string)wire["data"]["clock"]["sourceObservationId"]);
                    Equal(12L, (long)wire["data"]["clock"]["endFrame"]);
                }

                if (name == "failed-source")
                {
                    Equal(JTokenType.Null, wire["data"]["values"]["city.money"]["value"].Type);
                    Equal("unavailable", (string)wire["data"]["sources"][0]["freshness"]);
                }

                Emit(name, result);
            }
        });
        check("actual O7 scheduler consumes native metric binding and serializes one fixed item", () =>
        {
            var handlers = new RequestHandlers();
            byte[] bytes = null;
            var scheduler = new ObservationScheduler(Mod.Worlds, new ObservationCatalog(handlers, () => 10, () => DateTime.UtcNow), () => 10, () => 0, new ObservationWire());
            scheduler.Admit(ObservationCatalog.Parse("{\"items\":[{\"id\":\"m\",\"name\":\"metrics.read\",\"args\":" + City + "}]}"), value => bytes = value);
            scheduler.Pump();
            var wire = JObject.Parse(Encoding.UTF8.GetString(bytes));
            Equal(9d, (double)wire["results"][0]["observation"]["data"]["values"]["city.money"]["value"]);
            Equal("city.overview", string.Join(",", handlers.BoundCalls));
            Equal(0, scheduler.Pending);
        });
        check("actual HTTP server carries normalized metric request to GET facade", () =>
        {
            var handlers = new RequestHandlers();
            var server = new HttpBridgeServer(0, request =>
            {
                request.Complete(Invoke(handlers, "MetricsRequest", request));
                return true;
            }, _ => throw new Exception("writer router used"), Mod.Worlds);
            server.Start();
            try
            {
                var listener = (TcpListener)typeof(HttpBridgeServer).GetField("m_Listener", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(server);
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                    byte[] request = Encoding.UTF8.GetBytes("GET /v2/metrics?query=" + Uri.EscapeDataString(City) + " HTTP/1.1\r\nHost: localhost\r\n\r\n");
                    client.GetStream().Write(request, 0, request.Length);
                    using (var reader = new StreamReader(client.GetStream()))
                    {
                        string response = reader.ReadToEnd();
                        Equal(true, response.StartsWith("HTTP/1.1 200"));
                        var wire = JObject.Parse(response.Substring(response.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4));
                        Equal(9d, (double)wire["data"]["city.money"]["value"]);
                    }
                }
            }
            finally
            {
                server.Stop();
            }
        });
        check("catalog preserves array compatibility and adds managed availability freshness only on request", () =>
        {
            var handlers = new RequestHandlers();
            var plain = Invoke(handlers, "MetricCatalogRequest", new BridgeRequest { MetricCatalog = Get("/v2/metrics/catalog").MetricCatalog });
            var array = JArray.Parse(Encoding.UTF8.GetString(plain.Body));
            Equal(26, array.Count);
            var envelope = Invoke(handlers, "MetricCatalogRequest", new BridgeRequest { MetricCatalog = Get("/v2/metrics/catalog?envelope=true&family=budget").MetricCatalog });
            var value = JObject.Parse(Encoding.UTF8.GetString(envelope.Body));
            Equal(4, ((JArray)value["definitions"]).Count);
            Equal("current", (string)value["freshness"]);
            Equal("managedReadinessOnlyNativeParityNotRun", (string)value["freshnessReason"]);
            Emit("catalog", array);
            Emit("catalog-envelope", value);
        });
        check("metric normalized GET route bypasses permissive legacy query parsing", () =>
        {
            var parsed = Get("/v2/metrics?query=" + Uri.EscapeDataString(City));
            var request = new BridgeRequest
            {
                Path = "/v2/metrics",
                Metrics = parsed.Metrics
            };
            typeof(HttpBridgeServer).GetMethod("ParseUrl", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "/v2/metrics?arbitrary=ignored", request });
            Equal(0, request.Query.Count);
            Equal("city.money", request.Metrics.Keys[0]);
        });
        check("metric original world rejects O7 admission even without expectedWorld", () =>
        {
            var worlds = new WorldIdentity("new-boot");
            worlds.BeginLoad();
            worlds.CompleteLoad();
            var scheduler = new ObservationScheduler(worlds, new ObservationCatalog(new RequestHandlers(), () => 10, () => DateTime.UtcNow), () => 10, () => 0, new ObservationWire());
            bool refused = false;
            try
            {
                scheduler.Admit(ObservationCatalog.Parse("{\"items\":[{\"id\":\"m\",\"name\":\"metrics.read\",\"args\":" + City + "}]}"), _ =>
                {
                });
            }
            catch (OperationException)
            {
                refused = true;
            }

            Equal(true, refused);
            Equal(0, scheduler.Pending);
        });
        check("production metric serializer caps hostile source diagnostics without leaking oversized response", () =>
        {
            bool refused = false;
            try
            {
                ObservationWire.Standalone(new { warning = new string ('雪', 100000) });
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            Equal(true, refused);
        });
    }
}
