using System;

namespace CS2MCP.CoreTests
{
    internal static class Program
    {
        private static int passed;
        private static int failed;

        public static void Check(string name, Action assertion)
        {
            try { assertion(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
        }

        public static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception($"expected {expected}, got {actual}");
        }

        public static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("expected " + typeof(T).Name);
        }

        private static int Main(string[] args)
        {
            string suite = args.Length == 2 && args[0] == "--suite" ? args[1] : null;
            if (suite == "deferred") DeferredWorkTests.Run();
            else if (suite == "construction-inventory") ConstructionInventoryTests.Run();
            else if (suite == "operations") OperationTests.Run();
            else if (suite == "contracts") ContractTests.Run();
            else if (suite == "endpoints") EndpointTests.Run();
            else if (suite == "metrics") MetricTests.Run();
            else if (suite == "values") ValueTests.Run();
            else { failed++; Console.WriteLine("FAIL unknown or missing suite: " + (suite ?? "<missing>")); }
            if (passed + failed == 0) { failed++; Console.WriteLine("FAIL zero assertions"); }
            Console.WriteLine($"Suite {suite ?? "<missing>"}: {passed + failed} assertions, {passed} passed, {failed} failed");
            return failed == 0 ? 0 : 1;
        }
    }
}
