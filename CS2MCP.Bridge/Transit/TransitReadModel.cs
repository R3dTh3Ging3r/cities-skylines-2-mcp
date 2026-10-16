using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2MCP.Core
{
    public static class TransitReader
    {
        public const int MaxRouteEntries = 128;
        public static Observation<TransitPage> Read(TransitQuery query, WorldIdentity worlds, Func<long> frame, ITransitSource source)
        {
            worlds.Validate(query.World);
            long start = frame();
            var root = source.ReadRoot(query);
            worlds.Validate(query.World);
            if (root.state != "available")
                throw new OperationException(new BridgeErrorData(ErrorCodes.InvalidArgument, "selected line " + root.state, httpStatus: root.state == "wrongKind" ? 400 : 404));
            var waypoints = Array.Empty<TransitWaypoint>();
            var segments = Array.Empty<TransitSegment>();
            bool withinBudget = root.waypointCount.HasValue && root.segmentCount.HasValue && root.waypointCount >= 0 && root.segmentCount >= 0 && (long)root.waypointCount.Value + root.segmentCount.Value <= MaxRouteEntries;
            if (withinBudget)
            {
                waypoints = new TransitWaypoint[root.waypointCount.Value];
                segments = new TransitSegment[root.segmentCount.Value];
                for (int i = 0; i < waypoints.Length; i++)
                {
                    worlds.Validate(query.World);
                    waypoints[i] = source.ReadWaypoint(i);
                }

                for (int i = 0; i < segments.Length; i++)
                {
                    worlds.Validate(query.World);
                    segments[i] = source.ReadSegment(i);
                }
            }

            worlds.Validate(query.World);
            var data = TransitReadModel.Project(query, root, waypoints, segments);
            long end = frame();
            worlds.Validate(query.World);
            var warnings = new List<string>
            {
                "partialSelectedTransit",
                "nativeTransitFixtureNotRun",
                "structuralFactsNotNativeValidity"
            };
            if (start != end)
                warnings.Add("multiFrameReadNotAtomic");
            if (data.filter.state == "unverifiable")
                warnings.Add("filterUnverifiable");
            return new Observation<TransitPage>
            {
                data = data,
                world = query.World,
                startFrame = start,
                endFrame = end,
                sampledAtUtc = DateTime.UtcNow,
                freshness = "current",
                warnings = warnings.ToArray()
            };
        }
    }

    public static class TransitReadModel
    {
        static bool MissingReference(string state) => state == "missing" || state == "deleted" || state == "temporary" || state == "nullRef";
        static Point3 Point(Point3 point) => point != null && ConstructionGeometry.Finite(point.x) && ConstructionGeometry.Finite(point.y) && ConstructionGeometry.Finite(point.z) ? point : null;
        static string Purpose(bool? passenger, bool? cargo) => !passenger.HasValue || !cargo.HasValue ? "unknown" : passenger.Value && cargo.Value ? "mixed" : passenger.Value ? "passenger" : cargo.Value ? "cargo" : "unknown";
        static TransitFilter Filter(TransitQuery query, TransitRootFacts root)
        {
            var missing = new List<string>();
            var mismatches = new List<string>();
            if (query.Mode != null)
            {
                if (root.mode == null)
                    missing.Add("modeUnavailable");
                else if (root.mode != query.Mode)
                    mismatches.Add("modeMismatch");
            }

            if (query.Purpose != null)
            {
                bool? flag = query.Purpose == "passenger" ? root.passenger : root.cargo;
                if (!flag.HasValue)
                    missing.Add("purposeUnavailable");
                else if (!flag.Value)
                    mismatches.Add("purposeMismatch");
            }

            if (missing.Count > 0)
                return new TransitFilter
                {
                    state = "unverifiable",
                    reasons = missing.ToArray()
                };
            if (mismatches.Count > 0)
                return new TransitFilter
                {
                    state = "excluded",
                    reasons = mismatches.ToArray()
                };
            return new TransitFilter
            {
                state = query.Mode == null && query.Purpose == null ? "notRequested" : "matched"
            };
        }

        public static TransitPage Project(TransitQuery query, TransitRootFacts root, TransitWaypoint[] waypoints, TransitSegment[] segments)
        {
            var filter = Filter(query, root);
            var page = new TransitPage
            {
                requestedLine = query.Line,
                filter = filter,
                matched = filter.state == "unverifiable" ? (int? )null : filter.state == "excluded" ? 0 : 1
            };
            if (filter.state == "excluded")
                return page;
            var incomplete = new List<string>();
            if (!root.waypointCount.HasValue || !root.segmentCount.HasValue)
                incomplete.Add("routeBufferUnavailable");
            else if ((long)root.waypointCount.Value + root.segmentCount.Value > TransitReader.MaxRouteEntries)
                incomplete.Add("routeBuffersExceedDiagnosticBudget");
            else if (root.waypointCount != waypoints.Length || root.segmentCount != segments.Length)
                incomplete.Add("routeBufferReadIncomplete");
            var reasons = new List<string>(incomplete);
            if (root.waypointCount == 0 || root.segmentCount == 0)
                reasons.Add("emptyRouteBuffer");
            if (root.waypointCount != root.segmentCount)
                reasons.Add("routeCardinalityUnverified");
            var broken = new List<EntityRef>();
            var invalid = new List<EntityRef>();
            bool usable = reasons.Count == 0;
            double length = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var waypoint = waypoints[i];
                waypoint.order = i + 1;
                var issues = new List<string>();
                if (waypoint.state != "available")
                    issues.Add("waypointUnavailable");
                if (waypoint.connectionState != "available" || waypoint.connection == null)
                    issues.Add("stopConnectionUnavailable");
                waypoint.position = Point(waypoint.position);
                waypoint.positionReason = waypoint.position == null ? "waypointPositionUnavailable" : null;
                if (waypoint.position == null)
                    issues.Add("waypointPositionUnavailable");
                bool invalidWaiting = waypoint.waitingPassengers < 0;
                if (invalidWaiting)
                    waypoint.waitingPassengers = null;
                waypoint.waitingReason = invalidWaiting ? "invalidNativeWaitingCount" : waypoint.waitingPassengers.HasValue ? null : "waitingPassengersUnavailable";
                if (waypoint.stop != null)
                {
                    waypoint.stop.position = Point(waypoint.stop.position);
                    waypoint.stop.positionReason = waypoint.stop.position == null ? "stopPositionUnavailable" : null;
                }

                waypoint.issues = issues.ToArray();
                if (issues.Count > 0)
                    usable = false;
                bool knownFault = MissingReference(waypoint.state) || MissingReference(waypoint.connectionState);
                waypoint.structuralAssessment = knownFault ? "knownFault" : waypoint.state == "available" && waypoint.connectionState == "available" ? "noKnownFault" : "unassessed";
                if (knownFault && waypoint.entity != null)
                    broken.Add(waypoint.entity);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                segment.order = i + 1;
                var issues = new List<string>();
                if (segment.state != "available")
                    issues.Add("segmentUnavailable");
                bool invalidDistance = segment.rawDistance.HasValue && (!ConstructionGeometry.Finite(segment.rawDistance.Value) || segment.rawDistance.Value < 0);
                if (segment.rawDistance.HasValue && !ConstructionGeometry.Finite(segment.rawDistance.Value))
                {
                    segment.rawDistance = null;
                    segment.distanceReason = "nonfiniteNativeDistance";
                }

                if (!segment.rawDistance.HasValue)
                {
                    segment.distanceReason = segment.distanceReason ?? "pathInformationUnavailable";
                    issues.Add(segment.distanceReason);
                }
                else if (segment.rawDistance < 0)
                    issues.Add("negativeNativeDistance");
                else
                    length += segment.rawDistance.Value;
                segment.issues = issues.ToArray();
                if (issues.Count > 0)
                    usable = false;
                bool knownFault = MissingReference(segment.state) || invalidDistance;
                segment.structuralAssessment = knownFault ? "knownFault" : segment.state == "available" && segment.rawDistance.HasValue ? "noKnownFault" : "unassessed";
                if (knownFault && segment.entity != null)
                    invalid.Add(segment.entity);
            }

            if (waypoints.Any(w => w.issues.Length > 0))
                reasons.Add("waypointEvidenceIncomplete");
            if (segments.Any(s => s.issues.Length > 0))
                reasons.Add("segmentEvidenceIncomplete");
            bool waypointFault = waypoints.Any(w => w.structuralAssessment == "knownFault");
            bool segmentFault = segments.Any(s => s.structuralAssessment == "knownFault");
            if (waypointFault)
                reasons.Add("brokenWaypointFacts");
            if (segmentFault)
                reasons.Add("invalidSegmentFacts");
            string purpose = Purpose(root.passenger, root.cargo);
            page.items = new[]
            {
                new TransitLineView
                {
                    entity = query.Line,
                    assetKey = root.assetKey != null && root.assetKey.Length <= 256 ? root.assetKey : null,
                    assetKeyReason = root.assetKey == null ? "prefabNameUnavailable" : root.assetKey.Length > 256 ? "prefabNameExceedsBound" : null,
                    owner = root.owner,
                    ownerReason = root.owner == null ? "ownerUnavailable" : null,
                    routeNumber = root.routeNumber,
                    mode = root.mode,
                    modeReason = root.mode == null ? "transportModeUnavailable" : null,
                    purpose = purpose,
                    purposeFlags = new TransitPurposeFlags
                    {
                        passenger = root.passenger,
                        cargo = root.cargo
                    },
                    purposeReason = purpose == "unknown" ? (root.passenger == false && root.cargo == false ? "noTransportPurposeFlag" : "transportPurposeUnavailable") : null,
                    load = new TransitLoad
                    {
                        kind = purpose == "passenger" ? "passengers" : purpose == "cargo" ? "cargo" : null
                    },
                    completeness = new TransitCompleteness
                    {
                        state = incomplete.Count == 0 ? "complete" : "incomplete",
                        waypointCount = root.waypointCount,
                        segmentCount = root.segmentCount,
                        readWaypoints = waypoints.Length,
                        readSegments = segments.Length,
                        reasons = incomplete.ToArray()
                    },
                    waypoints = waypoints,
                    segments = segments,
                    health = new TransitHealth
                    {
                        state = waypointFault || segmentFault ? "invalid" : "unknown",
                        reasons = reasons.ToArray(),
                        brokenWaypoints = broken.ToArray(),
                        invalidSegments = invalid.ToArray(),
                        rawLength = usable && ConstructionGeometry.Finite(length) ? length : (double? )null
                    }
                }
            };
            return page;
        }
    }
}
