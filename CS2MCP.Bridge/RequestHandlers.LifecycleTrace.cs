using System;
using System.Collections.Generic;
using CS2MCP.Core;

namespace CS2MCP
{
    public sealed partial class RequestHandlers : ILifecycleTraceReader
    {
        public BridgeResponse Handle(BridgeRequest request)
        {
            try
            {
                if (request.Path == "/diagnostics/construction-lifecycle")
                    return LifecycleTraceRequest(request);
                return HandleCore(request);
            }
            catch (Exception error)
            {
                Mod.Log.Warn($"error handling {request.Method} {request.Path}: {error}");
                return BridgeResponse.Error(500, $"{error.GetType().Name}: {error.Message}", ErrorCodes.OutcomeUnknown, retryable: false, outcome: "unknown");
            }
        }

        private static BridgeResponse LifecycleTraceRequest(BridgeRequest request)
        {
            try
            {
                if (request.Method != "GET" || !string.IsNullOrEmpty(request.Body))
                    throw CommandJson.Invalid("trace read requires GET without body");
                // Managed snapshots only; intentionally readable while the city is unavailable.
                return new BridgeResponse
                {
                    Body = ObservationWire.Standalone(ConstructionLifecycleTrace.Read(request.Query))
                };
            }
            catch (OperationException error)
            {
                var e = error.Error;
                return BridgeResponse.Error(e.httpStatus ?? 400, e.message, e.code, outcome: "notApplied");
            }
            catch (ObservationOutputLimitException)
            {
                return BridgeResponse.Error(413, "lifecycle trace page exceeds byte limit", "RESULT_TOO_LARGE");
            }
            catch
            {
                return BridgeResponse.Error(500, "lifecycle trace unavailable", ErrorCodes.ProtocolError, outcome: "unknown");
            }
        }

        Observation<LifecycleTracePage> ILifecycleTraceReader.LifecycleTrace(LifecycleTraceQuery query)
        {
            long start = m_System.CurrentFrame;
            var page = ConstructionLifecycleTrace.Journal.Read(query, Mod.Worlds.Snapshot, DateTime.UtcNow);
            long end = m_System.CurrentFrame;
            return new Observation<LifecycleTracePage>
            {
                data = page,
                world = page.readWorld,
                startFrame = start,
                endFrame = end,
                sampledAtUtc = page.readAtUtc,
                freshness = "current",
                warnings = start == end ? new[]
                {
                    "managedHistoryNotNativeObservation",
                    "nativeLifecycleNotRun"
                }

                : new[]
                {
                    "managedHistoryNotNativeObservation",
                    "nativeLifecycleNotRun",
                    "multiFrameReadNotAtomic"
                }
            };
        }
    }
}
