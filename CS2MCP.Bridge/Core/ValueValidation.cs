using System;

namespace CS2MCP.Core
{
    /// <summary>Strict numeric validation; step zero means unknown/no grid, not a native slider step.</summary>
    public static class ValueValidation
    {
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public static ValueCheck Check(double requested, double min, double max, double step)
        {
            if (!IsFinite(requested)) return new ValueCheck(null, null, "nonFinite");
            if (!IsFinite(min) || !IsFinite(max) || min > max || !IsFinite(step) || step < 0)
                return new ValueCheck(requested, null, "invalidConstraints");
            if (requested < min || requested > max) return new ValueCheck(requested, null, "outOfRange");
            if (step > 0)
            {
                double units = (requested - min) / step;
                if (!IsFinite(units)) return new ValueCheck(requested, null, "invalidConstraints");
                // Native policy inputs are floats. Allow representation noise, never snap the input.
                if (Math.Abs(units - Math.Round(units)) > 0.00001)
                    return new ValueCheck(requested, null, "offStep");
            }
            return new ValueCheck(requested, requested, null);
        }

        public static ValueCheck LegacyClamp(double requested, double min, double max)
        {
            if (!IsFinite(requested) || !IsFinite(min) || !IsFinite(max) || min > max)
                return Check(requested, min, max, 0);
            double candidate = Math.Max(min, Math.Min(max, requested));
            return new ValueCheck(requested, candidate, requested == candidate ? null : "legacyClamped");
        }
    }

    public sealed class ValueCheck
    {
        public bool accepted => effectiveCandidate.HasValue;
        public double? requested { get; }
        public double? effectiveCandidate { get; }
        public string reason { get; }
        internal ValueCheck(double? requested, double? candidate, string reason)
        { this.requested = requested; effectiveCandidate = candidate; this.reason = reason; }
    }

    public sealed class ValueRange
    {
        public double min { get; }
        public double max { get; }
        public string source { get; }
        public ValueRange(double min, double max, string source)
        { this.min = min; this.max = max; this.source = source; }
    }

    /// <summary>Verified means the named getter/buffer was read, not that deferred gameplay effects completed.</summary>
    public sealed class SetterReadback
    {
        public int schemaVersion => 1;
        public object requested { get; }
        public object effective { get; }
        public ValueRange range { get; }
        public double? step { get; }
        public string verification { get; }
        public string mechanism { get; }
        public string warning { get; }
        public string nativeParityStatus => "notRun";

        private SetterReadback(object requested, object effective, ValueRange range, double? step,
            string verification, string mechanism, string warning)
        {
            this.requested = requested; this.effective = effective; this.range = range; this.step = step;
            this.verification = verification; this.mechanism = mechanism; this.warning = warning;
        }

        public static SetterReadback Pending(object requested, ValueRange range, double? step, string mechanism, string warning = null)
            => new SetterReadback(requested, null, range, step, "pending", mechanism, warning);

        public static SetterReadback Observed(object requested, object observed, ValueRange range, double? step, string mechanism, string warning = null)
        {
            bool valid = observed != null && (!(observed is double d) || ValueValidation.IsFinite(d))
                && (!(observed is float f) || ValueValidation.IsFinite(f));
            return new SetterReadback(requested, valid ? observed : null, range, step,
                valid ? "verified" : "pending", mechanism, valid ? warning : "getter did not provide a finite value; " + warning);
        }

        public static SetterReadback Unsupported(object requested, string mechanism, string warning)
            => new SetterReadback(requested, null, null, null, "unsupported", mechanism, warning);
    }

    /// <summary>Role-based normal prefab discovery; existing station platforms use a separate route-stop path.</summary>
    public static class StopDiscovery
    {
        public static bool IsNormalPlaceable(bool roadsideType, bool passenger, bool placeableRole,
            bool placeholderData, bool placeholderElements)
            => roadsideType && passenger && placeableRole && !placeholderData && !placeholderElements;

        public static TransitPrerequisites DescribePrerequisites(bool tickedSinceLoad, bool? locked, int? depotCount, int? stopCount)
            => new TransitPrerequisites(tickedSinceLoad, locked, depotCount, stopCount);
    }

    public sealed class TransitPrerequisites
    {
        public int schemaVersion => 1;
        public string freshness { get; }
        public string unlockState { get; }
        public string unlockNodes => "unknown";
        public int? depotCount { get; }
        public int? stopCount { get; }
        public string dispatchReadiness => "unknown";
        public string nativeParityStatus => "notRun";
        public string warning => "unlock-node dependencies are not resolved; depot count does not prove activation, connectivity or available vehicles";
        internal TransitPrerequisites(bool tickedSinceLoad, bool? locked, int? depots, int? stops)
        {
            freshness = tickedSinceLoad ? "current" : "warming";
            unlockState = !tickedSinceLoad || !locked.HasValue ? "unknown" : locked.Value ? "locked" : "unlocked";
            depotCount = depots; stopCount = stops;
        }
    }
}
