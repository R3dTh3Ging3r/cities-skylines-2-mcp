using CS2MCP.Core;
namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        private BridgeResponse ConstructionInventoryRequest(BridgeRequest request)
        {
            try {
                NativeConstructionReader reader=null;
                var observation=ConstructionInventory.Read(request.Query,Mod.Worlds,
                    ()=>m_System.CurrentFrame,
                    entity=> {
                        if(reader==null)reader=new NativeConstructionReader(EntityManager,PrefabNameOf);
                        return reader.ReadEntity(entity);
                    });
                return BridgeResponse.Json(observation);
            } catch(OperationException error) {
                var e=error.Error;
                return BridgeResponse.Error(e.httpStatus??400,e.message,e.code,e.retryable,e.outcome,e.operationId);
            }
        }
    }
}
