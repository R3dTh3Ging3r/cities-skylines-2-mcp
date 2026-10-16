using CS2MCP.Core;
using Game.Prefabs;

namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        private Observation<TransitPage> ReadTransitData(TransitQuery query)
        {
            Mod.Worlds.Validate(query.World);
            var source = new NativeTransitReader(EntityManager, World.GetOrCreateSystemManaged<PrefabSystem>());
            return TransitReader.Read(query, Mod.Worlds, () => m_System.CurrentFrame, source);
        }

        private BridgeResponse TransitStatusRequest(BridgeRequest request)
        {
            try
            {
                var observation = ReadTransitData(TransitQuery.Parse(request.Query));
                try
                {
                    return new BridgeResponse
                    {
                        Body = ObservationWire.Standalone(observation)
                    };
                }
                catch (System.InvalidOperationException)
                {
                    return BridgeResponse.Error(413, "selected transit exceeds bounded response budget", "RESULT_TOO_LARGE");
                }
            }
            catch (OperationException error)
            {
                var e = error.Error;
                return BridgeResponse.Error(e.httpStatus ?? 400, e.message, e.code, e.retryable, e.outcome, e.operationId);
            }
        }

        private BridgeResponse TransitCapabilitiesRequest(BridgeRequest request)
        {
            if (request.Query.Count != 0)
                return BridgeResponse.Error(400, "transit capabilities accepts no query", ErrorCodes.InvalidArgument);
            return BridgeResponse.Json(new TransitCapabilities());
        }
    }
}
