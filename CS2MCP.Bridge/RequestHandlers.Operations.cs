using System;
using CS2MCP.Core;

namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        /// <summary>Managed-only protocol refusal; safe on listener threads. O3/O5 must replace the
        /// authority producer before enqueue/apply or scoped status/cancel can become available.</summary>
        internal BridgeResponse HandleOperationRequest(BridgeRequest request)
        {
            string id = request.Command?.operationId ?? request.OperationId;
            try
            {
                if (request.Path == "/v2/commands")
                {
                    CommandCatalog.Resolve(request.Command.name);
                    // Registry validates epoch/readiness and authority on both new admission and replay.
                    // Current authority always refuses, so no native work or operation record is fabricated.
                    var result = m_System.Operations.AdmitWithDisposition(request.Command, request.Command.RequestHash, DateTimeOffset.UtcNow);
                    return BridgeResponse.Json(result.Record, result.IsNew ? 202 : 200);
                }
                // An ID alone does not establish the future scoped lease/lookup authority. Do not expose
                // pure registry Get/RequestCancel as publicly authorized endpoints before O5 supplies it.
                return BridgeResponse.Error(501, "scoped operation status/cancel authority is not implemented", ErrorCodes.Unsupported, operationId: id);
            }
            catch (OperationException error)
            {
                BridgeErrorData e = error.Error;
                return BridgeResponse.Error(e.httpStatus ?? 409, e.message, e.code, e.retryable, e.outcome, id, e.nativeErrors);
            }
        }
    }
}
