// BCL-only boundary doubles for the linked, unmodified production HTTP server.
// Routing tests execute its real Dispatch method; no game or serializer is loaded.
using System;
using System.Collections.Generic;
using CS2MCP.Core;
namespace CS2MCP
{
    public sealed class BridgeRequest
    {
        public string Method, Path, Body, OperationId;
        public CommandRequest Command;
        public WorldRef IngressWorld;
        public Dictionary<string,string> Query=new Dictionary<string,string>();
        private BridgeResponse response;
        public void Complete(BridgeResponse value){response=value;}
        public BridgeResponse WaitForResponse(int timeout){return response;}
    }
    public sealed class BridgeResponse
    {
        public int Status=200;public string ContentType="application/json";public byte[] Body=new byte[0];
        public static BridgeResponse Json(object payload){return new BridgeResponse();}
        public static BridgeResponse Error(int status,string message,string code,bool retryable=false,string outcome="notApplied",string operationId=null){return new BridgeResponse{Status=status};}
    }
    internal static class Mod
    {
        internal const string Name="test",Version="test";
        internal static readonly BuildIdentity Identity=new BuildIdentity("test","test",null,null);
        internal static readonly RuntimeSnapshotStore RuntimeSnapshots=new RuntimeSnapshotStore();
        internal static readonly NativeValidationMetadata NativeValidation=new NativeValidationMetadata(null);
        internal static readonly TestLog Log=new TestLog();
        internal sealed class TestLog { internal void Warn(string message){} }
    }
}
