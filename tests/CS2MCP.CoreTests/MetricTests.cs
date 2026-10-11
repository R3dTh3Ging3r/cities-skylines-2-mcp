using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class MetricTests
    {
        private static object Describe(string method, params object[] args)
        {
            var type = typeof(Capability).Assembly.GetType("CS2MCP.Core.MetricContracts");
            if (type == null) throw new Exception("metric contracts are missing");
            return type.GetMethod(method).Invoke(null, args);
        }
        private static object Field(object value, string name) => value.GetType().GetProperty(name).GetValue(value);

        private static void EqualWire(object expected, object actual)
        {
            if (expected is Dictionary<string, object> map)
            {
                var result = (Dictionary<string, object>)actual;
                Program.Equal(map.Count, result.Count);
                foreach (var pair in map) EqualWire(pair.Value, result[pair.Key]);
            }
            else if (expected is object[] array)
            {
                var result = (object[])actual;
                Program.Equal(array.Length, result.Length);
                for (int i = 0; i < array.Length; i++) EqualWire(array[i], result[i]);
            }
            else if (expected is int || expected is decimal || expected is double)
            {
                if (!(actual is int || actual is decimal || actual is double)) throw new Exception("expected numeric wire value");
                Program.Equal(Convert.ToDouble(expected), Convert.ToDouble(actual));
            }
            else Program.Equal(expected, actual);
        }

        public static void Run()
        {
            Program.Check("production demand and budget metadata agree with hand-derived wire fixtures", () =>
            {
                var serializer = new JavaScriptSerializer();
                var fixtures = (Dictionary<string, object>)serializer.DeserializeObject(File.ReadAllText("tests/contracts/metrics.json"));
                foreach (var entry in new[] { ("demand", "DescribeDemand"), ("budget", "DescribeBudget") })
                    EqualWire(((Dictionary<string, object>)fixtures[entry.Item1])["metricMetadata"],
                        serializer.DeserializeObject(serializer.Serialize(Describe(entry.Item2))));
            });
            Program.Check("production route metadata qualifies both raw cargo and passenger fixtures", () =>
            {
                var serializer = new JavaScriptSerializer();
                var fixtures = (Dictionary<string, object>)serializer.DeserializeObject(File.ReadAllText("tests/contracts/metrics.json"));
                var transit = (Dictionary<string, object>)fixtures["transit"];
                foreach (Dictionary<string, object> line in (object[])transit["lines"])
                    EqualWire(line["metricMetadata"], serializer.DeserializeObject(serializer.Serialize(
                        Describe("DescribeRoute", Convert.ToSingle(line["lengthMeters"]), (bool)line["isCargo"], (int)line["passengersOnBoard"]))));
            });
            Program.Check("building demand preserves the native 0 through 255 scale", () =>
            {
                var value = Describe("DescribeDemand");
                Program.Equal(1, Field(value, "schemaVersion"));
                Program.Equal(0, Field(value, "buildingDemandMin"));
                Program.Equal(255, Field(value, "buildingDemandMax"));
                Program.Equal("nativeCounter", Field(value, "companyDemandUnit"));
                Program.Equal("simulationUpdates", Field(value, "refreshPeriod"));
            });
            Program.Check("monthly budget distinguishes signed aggregate from positive cost components", () =>
            {
                var value = Describe("DescribeBudget");
                Program.Equal("month", Field(value, "ratePeriod"));
                Program.Equal("gameHour", Field(value, "refreshPeriod"));
                Program.Equal("signedNegativeCosts", Field(value, "totalExpensesConvention"));
                Program.Equal("positiveCosts", Field(value, "expenseComponentsConvention"));
                Program.Equal("totalIncome + totalExpenses", Field(value, "balanceConvention"));
                Program.Equal("unverified", Field(value, "nativeParityStatus"));
            });
            foreach (float length in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Program.Check("invalid route length is qualified: " + length, () =>
                {
                    var value = Describe("DescribeRoute", length, false, 12);
                    Program.Equal(length, Field(value, "rawLength"));
                    Program.Equal(null, Field(value, "validatedLengthMeters"));
                    Program.Equal("invalid", Field(value, "lengthStatus"));
                    if (string.IsNullOrEmpty((string)Field(value, "lengthWarning"))) throw new Exception("missing warning");
                });
            foreach (float length in new[] { 0f, 123.5f })
                Program.Check("finite nonnegative route length retains metres: " + length, () =>
                {
                    var value = Describe("DescribeRoute", length, false, 12);
                    Program.Equal(length, Field(value, "validatedLengthMeters"));
                    Program.Equal("valid", Field(value, "lengthStatus"));
                    Program.Equal(null, Field(value, "lengthWarning"));
                    Program.Equal("passengers", Field(value, "loadKind"));
                    Program.Equal("passengers", Field(value, "loadUnit"));
                });
            Program.Check("cargo load 12 retains its value without claiming passengers or tonnes", () =>
            {
                var value = Describe("DescribeRoute", -5f, true, 12);
                Program.Equal(12, Field(value, "rawLoad"));
                Program.Equal("cargo", Field(value, "loadKind"));
                Program.Equal("unknown", Field(value, "loadUnit"));
                Program.Equal("unverified", Field(value, "loadUnitStatus"));
                if (string.IsNullOrEmpty((string)Field(value, "loadUnitReason"))) throw new Exception("missing cargo evidence reason");
            });
            Program.Check("calendar conversion preserves raw native clock and remains unverified", () =>
            {
                var value = Describe("DescribeCalendar", 0.25f, 0.5f, 2026, 12);
                Program.Equal(0.25f, Field(value, "rawNormalizedDate"));
                Program.Equal(0.5f, Field(value, "rawNormalizedTime"));
                Program.Equal(2026, Field(value, "rawYear"));
                Program.Equal(12, Field(value, "rawDaysPerYear"));
                Program.Equal("unverified", Field(value, "conversionStatus"));
                Program.Equal("nativeCalendarParityNotRun", Field(value, "reason"));
            });
            Program.Check("service count remains raw with unresolved semantics", () =>
            {
                var value = Describe("DescribeServiceBuildingCount", 7);
                Program.Equal(7, Field(value, "rawCount"));
                Program.Equal("unverified", Field(value, "status"));
                Program.Equal("nativeServiceBuildingCountParityNotRun", Field(value, "reason"));
            });
        }
    }
}
