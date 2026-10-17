using System;
using System.Threading;
using System.Threading.Tasks;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>Passive managed evidence only. It owns no save, preview, request or native callback.</summary>
    internal sealed class BridgeSaveTaskObservation
    {
        private readonly LifecycleTraceContext context;
        private readonly LifecycleTraceJournal journal;
        private int reserved;

        private BridgeSaveTaskObservation(LifecycleTraceContext context, LifecycleTraceJournal journal)
        {
            this.context = context;
            this.journal = journal;
        }

        internal static BridgeSaveTaskObservation Begin(BridgeRequest request)
        {
            try
            {
                var observer = new BridgeSaveTaskObservation(ConstructionLifecycleTrace.Start(request), ConstructionLifecycleTrace.Journal);
                observer.Record(TracePhase.saveCallAttempt);
                return observer;
            }
            catch
            {
                ConstructionLifecycleTrace.Loss();
                return null;
            }
        }

        internal void CallThrew() => Record(TracePhase.saveCallThrew);

        internal void Returned(Task<bool> task) => Returned(task, Attach);

        // Synchronous fault-injection boundary; the actual SaveGame always uses fixed BCL Attach.
        // The registration delegate is never retained by this state or its continuation.
        internal void Returned(Task<bool> task, Action<Task<bool>, object> register)
        {
            Record(TracePhase.saveCallReturned);
            try
            {
                if (task == null || context == null)
                {
                    Unavailable();
                    return;
                }
                if (!journal.TryReserveSaveTaskObserver())
                {
                    Record(TracePhase.saveObserverSkipped);
                    return;
                }
                // Set before publishing this state to a possibly already-completed antecedent.
                reserved = 1;
                if (ExecutionContext.IsFlowSuppressed())
                    register(task, this);
                else
                    using (ExecutionContext.SuppressFlow())
                        register(task, this);
            }
            catch
            {
                Unavailable();
                Release();
            }
        }

        private static void Attach(Task<bool> task, object state)
        {
            // No ExecuteSynchronously, caller scheduler, ambient execution context or native closure.
            _ = task.ContinueWith(OnCompleted, state, CancellationToken.None,
                TaskContinuationOptions.None, TaskScheduler.Default);
        }

        private static void OnCompleted(Task<bool> task, object state)
        {
            if (state is BridgeSaveTaskObservation observer)
                observer.Completed(task);
        }

        private void Completed(Task<bool> task)
        {
            try
            {
                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        // Result is read only from an immutable terminal state; never wait for it.
                        Record(task.Result ? TracePhase.saveTaskTrue : TracePhase.saveTaskFalse);
                        break;
                    case TaskStatus.Faulted:
                        // Do not inspect/store exception details, native success events or artifacts.
                        Record(TracePhase.saveTaskFaulted);
                        break;
                    case TaskStatus.Canceled:
                        Record(TracePhase.saveTaskCanceled);
                        break;
                    default:
                        journal.NoteLoss();
                        Record(TracePhase.saveObserverUnavailable);
                        break;
                }
            }
            catch
            {
                Loss();
            }
            finally
            {
                Release();
            }
        }

        private void Release()
        {
            if (Interlocked.Exchange(ref reserved, 0) == 0)
                return;
            try { journal.ReleaseSaveTaskObserver(); }
            catch { Loss(); }
        }

        private void Unavailable()
        {
            try { journal.SaveTaskAttachmentFailed(); }
            catch { Loss(); }
            Record(TracePhase.saveObserverUnavailable);
        }

        private void Loss()
        {
            try { journal.NoteLoss(); }
            catch { }
        }

        private void Record(TracePhase phase)
        {
            try
            {
                // Every save-specific phase, including terminal callbacks, samples managed data only.
                // The captured journal/context remain the owner even across reload or test replacement.
                journal.Observe(context, TraceAdapter.save, TraceKind.save, phase,
                    () => Mod.Worlds.Snapshot, () => null, () => DateTime.UtcNow);
            }
            catch { Loss(); }
        }
    }
}
