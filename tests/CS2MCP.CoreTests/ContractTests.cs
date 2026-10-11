using System;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class ContractTests
    {
        public static void Run()
        {
            foreach (var entry in new[] {
                (400, "INVALID_ARGUMENT"), (403, "LOCKED"), (404, "UNSUPPORTED"),
                (405, "UNSUPPORTED"), (409, "BRIDGE_BUSY"), (422, "NATIVE_REJECTED"),
                (501, "UNSUPPORTED"), (503, "NOT_READY"), (500, "PROTOCOL_ERROR"), (418, "PROTOCOL_ERROR") })
            {
                Program.Check("status fallback " + entry.Item1, () => Program.Equal(entry.Item2, ErrorCodes.FromStatus(entry.Item1)));
            }
            foreach (string outcome in new[] { "notApplied", "unknown", "partial" })
                Program.Check("accepted outcome " + outcome, () => Program.Equal(outcome, new BridgeErrorData("NATIVE_REJECTED", "native reason", outcome: outcome).outcome));
            foreach (string outcome in new[] { "cancelled", "success", "", null })
                Program.Check("rejected outcome " + (outcome ?? "null"), () => Program.Throws<ArgumentException>(() => new BridgeErrorData("NATIVE_REJECTED", "native reason", outcome: outcome)));
            Program.Check("outcome setter cannot bypass validation", () => Program.Throws<ArgumentException>(() => new BridgeErrorData("LOCKED", "locked").outcome = "cancelled"));
            Program.Check("default rejection has no effects", () => Program.Equal("notApplied", new BridgeErrorData("LOCKED", "locked").outcome));
        }
    }
}
