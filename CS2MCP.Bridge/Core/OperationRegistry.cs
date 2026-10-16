using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2MCP.Core
{
    public sealed class AdmissionResult
    {
        public bool IsNew { get; } public OperationRecord Record { get; }
        internal AdmissionResult(bool isNew, OperationRecord record) { IsNew = isNew; Record = record; }
    }
    /// <summary>One epoch, no key eviction. Own and call from the game thread; the lock also makes
    /// admission disposition atomic for concurrent managed callers. Authority must be a managed policy.
    /// An old registry is frozen on world change; replace it for the next epoch, never reset/replay it.</summary>
    public sealed class OperationRegistry
    {
        public const int Capacity = 2048, PendingCapacity = 256;
        private sealed class Entry { internal CommandRequest Request; internal OperationRecord Record; internal bool Pending = true; }
        private readonly Dictionary<string, Entry> records = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate = new object();
        private readonly WorldIdentity identity; private readonly WorldRef epoch;
        private readonly Func<CommandRequest, BridgeErrorData> authority;
        private int pending;
        public OperationRegistry(WorldIdentity world, Func<CommandRequest, BridgeErrorData> authority)
        {
            identity = world ?? throw new ArgumentNullException(nameof(world)); epoch = world.Current;
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
        }
        private static OperationException Reject(string code, string message, string id, int status = 409, string outcome = "notApplied")
            => new OperationException(new BridgeErrorData(code, message, outcome: outcome, httpStatus: status, operationId: id));
        private void Validate(CommandRequest request)
        {
            if (!WorldIdentity.Same(epoch, request.world)) throw Reject(ErrorCodes.WorldMismatch, "registry belongs to a different epoch", request.operationId);
            try { identity.Validate(request.world); }
            catch (OperationException e) { e.Error.operationId = request.operationId; throw; }
            BridgeErrorData error = authority(request);
            if (error != null) { error.operationId = request.operationId; throw new OperationException(error); }
        }
        public OperationRecord Admit(CommandRequest request, string requestHash, DateTimeOffset now)
            => AdmitWithDisposition(request, requestHash, now).Record;
        public AdmissionResult AdmitWithDisposition(CommandRequest request, string requestHash, DateTimeOffset now)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            lock (gate)
            {
                InvalidateOldWorld(now); Validate(request);
                if (requestHash != request.RequestHash) throw Reject(ErrorCodes.InvalidArgument, "requestHash does not match canonical request", request.operationId, 400);
                if (records.TryGetValue(request.operationId, out Entry old))
                {
                    if (old.Record.requestHash != requestHash) throw Reject(ErrorCodes.IdempotencyConflict, "operation ID already belongs to a different request", request.operationId);
                    if (old.Request.leaseId != request.leaseId) throw Reject(ErrorCodes.Locked, "operation ID is bound to another lease", request.operationId, 403);
                    return new AdmissionResult(false, Copy(old.Record));
                }
                if (records.Count >= Capacity) throw Reject(ErrorCodes.OperationCapacity, "operation registry is full for this epoch", request.operationId);
                if (pending >= PendingCapacity) throw Reject(ErrorCodes.BridgeBusy, "pending operation queue is full", request.operationId);
                var record = new OperationRecord { operationId = request.operationId, requestHash = requestHash, world = request.world,
                    name = request.name, phase = "queued", createdAtUtc = now, updatedAtUtc = now, retrySafe = true };
                records.Add(request.operationId, new Entry { Request = request, Record = record }); pending++;
                return new AdmissionResult(true, Copy(record));
            }
        }
        private Entry Find(string id)
        {
            if (!Guid.TryParseExact(id, "D", out Guid parsed)) throw CommandJson.Invalid("operationId must be a UUID");
            if (!records.TryGetValue(parsed.ToString("D"), out Entry entry)) throw Reject(ErrorCodes.OutcomeUnknown,
                "no retained operation in this registry; reconcile before creating new work", id, 404, "unknown");
            return entry;
        }
        public OperationRecord Get(string id) { lock (gate) { InvalidateOldWorld(DateTimeOffset.UtcNow); return Copy(Find(id).Record); } }
        public void ValidateForApply(string id)
        {
            lock (gate) { InvalidateOldWorld(DateTimeOffset.UtcNow); Entry entry = Find(id); Validate(entry.Request);
                if (Terminal(entry.Record.phase) || entry.Record.cancellationRequested) throw Reject(ErrorCodes.Locked, "operation cannot apply", id, 403); }
        }
        private void ReleasePending(Entry entry) { if (entry.Pending) { entry.Pending = false; pending--; } }
        private void InvalidateOldWorld(DateTimeOffset now)
        {
            if (WorldIdentity.Same(epoch, identity.Current)) return;
            foreach (Entry entry in records.Values.Where(e => !Terminal(e.Record.phase)))
            {
                bool started = entry.Record.phase == "applying" || entry.Record.phase == "verifying";
                entry.Record.phase = started ? "indeterminate" : "interrupted";
                entry.Record.error = new BridgeErrorData(ErrorCodes.WorldMismatch, "world changed", outcome: started ? "unknown" : "notApplied", operationId: entry.Record.operationId);
                entry.Record.updatedAtUtc = now; ReleasePending(entry);
            }
        }
        public static bool Terminal(string phase) => phase == "succeeded" || phase == "failed" || phase == "cancelled" || phase == "interrupted" || phase == "indeterminate";
        public OperationRecord Transition(string id, OperationPhase phase, MutationReceipt receipt, BridgeErrorData error, DateTimeOffset now)
            => Transition(id, phase.ToString().ToLowerInvariant(), receipt, error, now);
        public OperationRecord Transition(string id, string phase, MutationReceipt receipt, BridgeErrorData error, DateTimeOffset now)
        {
            lock (gate)
            {
                InvalidateOldWorld(now); Entry entry = Find(id); string from = entry.Record.phase;
                if (Terminal(from)) throw new InvalidOperationException("terminal outcomes require explicit scoped reconciliation");
                bool allowed = from == "queued" && (phase == "validating" || phase == "failed" || phase == "cancelled") ||
                    from == "validating" && (phase == "applying" || phase == "failed" || phase == "cancelled") ||
                    from == "applying" && (phase == "verifying" || phase == "indeterminate" || phase == "failed") ||
                    from == "verifying" && (phase == "succeeded" || phase == "failed" || phase == "indeterminate");
                if (!allowed) throw new InvalidOperationException("invalid phase transition: " + from + " -> " + phase);
                if (phase == "applying") Validate(entry.Request);
                if (phase == "succeeded" && (receipt == null || receipt.verification != "verified")) throw new InvalidOperationException("success requires verified receipt");
                if (phase == "failed" && error == null) throw new InvalidOperationException("failure requires error evidence");
                MutationReceipt detachedReceipt = CopyReceipt(receipt);
                BridgeErrorData detachedError = CopyError(error);
                entry.Record.phase = phase; entry.Record.receipt = detachedReceipt; entry.Record.error = detachedError; entry.Record.updatedAtUtc = now;
                if (phase != "queued" && phase != "validating") ReleasePending(entry);
                if (Terminal(phase)) ReleasePending(entry);
                return Copy(entry.Record);
            }
        }
        public OperationRecord RequestCancel(string id)
        {
            lock (gate)
            {
                InvalidateOldWorld(DateTimeOffset.UtcNow); Entry entry = Find(id);
                if (!Terminal(entry.Record.phase))
                {
                    entry.Record.cancellationRequested = true; entry.Record.updatedAtUtc = DateTimeOffset.UtcNow;
                    if (entry.Record.phase == "queued" || entry.Record.phase == "validating") { entry.Record.phase = "cancelled"; ReleasePending(entry); }
                }
                return Copy(entry.Record);
            }
        }
        private static BridgeErrorData CopyError(BridgeErrorData error) => error == null ? null : new BridgeErrorData(error.code, error.message, error.retryable, error.outcome, error.httpStatus,
            error.operationId, error.nativeErrors == null ? null : (string[])error.nativeErrors.Clone());
        private static EntityRef[] CopyEntities(EntityRef[] entities) => entities?.Select(e => e == null ? null : new EntityRef { world = e.world, index = e.index, version = e.version }).ToArray();
        private static MutationReceipt CopyReceipt(MutationReceipt r) => r == null ? null : new MutationReceipt {
            created = CopyEntities(r.created), changed = CopyEntities(r.changed), reused = CopyEntities(r.reused), removed = CopyEntities(r.removed), actualCost = r.actualCost,
            costStatus = r.costStatus, result = CommandJson.Snapshot(r.result), warnings = r.warnings == null ? null : (string[])r.warnings.Clone(), verification = r.verification };
        private static OperationRecord Copy(OperationRecord r) => new OperationRecord { operationId = r.operationId, requestHash = r.requestHash, world = r.world,
            name = r.name, phase = r.phase, error = CopyError(r.error), receipt = CopyReceipt(r.receipt), createdAtUtc = r.createdAtUtc, updatedAtUtc = r.updatedAtUtc,
            cancellationRequested = r.cancellationRequested, retrySafe = r.retrySafe };
    }
}
