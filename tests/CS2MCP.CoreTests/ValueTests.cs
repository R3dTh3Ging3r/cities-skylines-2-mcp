using System;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class ValueTests
    {
        public static void Run()
        {
            Program.Check("strict grid accepts 25", () => {
                var check = ValueValidation.Check(25, 0, 100, 5);
                Program.Equal(true, check.accepted);
                Program.Equal((double?)25, check.effectiveCandidate);
            });
            Program.Check("strict grid rejects 26 without snapping", () => {
                var check = ValueValidation.Check(26, 0, 100, 5);
                Program.Equal(false, check.accepted);
                Program.Equal("offStep", check.reason);
                Program.Equal((double?)null, check.effectiveCandidate);
            });
            Program.Check("strict bounds include endpoints", () => {
                Program.Equal(true, ValueValidation.Check(0, 0, 100, 5).accepted);
                Program.Equal(true, ValueValidation.Check(100, 0, 100, 5).accepted);
                Program.Equal(false, ValueValidation.Check(-1, 0, 100, 5).accepted);
                Program.Equal(false, ValueValidation.Check(101, 0, 100, 5).accepted);
            });
            Program.Check("reject nonfinite values and invalid constraints", () => {
                foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                    Program.Equal(false, ValueValidation.Check(value, 0, 100, 5).accepted);
                Program.Equal(false, ValueValidation.Check(25, 100, 0, 5).accepted);
                Program.Equal(false, ValueValidation.Check(25, 0, 100, -1).accepted);
            });
            Program.Check("fractional steps tolerate floating representation", () => {
                Program.Equal(true, ValueValidation.Check(0.3, 0, 1, 0.1).accepted);
                Program.Equal(false, ValueValidation.Check(0.31, 0, 1, 0.1).accepted);
                Program.Equal(true, ValueValidation.Check(15, 10, 100, 5).accepted);
            });
            Program.Check("native float grid accepts large step counts without snapping", () => {
                foreach (float requested in new[] { 0.9f, 50.1f, 99.9f, 5000.1f, 9999.9f })
                {
                    var check = ValueValidation.Check((double)requested, 0, 10000, (double)0.1f);
                    Program.Equal(true, check.accepted);
                    Program.Equal((double?)requested, check.effectiveCandidate);
                }
                Program.Equal(true, ValueValidation.Check((double)60.2f, (double)10.1f, 100, (double)0.1f).accepted);
                Program.Equal(true, ValueValidation.Check((double)49.9f, (double)(-50.1f), 100, (double)0.1f).accepted);
            });
            Program.Check("native float precision allowance rejects genuinely off-grid values", () => {
                foreach (float requested in new[] { 0.31f, 50.11f, 99.91f, 5000.11f, 9999.91f })
                {
                    var check = ValueValidation.Check((double)requested, 0, 10000, (double)0.1f);
                    Program.Equal(false, check.accepted);
                    Program.Equal("offStep", check.reason);
                    Program.Equal((double?)null, check.effectiveCandidate);
                }
                Program.Equal(false, ValueValidation.Check((double)50.10001f, 0, 100, (double)0.1f).accepted);
                Program.Equal(false, ValueValidation.Check(50.10000001, 0, 100, 0.1).accepted);
            });
            Program.Check("unknown step checks bounds only", () => {
                Program.Equal(true, ValueValidation.Check(26, 0, 100, 0).accepted);
            });
            Program.Check("legacy clamp reports requested and candidate distinctly", () => {
                var check = ValueValidation.LegacyClamp(200, 50, 150);
                Program.Equal((double?)200, check.requested);
                Program.Equal((double?)150, check.effectiveCandidate);
                Program.Equal("legacyClamped", check.reason);
                Program.Equal(false, ValueValidation.LegacyClamp(double.PositiveInfinity, 50, 150).accepted);
            });
            Program.Check("deferred ack has no effective value", () => {
                var readback = SetterReadback.Pending(25, new ValueRange(0, 100, "synthetic"), 5, "queued");
                Program.Equal("pending", readback.verification);
                Program.Equal(null, readback.effective);
                Program.Equal(25, readback.requested);
            });
            Program.Check("later getter observation verifies actual value", () => {
                var readback = SetterReadback.Observed(25, 20, null, null, "getter");
                Program.Equal("verified", readback.verification);
                Program.Equal((object)20, readback.effective);
                Program.Equal((object)25, readback.requested);
                Program.Equal((object)25, SetterReadback.Observed(25, 25, null, null, "laterGetter").effective);
            });
            Program.Check("failed readback cannot verify candidate", () => {
                Program.Equal("pending", SetterReadback.Observed(25, null, null, null, "getter").verification);
                Program.Equal("pending", SetterReadback.Observed(25, double.NaN, null, null, "getter").verification);
            });
            Program.Check("unsupported has no inferred effective value", () => {
                var readback = SetterReadback.Unsupported(25, "unavailable", "getter unsupported");
                Program.Equal("unsupported", readback.verification);
                Program.Equal(null, readback.effective);
                Program.Equal("notRun", readback.nativeParityStatus);
            });
            Program.Check("normal stop excludes native placeholder and integrated roles", () => {
                Program.Equal(true, StopDiscovery.IsNormalPlaceable(true, true, true, false, false));
                Program.Equal(false, StopDiscovery.IsNormalPlaceable(true, true, true, true, false));
                Program.Equal(false, StopDiscovery.IsNormalPlaceable(true, true, true, false, true));
                Program.Equal(false, StopDiscovery.IsNormalPlaceable(true, true, false, false, false));
                Program.Equal(false, StopDiscovery.IsNormalPlaceable(false, true, true, false, false));
                Program.Equal(false, StopDiscovery.IsNormalPlaceable(true, false, true, false, false));
            });
            Program.Check("loaded not ticked prerequisites stay warming", () => {
                var prerequisites = StopDiscovery.DescribePrerequisites(false, true, 0, 2);
                Program.Equal("warming", prerequisites.freshness);
                Program.Equal("unknown", prerequisites.unlockState);
                Program.Equal("unknown", prerequisites.unlockNodes);
                Program.Equal((int?)0, prerequisites.depotCount);
                Program.Equal((int?)2, prerequisites.stopCount);
                Program.Equal("unknown", prerequisites.dispatchReadiness);
            });
            Program.Check("warmed lock is distinct from node and depot evidence", () => {
                var prerequisites = StopDiscovery.DescribePrerequisites(true, true, 0, null);
                Program.Equal("locked", prerequisites.unlockState);
                Program.Equal("unknown", prerequisites.unlockNodes);
                Program.Equal("unknown", prerequisites.dispatchReadiness);
            });
        }
    }
}
