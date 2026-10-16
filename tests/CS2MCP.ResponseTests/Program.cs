using System;
using System.Text;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int passed, failed;
    private static void Check(string name, Action assertion)
    {
        try { assertion(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }
    private static JObject Wire(BridgeResponse response) => JObject.Parse(Encoding.UTF8.GetString(response.Body));
    private static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Exception($"expected {expected}, got {actual}");
    }
    private static int Main()
    {
        // These messages can be emitted after Apply; generic status cannot attest no effects.
        foreach (string message in new[] {
            "road replacement interrupted because another tool became active; check cs2_list_roads before retrying",
            "transit operation interrupted because another tool became active; check cs2_list_transit_lines / cs2_list_transit_stops before retrying",
            "the game did not commit the stop (it may have been rejected while applying)",
            "the game did not commit the line (it may have been rejected while applying)" })
            Check("post-apply 409 serialization: " + message, () => {
                var response = BridgeResponse.Error(409, message);
                var wire = Wire(response);
                Equal(409, response.Status);
                Equal(message, (string)wire["error"]);
                Equal(message, (string)wire["errorData"]["message"]);
                Equal("BRIDGE_BUSY", (string)wire["errorData"]["code"]);
                Equal("unknown", (string)wire["errorData"]["outcome"]);
                Equal(false, (bool)wire["errorData"]["retryable"]);
                Equal(409, (int)wire["errorData"]["httpStatus"]);
            });
        Check("generic 400 does not infer effects from status", () =>
            Equal("unknown", (string)Wire(BridgeResponse.Error(400, "legacy rejection"))["errorData"]["outcome"]));
        Check("explicit pre-apply validation rejection retains no-effect evidence and diagnostics", () => {
            var wire = Wire(BridgeResponse.Error(409, "road blocked before Apply; nothing was changed",
                ErrorCodes.NativeRejected, outcome: "notApplied", nativeErrors: new[] { "overlap" }));
            Equal("NATIVE_REJECTED", (string)wire["errorData"]["code"]);
            Equal("notApplied", (string)wire["errorData"]["outcome"]);
            Equal(false, (bool)wire["errorData"]["retryable"]);
            Equal("overlap", (string)wire["errorData"]["nativeErrors"][0]);
            Equal(null, wire["errorData"]["operationId"]);
        });
        Check("queued timeout remains unknown", () =>
            Equal("unknown", (string)Wire(BridgeResponse.Error(503, "simulation wait expired",
                ErrorCodes.OutcomeUnknown, outcome: "unknown"))["errorData"]["outcome"]));
        Check("success serialization retains null capability reason", () =>
            Equal(JTokenType.Null, Wire(BridgeResponse.Json(new { reason = (string)null }))["reason"].Type));
        Check("detached receipt numeric values serialize as JSON scalars", () => {
            var result = new System.Collections.Generic.Dictionary<string,object> {
                ["cost"] = 12.5m, ["counts"] = new object[] { 123L, -4.5d }
            };
            var record = new OperationRecord { operationId = "b6f2a37a-5676-40ec-8b56-3553b32c28e4",
                world = new WorldRef("boot",1), receipt = new MutationReceipt { result = CommandJson.Snapshot(result) } };
            var wire = Wire(BridgeResponse.Json(record));
            Equal(JTokenType.Float,wire["receipt"]["result"]["cost"].Type);
            Equal(12.5m,(decimal)wire["receipt"]["result"]["cost"]);
            Equal(JTokenType.Integer,wire["receipt"]["result"]["counts"][0].Type);
            Equal(123L,(long)wire["receipt"]["result"]["counts"][0]);
            Equal(-4.5d,(double)wire["receipt"]["result"]["counts"][1]);
            Equal(1L,(long)wire["world"]["epoch"]);
        });
        ConstructionWireTests.Run(Check);
        ObservationHttpTests.Run(Check);
        TransitWireTests.Run(Check);
        DiagnosticMetricWireTests.Run(Check);
        WorkforceWireTests.Run(Check);
        Console.WriteLine($"Response serialization: {passed + failed} assertions, {passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
