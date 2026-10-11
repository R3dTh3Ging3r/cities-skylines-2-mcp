using System;
using System.Threading;

namespace CS2MCP.Core
{
    /// <summary>Additive error wire data. Unknown requires reconciliation, not automatic retry.</summary>
    public sealed class BridgeErrorData
    {
        public string code { get; set; }
        public string message { get; set; }
        public bool retryable { get; set; }
        private string m_Outcome;
        public string outcome
        {
            get => m_Outcome;
            set
            {
                if (value != "notApplied" && value != "unknown" && value != "partial")
                    throw new ArgumentException("outcome must be notApplied, unknown or partial", nameof(value));
                m_Outcome = value;
            }
        }
        public int? httpStatus { get; set; }
        public string[] nativeErrors { get; set; }
        public string operationId { get; set; }

        public BridgeErrorData(string code, string message, bool retryable = false,
            string outcome = "notApplied", int? httpStatus = null,
            string operationId = null, string[] nativeErrors = null)
        {
            this.code = code;
            this.message = message;
            this.retryable = retryable;
            this.outcome = outcome;
            this.httpStatus = httpStatus;
            this.operationId = operationId;
            this.nativeErrors = nativeErrors;
        }
    }

    public sealed class Capability
    {
        public string name { get; set; }
        public string support { get; set; }
        public string reason { get; set; }
    }

    /// <summary>Independent identities; unavailable evidence stays null with a reason.</summary>
    public sealed class BuildIdentity
    {
        public string assemblyVersion { get; }
        public string modVersion { get; }
        public string buildIdentity { get; }
        public string buildIdentityReason => buildIdentity == null ? "buildIdentityUnavailable" : null;
        public string sourceRevision { get; }
        public string sourceRevisionReason => sourceRevision == null ? "sourceRevisionUnavailable" : null;
        public string gameVersion => null;
        public string gameVersionReason => "gameVersionNotObserved";

        public BuildIdentity(string assemblyVersion, string modVersion, string buildIdentity, string sourceRevision)
        {
            this.assemblyVersion = assemblyVersion;
            this.modVersion = modVersion;
            this.buildIdentity = buildIdentity;
            this.sourceRevision = sourceRevision ?? EmbeddedRevision(buildIdentity);
        }

        private static string EmbeddedRevision(string information)
        {
            if (information == null) return null;
            int separator = information.LastIndexOf('+');
            if (separator < 0) return null;
            string candidate = information.Substring(separator + 1);
            if (candidate.Length != 40 && candidate.Length != 64) return null;
            foreach (char character in candidate)
                if (!Uri.IsHexDigit(character)) return null;
            return candidate;
        }
    }

    /// <summary>Immutable managed evidence, safe to serialize on listener threads.</summary>
    public sealed class RuntimeEvidence
    {
        public DateTime? observedAtUtc { get; }
        public double? ageMilliseconds { get; }
        public string gameMode { get; }
        public bool? isLoading { get; }
        public bool? cityLoaded { get; }
        public string reason { get; }

        internal RuntimeEvidence(DateTime? observedAtUtc, double? ageMilliseconds,
            string gameMode, bool? isLoading, bool? cityLoaded, string reason)
        {
            this.observedAtUtc = observedAtUtc;
            this.ageMilliseconds = ageMilliseconds;
            this.gameMode = gameMode;
            this.isLoading = isLoading;
            this.cityLoaded = cityLoaded;
            this.reason = reason;
        }
    }

    /// <summary>Publish only on the main thread. Read never touches native game objects.</summary>
    public sealed class RuntimeSnapshotStore
    {
        public const int FreshnessMilliseconds = 5000;
        private RuntimeEvidence m_Snapshot;

        public void Publish(DateTime observedAtUtc, string gameMode, bool? isLoading, bool? cityLoaded, string reason)
        {
            Volatile.Write(ref m_Snapshot, new RuntimeEvidence(observedAtUtc, 0, gameMode, isLoading, cityLoaded, reason));
        }

        public RuntimeEvidence Read(DateTime nowUtc)
        {
            RuntimeEvidence snapshot = Volatile.Read(ref m_Snapshot);
            if (snapshot == null) return new RuntimeEvidence(null, null, null, null, null, "noMainThreadObservation");
            double age = (nowUtc - snapshot.observedAtUtc.Value).TotalMilliseconds;
            // A backwards clock change also makes age evidence unreliable.
            if (age < 0 || age > FreshnessMilliseconds)
                return new RuntimeEvidence(snapshot.observedAtUtc, age, null, null, null, "mainThreadObservationStale");
            return new RuntimeEvidence(snapshot.observedAtUtc, age, snapshot.gameMode,
                snapshot.isLoading, snapshot.cityLoaded, snapshot.reason);
        }
    }
}
