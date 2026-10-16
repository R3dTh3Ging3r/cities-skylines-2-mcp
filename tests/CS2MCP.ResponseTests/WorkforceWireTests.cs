using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;

// Exact producer/handler/serializer with controlled typed labor boundary data.
// ReadLaborData's unchanged Game field assignments are source-inspected, not
// linked to Game DLLs here. No native getter-count or runtime-binding claim.
internal static class WorkforceWireTests
{
    static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Exception($"expected {expected}, got {actual}");
    }

    internal static void Run(Action<string, Action> check)
    {
        check("workforce direct and O7 production paths serialize all ten typed counters and original State clock", () =>
        {
            var tiers = new[] { "uneducated", "poorlyEducated", "educated", "wellEducated", "highlyEducated" };
            var keys = new[] { "total", "free" }.SelectMany(kind => tiers.Select(tier => "labor.jobs." + kind + "ByEducation." + tier)).ToArray();
            var request = new JObject
            {
                ["world"] = new JObject { ["bootId"] = "boot", ["epoch"] = 1 },
                ["scope"] = new JObject { ["kind"] = "city" },
                ["queries"] = new JArray(keys.Select(key => new JObject { ["key"] = key, ["parameters"] = new JObject() }))
            };
            var handlers = new RequestHandlers
            {
                TestLaborData = new CityLaborData
                {
                    jobs = new CityJobsData
                    {
                        total = 987, free = 654,
                        totalByEducation = new EducationCounters { uneducated = 11, poorlyEducated = 12, educated = 13, wellEducated = 14, highlyEducated = 15 },
                        freeByEducation = new EducationCounters { uneducated = 1, poorlyEducated = 2, educated = 3, wellEducated = 4, highlyEducated = 0 }
                    }
                },
                TestStateData = new CityStateData { simulation = new CitySimulationData { calendarMetadata = MetricContracts.DescribeCalendar(.2f, .3f, 2026, 12) } }
            };
            var normalized = MetricReadRequest.DecodeQuery("query=" + Uri.EscapeDataString(request.ToString()));
            var response = (BridgeResponse)typeof(RequestHandlers).GetMethod("MetricsRequest", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(handlers, new object[] { new BridgeRequest { Metrics = normalized } });
            Equal(200, response.Status);
            Equal("city.labor,state", string.Join(",", handlers.BoundCalls));
            var direct = JObject.Parse(Encoding.UTF8.GetString(response.Body));
            handlers.BoundCalls.Clear();
            var catalog = new ObservationCatalog(handlers, () => 10, () => new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc));
            var groupRequest = new JObject { ["expectedWorld"] = request["world"].DeepClone(), ["items"] = new JArray(new JObject { ["id"] = "workforce", ["name"] = "metrics.read", ["args"] = request.DeepClone() }) };
            byte[] groupBytes = null;
            var scheduler = new ObservationScheduler(Mod.Worlds, catalog, () => 10, () => 0, new ObservationWire());
            var lease = scheduler.Admit(ObservationCatalog.Parse(groupRequest.ToString()), bytes => groupBytes = bytes);
            scheduler.Pump(); lease.Detach();
            var group = JObject.Parse(Encoding.UTF8.GetString(groupBytes));
            var observation = (JObject)group["results"][0]["observation"];
            Equal("city.labor,state", string.Join(",", handlers.BoundCalls));
            Equal(true, JToken.DeepEquals(direct["data"], observation["data"]["values"]));
            Equal("state", (string)observation["data"]["clock"]["sourceObservationId"]);
            Equal("diagnosticMetrics.v1", (string)observation["data"]["definitionRevision"]);
            for (int i = 0; i < keys.Length; i++)
            {
                Equal((double)(i < 5 ? 11 + i : i == 9 ? 0 : i - 4), (double)direct["data"][keys[i]]["value"]);
                Equal("workplaceSlots", (string)direct["data"][keys[i]]["unit"]);
                Equal(JTokenType.Null, direct["data"][keys[i]]["period"].Type);
            }
            var fixture = new JObject
            {
                ["request"] = request, ["direct"] = direct, ["observation"] = observation, ["group"] = group,
                ["catalog"] = JArray.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(DiagnosticMetricCatalog.Read())))
            };
            var samples = new JArray();
            for (int sample = 0; sample < 4; sample++)
            {
                // Controlled two-window data, emitted by the exact producer.
                // Native time interpretation and gameplay effects are not claimed.
                handlers.SetMetricClock(100 + (sample / 2) * 100 + (sample % 2) * 10, .2f + (sample % 2) * .1f);
                for (int i = 0; i < tiers.Length; i++)
                {
                    typeof(EducationCounters).GetProperty(tiers[i]).SetValue(handlers.TestLaborData.jobs.totalByEducation,
                        100 + i + (sample / 2) * 10 + (sample % 2) * 2);
                    typeof(EducationCounters).GetProperty(tiers[i]).SetValue(handlers.TestLaborData.jobs.freeByEducation,
                        i + (sample / 2) * 20 + (sample % 2) * 3);
                }
                samples.Add(JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(((IObservationReaders)handlers).Metrics(normalized)))));
            }
            fixture["samples"] = samples;
            handlers.TestLaborData.jobs.totalByEducation = null;
            handlers.TestLaborData.jobs.freeByEducation.uneducated = -1;
            var unavailable = ((IObservationReaders)handlers).Metrics(normalized);
            fixture["unavailable"] = JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(unavailable)));
            Equal(JTokenType.Null, fixture["unavailable"]["data"]["values"][keys[0]]["value"].Type);
            Equal("invalidNativeCount", (string)fixture["unavailable"]["data"]["values"][keys[5]]["reason"]);
            string directory = Environment.GetEnvironmentVariable("WORKFORCE_WIRE_DIR");
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "workforce.json"), fixture.ToString());
            }
        });
    }
}
