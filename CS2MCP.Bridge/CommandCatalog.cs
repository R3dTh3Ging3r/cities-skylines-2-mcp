using System;
using System.Linq;
using CS2MCP.Core;

namespace CS2MCP
{
    public sealed class CommandDescriptor
    {
        public string Name { get; } public string HandlerPath { get; } public string Access { get; } public string CompletionKind { get; }
        internal CommandDescriptor(EndpointDescriptor endpoint) { Name = endpoint.capability; HandlerPath = endpoint.path; Access = endpoint.access; CompletionKind = endpoint.completionMechanism; }
    }
    /// <summary>Only fixed classified writer names. Resolving a descriptor grants no execution authority.</summary>
    public static class CommandCatalog
    {
        public static CommandDescriptor Resolve(string name)
        {
            EndpointDescriptor endpoint = EndpointCatalog.All.SingleOrDefault(e => e.capability == name && e.access != "read" && !e.path.StartsWith("/v2/", StringComparison.Ordinal));
            if (endpoint == null) throw new OperationException(new BridgeErrorData(ErrorCodes.Unsupported, "unknown command", httpStatus: 404));
            return new CommandDescriptor(endpoint);
        }
    }
}
