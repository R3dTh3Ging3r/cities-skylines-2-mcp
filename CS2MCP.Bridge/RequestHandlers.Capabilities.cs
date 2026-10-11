using System;

namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        public BridgeResponse GetCapabilities(BridgeRequest request)
        {
            return BridgeResponse.Json(EndpointCatalog.CreateValidationManifest(Mod.Identity,
                Mod.RuntimeSnapshots.Read(DateTime.UtcNow), Mod.NativeValidation));
        }
    }
}
