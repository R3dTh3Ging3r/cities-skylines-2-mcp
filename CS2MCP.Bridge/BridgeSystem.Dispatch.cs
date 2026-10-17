using System;
using CS2MCP.Core;

namespace CS2MCP
{
    public sealed partial class BridgeSystem
    {
        private bool TryEnqueue(BridgeRequest request)
        {
            if (request.Observations != null)
            {
                var group = m_Scheduler.Admit(request.Observations, bytes => request.Complete(new BridgeResponse { Body = bytes }));
                request.AttachObservation(group.Detach);
                return true;
            }

            EndpointDescriptor endpoint = EndpointCatalog.Resolve(request.Path, out _);
            bool writer = endpoint == null || endpoint.access != "read";
            return m_Scheduler.TryOrdinary(writer, () => DispatchOrdinary(request, writer));
        }

        private void DispatchOrdinary(BridgeRequest request, bool writer)
        {
            BridgeResponse response;
            if (writer)
                ConstructionLifecycleTrace.Record(ConstructionLifecycleTrace.Start(request), TraceAdapter.envelope, TraceKind.direct, TracePhase.dispatch);
            try
            {
                // Legacy ingress fencing is weaker than caller-observed world/lease authority.
                // O5 still owns all-writer admission; ordinary mutation timeouts remain unknown outcomes.
                if (writer)
                    Mod.Worlds.Validate(request.IngressWorld);
                response = m_Handlers.Handle(request);
            }
            catch (OperationException error)
            {
                ConstructionLifecycleTrace.Record(request.LifecycleContext, TraceAdapter.envelope, TraceKind.direct, TracePhase.exceptionObserved);
                var e = error.Error;
                response = BridgeResponse.Error(e.httpStatus ?? 409, e.message, e.code, e.retryable, e.outcome, e.operationId);
            }
            catch (Exception error)
            {
                ConstructionLifecycleTrace.Record(request.LifecycleContext, TraceAdapter.envelope, TraceKind.direct, TracePhase.exceptionObserved);
                Mod.Log.Warn($"error handling {request.Method} {request.Path}: {error}");
                response = BridgeResponse.Error(500, $"{error.GetType().Name}: {error.Message}");
            }

            if (response != null)
                request.Complete(response);
        }
    }
}
