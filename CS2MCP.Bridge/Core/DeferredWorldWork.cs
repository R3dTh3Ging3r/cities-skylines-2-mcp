using System;

namespace CS2MCP.Core
{
    /// <summary>Game-thread continuation fence, not mutation/lease authority. Keep the original
    /// ingress world; never construct a new world identity after an effect has been deferred.
    /// Ready=false is reserved for diagnostic screen capture outside a loaded ready city.</summary>
    public sealed class DeferredWorldWork
    {
        private readonly WorldIdentity identity;
        private readonly string outcome, operationId;
        private BridgeErrorData discardedError;
        public WorldRef World { get; }
        public bool Ready { get; }

        public DeferredWorldWork(WorldIdentity identity, WorldRef world, bool ready, string outcome, string operationId = null)
        {
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            World = world;
            Ready = ready;
            // Validate the retained effect classification without claiming that the callback has not applied.
            this.outcome = new BridgeErrorData(ErrorCodes.OutcomeUnknown, "deferred work", outcome: outcome).outcome;
            this.operationId = operationId;
        }

        private OperationException Reject(string code, string message, int status) => new OperationException(
            new BridgeErrorData(code, message, outcome: outcome, httpStatus: status, operationId: operationId));

        public void ThrowIfInvalid()
        {
            if (discardedError != null) throw new OperationException(CopyError(discardedError));
            WorldSnapshot current = identity.Snapshot;
            if (!WorldIdentity.Same(World, current.world))
                throw Reject(ErrorCodes.WorldMismatch, "deferred work belongs to an earlier world; prior effects were not reconciled in the current world", 409);
            if (current.ready != Ready)
                throw Reject(ErrorCodes.NotReady, "deferred work crossed a world-readiness boundary", 503);
        }

        private static BridgeErrorData CopyError(BridgeErrorData error) => new BridgeErrorData(error.code, error.message,
            outcome: error.outcome, httpStatus: error.httpStatus, operationId: error.operationId,
            nativeErrors: error.nativeErrors == null ? null : (string[])error.nativeErrors.Clone());

        public bool TryRun(Action action, out BridgeErrorData error)
            => TryRun(() => { action(); return true; }, out _, out error);

        public bool TryRun<T>(Func<T> action, out T result, out BridgeErrorData error)
        {
            result = default;
            error = null;
            try
            {
                ThrowIfInvalid();
                T candidate = action();
                // A callback can synchronously observe or trigger invalidation. Never publish its stale result.
                ThrowIfInvalid();
                result = candidate;
                return true;
            }
            catch (OperationException failure)
            {
                var source = failure.Error;
                error = new BridgeErrorData(source.code, source.message, outcome: outcome,
                    httpStatus: source.httpStatus, operationId: operationId, nativeErrors: source.nativeErrors);
            }
            catch (Exception failure)
            {
                error = new BridgeErrorData(ErrorCodes.OutcomeUnknown,
                    $"deferred callback failed ({failure.GetType().Name}: {failure.Message})",
                    outcome: outcome, httpStatus: 500, operationId: operationId);
            }
            // Recovered readiness must not revive discarded native work or a later idle restoration.
            if (discardedError == null) discardedError = CopyError(error);
            return false;
        }
    }

    /// <summary>Legacy timer safety only. It does not claim supervised-run completion or cancel
    /// simulation effects. Readiness/load/disposal invalidation must call Clear on the game thread.</summary>
    public sealed class WorldBoundPause
    {
        private readonly WorldIdentity identity;
        private DeferredWorldWork work;
        public uint TargetFrame { get; private set; }

        public WorldBoundPause(WorldIdentity identity) { this.identity = identity ?? throw new ArgumentNullException(nameof(identity)); }

        public void Arm(WorldRef world, uint target)
        {
            Clear();
            if (target == 0) return;
            var candidate = new DeferredWorldWork(identity, world, true, "partial");
            candidate.ThrowIfInvalid();
            work = candidate;
            TargetFrame = target;
        }

        public void Clear() { work = null; TargetFrame = 0; }

        public bool Poll(uint frame, Action pause, out BridgeErrorData error)
        {
            error = null;
            if (work == null) return false;
            if (!work.TryRun(() => { }, out error)) { Clear(); return false; }
            if (frame < TargetFrame) return false;
            DeferredWorldWork pending = work;
            // Never retry a setter that throws or finishes after invalidation.
            Clear();
            return pending.TryRun(pause, out error);
        }
    }
}
