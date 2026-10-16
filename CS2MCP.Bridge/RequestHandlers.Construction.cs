using CS2MCP.Core;
namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        private Observation<ConstructionSnapshot> ReadConstructionInventory(System.Collections.Generic.IDictionary<string,string> query)
        {
            NativeConstructionReader reader=null;
            return ConstructionInventory.Read(query,Mod.Worlds,()=>m_System.CurrentFrame,entity=> {
                if(reader==null)reader=new NativeConstructionReader(EntityManager,PrefabNameOf);
                return reader.ReadEntity(entity);
            });
        }

        private BridgeResponse ConstructionInventoryRequest(BridgeRequest request)
        {
            try {
                var observation=ReadConstructionInventory(request.Query);
                return BridgeResponse.Json(observation);
            } catch(OperationException error) {
                var e=error.Error;
                return BridgeResponse.Error(e.httpStatus??400,e.message,e.code,e.retryable,e.outcome,e.operationId);
            }
        }
    }
}
