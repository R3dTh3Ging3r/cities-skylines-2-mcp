using CS2MCP.Core;
namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        private Observation<ConstructionSnapshot> ReadConstructionInventory(System.Collections.Generic.IDictionary<string,string> query)
        {
            var reader=new NativeConstructionReader(()=>EntityManager,PrefabNameOf);
            return ConstructionInventory.Read(query,Mod.Worlds,()=>m_System.CurrentFrame,reader.ReadEntity,reader);
        }

        internal static BridgeResponse BoundedConstructionResponse(object observation)
        {
            try {return new BridgeResponse{Body=ObservationWire.Standalone(observation)};}
            catch(System.InvalidOperationException) {
                return BridgeResponse.Error(413,"inventory result exceeds256KiB; select fewer roots or omit expansion","RESULT_TOO_LARGE");
            }
        }

        private BridgeResponse ConstructionInventoryRequest(BridgeRequest request)
        {
            try {
                var observation=ReadConstructionInventory(request.Query);
                return BoundedConstructionResponse(observation);
            } catch(OperationException error) {
                var e=error.Error;
                return BridgeResponse.Error(e.httpStatus??400,e.message,e.code,e.retryable,e.outcome,e.operationId);
            }
        }
    }
}
