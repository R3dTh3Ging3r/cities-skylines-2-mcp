using System;

namespace CS2MCP.Core
{
    public sealed class TransitCapabilities
    {
        public string scope => "explicitLine";
        public string support => "partial";
        public int maxRouteEntries => TransitReader.MaxRouteEntries;
        public string[] modes => new[]
        {
            "bus",
            "tram",
            "metro",
            "train",
            "airplane"
        };
        public string nativeVerification => "transit-readback-v1:notRun";
        public bool discovery => false;
        public bool load => false;
        public bool nativeValidity => false;
    }

    public sealed class TransitFilter
    {
        public string state { get; set; }
        public string[] reasons { get; set; } = Array.Empty<string>();
    }

    public sealed class TransitPage
    {
        public string scope => "explicitLine";
        public EntityRef requestedLine { get; set; }
        public TransitLineView[] items { get; set; } = Array.Empty<TransitLineView>();
        public int returned => items.Length;
        public int? matched { get; set; }
        public TransitFilter filter { get; set; }
        public string nextCursor => null;
        public bool truncated => false;
        public string nativeVerification => "transit-readback-v1:notRun";
        public TransitCapabilities capabilities => new TransitCapabilities();
    }

    public sealed class TransitPurposeFlags
    {
        public bool? passenger { get; set; }
        public bool? cargo { get; set; }
    }

    public sealed class TransitCompleteness
    {
        public string state { get; set; }
        public int? waypointCount { get; set; }
        public int? segmentCount { get; set; }
        public int readWaypoints { get; set; }
        public int readSegments { get; set; }
        public string[] reasons { get; set; } = Array.Empty<string>();
    }

    public sealed class TransitLoad
    {
        public string kind { get; set; }
        public double? value => null;
        public double? capacity => null;
        public string unit => null;
        public double? occupancyRatio => null;
        public string reason => "boundedLoadReaderUnavailable";
    }

    public sealed class TransitHealth
    {
        public string state { get; set; } = "unknown";
        public string[] reasons { get; set; } = Array.Empty<string>();
        public EntityRef[] brokenWaypoints { get; set; } = Array.Empty<EntityRef>();
        public EntityRef[] invalidSegments { get; set; } = Array.Empty<EntityRef>();
        public double? rawLength { get; set; }
        public string lengthUnit => null;
        public string lengthUnitReason => "nativeSegmentDistanceUnitNotVerified";
        public string lengthValidation => "finiteNonnegativeSegmentsOnly";
        public string nativeValidity => "unknown";
        public string nativeValidityReason => "committedRouteFixtureNotRun";
        public string assessment => "structuralFactsOnly";
    }

    public sealed class TransitLineView
    {
        public EntityRef entity { get; set; }
        public string assetKey { get; set; }
        public string assetKeyReason { get; set; }
        public EntityRef owner { get; set; }
        public string ownerReason { get; set; }
        public int? routeNumber { get; set; }
        public string mode { get; set; }
        public string modeReason { get; set; }
        public string purpose { get; set; }
        public TransitPurposeFlags purposeFlags { get; set; }
        public string purposeReason { get; set; }
        public string name => null;
        public string nameReason => "labelReaderNotBounded";
        public bool? active => null;
        public string activeReason => "lineActiveReaderNotVerified";
        public int? vehicles => null;
        public string vehiclesReason => "boundedFleetReaderUnavailable";
        public TransitLoad load { get; set; }
        public TransitCompleteness completeness { get; set; }
        public TransitWaypoint[] waypoints { get; set; }
        public TransitSegment[] segments { get; set; }
        public TransitHealth health { get; set; }
        public object candidateDuplicates => null;
        public string candidateDuplicatesReason => "discoveryUnsupported";
    }

    public sealed class TransitWaypoint
    {
        public int order { get; set; }
        public EntityRef entity { get; set; }
        public string state { get; set; }
        public EntityRef connection { get; set; }
        public string connectionState { get; set; }
        public Point3 position { get; set; }
        public string positionReason { get; set; }
        public int? waitingPassengers { get; set; }
        public string waitingReason { get; set; }
        public TransitStopFacts stop { get; set; }
        public string structuralAssessment { get; set; }
        public string[] issues { get; set; } = Array.Empty<string>();
    }

    public sealed class TransitStopFacts
    {
        public EntityRef entity { get; set; }
        public Point3 position { get; set; }
        public string positionReason { get; set; }
        public EntityRef owner { get; set; }
        public string ownerReason { get; set; }
        public EntityRef attachedParent { get; set; }
        public string attachedParentReason { get; set; }
        public bool? isAttachedToEdge { get; set; }
        public string mode { get; set; }
        public TransitPurposeFlags purposeFlags { get; set; }
        public bool? active { get; set; }
        public string activeReason { get; set; }
        public bool outsideConnection { get; set; }
    }

    public sealed class TransitSegment
    {
        public int order { get; set; }
        public EntityRef entity { get; set; }
        public string state { get; set; }
        public double? rawDistance { get; set; }
        public string distanceReason { get; set; }
        public string structuralAssessment { get; set; }
        public string[] issues { get; set; } = Array.Empty<string>();
    }

    /// <summary>Fixed root reads and buffer lengths only; no buffer elements or helper traversal.</summary>
    public sealed class TransitRootFacts
    {
        public string state { get; set; }
        public string assetKey { get; set; }
        public EntityRef owner { get; set; }
        public int? routeNumber { get; set; }
        public string mode { get; set; }
        public bool? passenger { get; set; }
        public bool? cargo { get; set; }
        public int? waypointCount { get; set; }
        public int? segmentCount { get; set; }
    }

    public interface ITransitSource
    {
        TransitRootFacts ReadRoot(TransitQuery query);
        TransitWaypoint ReadWaypoint(int index);
        TransitSegment ReadSegment(int index);
    }
}
