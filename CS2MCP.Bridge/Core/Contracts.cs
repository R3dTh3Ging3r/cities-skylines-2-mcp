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

namespace CS2MCP.Core
{
    public sealed class WorldRef
    {
        public string bootId { get; } public long epoch { get; }
        public WorldRef(string bootId, long epoch) { this.bootId = bootId; this.epoch = epoch; }
    }
    public sealed class EntityRef { public WorldRef world { get; set; } public int index { get; set; } public int version { get; set; } }
    public sealed class MutationReceipt
    {
        public EntityRef[] created { get; set; } = new EntityRef[0];
        public EntityRef[] changed { get; set; } = new EntityRef[0];
        public EntityRef[] reused { get; set; } = new EntityRef[0];
        public EntityRef[] removed { get; set; } = new EntityRef[0];
        public decimal? actualCost { get; set; } public string costStatus { get; set; } = "unknown";
        public object result { get; set; } public string[] warnings { get; set; } = new string[0];
        public string verification { get; set; } = "unverified";
    }
    public sealed class CommandRequest
    {
        public string operationId { get; } public WorldRef world { get; } public string leaseId { get; }
        public string name { get; } public string argsJson { get; } public string expectedFingerprint { get; }
        public string RequestHash
        {
            get
            {
                // Lease is authorization, separately bound/revalidated by the registry, not effect identity.
                string canonical = CommandJson.Stringify(new System.Collections.Generic.Dictionary<string, object> {
                    ["world"] = new System.Collections.Generic.Dictionary<string, object> { ["bootId"] = world.bootId, ["epoch"] = world.epoch },
                    ["name"] = name, ["args"] = CommandJson.Parse(argsJson), ["expectedFingerprint"] = expectedFingerprint
                });
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(canonical))).Replace("-", "").ToLowerInvariant();
            }
        }
        public CommandRequest(string operationId, WorldRef world, string leaseId, string name, string argsJson, string expectedFingerprint = null)
        {
            if (!Guid.TryParseExact(operationId, "D", out Guid id) || world == null || string.IsNullOrWhiteSpace(world.bootId) || world.epoch < 0 ||
                string.IsNullOrWhiteSpace(leaseId) || leaseId.Length > 128 || string.IsNullOrWhiteSpace(name) || name.Length > 128)
                throw CommandJson.Invalid("invalid command identity/world/lease/name");
            if (!(CommandJson.Parse(argsJson) is System.Collections.Generic.Dictionary<string, object>)) throw CommandJson.Invalid("args must be an object");
            this.operationId = id.ToString("D"); this.world = world; this.leaseId = leaseId; this.name = name;
            this.argsJson = CommandJson.Canonicalize(argsJson); this.expectedFingerprint = expectedFingerprint;
        }
    }
    public enum OperationPhase { Queued, Validating, Applying, Verifying, Succeeded, Failed, Cancelled, Interrupted, Indeterminate }

    public sealed class OperationRecord
    {
        public string operationId { get; set; } public string requestHash { get; set; } public WorldRef world { get; set; }
        public string name { get; set; } public string phase { get; set; } public BridgeErrorData error { get; set; }
        public MutationReceipt receipt { get; set; } public System.DateTimeOffset createdAtUtc { get; set; }
        public System.DateTimeOffset updatedAtUtc { get; set; } public bool cancellationRequested { get; set; }
        public bool retrySafe { get; set; }
    }
    public sealed class OperationException : System.Exception
    {
        public BridgeErrorData Error { get; }
        public OperationException(BridgeErrorData error) : base(error.message) { Error = error; }
    }
}

namespace CS2MCP.Core
{
    /// <summary>One game-thread observation interval; does not promise an immutable ECS snapshot.</summary>
    public sealed class Observation<T>
    {
        public WorldRef world { get; set; } public long startFrame { get; set; } public long endFrame { get; set; }
        public System.DateTime startUtc { get; set; } public System.DateTime endUtc { get; set; }
        public T value { get; set; }
    }
}
