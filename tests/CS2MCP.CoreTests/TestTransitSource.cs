// Native-data boundary double only; extraction budget and projection are production code.
using CS2MCP.Core;

namespace CS2MCP
{
    internal sealed class TestTransitSource : ITransitSource
    {
        public TransitRootFacts ReadRoot(TransitQuery query) => new TransitRootFacts
        {
            state = "available",
            mode = "train",
            passenger = true,
            cargo = false,
            waypointCount = 0,
            segmentCount = 0
        };
        public TransitWaypoint ReadWaypoint(int index) => throw new System.Exception("unexpected native element read");
        public TransitSegment ReadSegment(int index) => throw new System.Exception("unexpected native element read");
    }
}
