using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;
internal static class ObservationHttpTests
{
    sealed class Readers:IObservationReaders {
        internal int Count;internal double Millis;internal object Data=new CityStateData{gameMode="Game",cityLoaded=true,cityName="City"};
        public Observation<MetricBatch> Metrics(MetricReadRequest request) => throw new InvalidOperationException("unused metric source");
        public Observation<TransitPage> Transit(TransitQuery query) => TransitReader.Read(query,Ready(),()=>10,new TestTransitSource());
            static WorldIdentity Ready(){var w=new WorldIdentity("boot");w.BeginLoad();w.CompleteLoad();return w;}
            public bool Warming=>false;public object State(){Count++;Millis=2;return Data;}
        public object Overview()=>State();public object Services()=>State();public object Labor()=>State();public object Budget()=>State();
        public Observation<ConstructionSnapshot> Inventory(System.Collections.Generic.IDictionary<string,string> q)=>throw new InvalidOperationException();
    }
    static void Equal(object expected,object actual){if(!Equals(expected,actual))throw new Exception($"expected {expected}, got {actual}");}
    static void Await(Func<bool> predicate){if(!SpinWait.SpinUntil(predicate,3000))throw new Exception("bounded test wait expired");}
    internal static void Run(Action<string,Action> check)
    {
        check("actual server disconnect detaches before native work and after partial dispatch",()=>{
            foreach(bool partial in new[]{false,true}){
                var worlds=new WorldIdentity("boot");worlds.BeginLoad();worlds.CompleteLoad();var readers=new Readers();
                var scheduler=new ObservationScheduler(worlds,new ObservationCatalog(readers,()=>10,()=>DateTime.UtcNow),()=>10,()=>readers.Millis,new ObservationWire());
                var server=new HttpBridgeServer(0,request=>{var group=scheduler.Admit(request.Observations,bytes=>request.Complete(new BridgeResponse{Body=bytes}));request.AttachObservation(group.Detach);return true;},_=>throw new Exception("wrong operation router"),worlds);
                server.Start();
                try{
                    var listener=(TcpListener)typeof(HttpBridgeServer).GetField("m_Listener",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(server);
                    using(var client=new TcpClient()){
                        client.Connect(IPAddress.Loopback,((IPEndPoint)listener.LocalEndpoint).Port);
                        string body="{\"items\":[{\"id\":\"a\",\"name\":\"state\",\"args\":{}},{\"id\":\"b\",\"name\":\"state\",\"args\":{}}]}";
                        byte[] data=Encoding.UTF8.GetBytes("POST /v2/observations HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/json\r\nContent-Length: "+Encoding.UTF8.GetByteCount(body)+"\r\n\r\n"+body);
                        client.GetStream().Write(data,0,data.Length);Await(()=>scheduler.Pending==2);
                        if(partial){scheduler.Pump();Equal(1,scheduler.Pending);Equal(1,readers.Count);}
                        client.Client.Shutdown(SocketShutdown.Both);
                    }
                    Await(()=>scheduler.ActiveGroups==0);Equal(0,scheduler.Pending);readers.Millis=0;scheduler.Pump();Equal(partial?1:0,readers.Count);
                }finally{server.Stop();scheduler.DetachAll();}
            }
        });
        check("actual request timeout detaches reads once while ordinary writer completion remains available",()=>{
            var request=new BridgeRequest{Observations=ObservationCatalog.Parse("{\"items\":[{\"id\":\"a\",\"name\":\"state\",\"args\":{}}]}")};int releases=0;
            request.AttachObservation(()=>releases++);Equal(null,request.WaitForResponse(1));request.DetachObservation();request.Complete(new BridgeResponse());Equal(1,releases);Equal(null,request.WaitForResponse(1));
            var writer=new BridgeRequest();Equal(null,writer.WaitForResponse(1));var reply=new BridgeResponse();writer.Complete(reply);Equal(reply,writer.WaitForResponse(1));
        });
        check("actual observation serializer keeps required null evidence and exactly one union member",()=>{
            var wire=new ObservationWire();var row=new ObservationItemResult{id="雪",observation=new Observation<object>{data=new CityOverviewData(),world=new WorldRef("boot",1),sampledAtUtc=DateTime.UtcNow,startFrame=1,endFrame=1,freshness="current",warnings=new[]{"populationComponentUnavailableLegacyDefaults"}}};
            byte[] bytes=wire.Item(row);var json=JObject.Parse(Encoding.UTF8.GetString(bytes));Equal(null,json["error"]);Equal(JTokenType.Object,json["observation"]["data"].Type);Equal(null,json["observation"]["data"]["PopulationAvailable"]);Equal(null,json["observation"]["data"]["ObservationWarnings"]);
            var batch=JObject.Parse(Encoding.UTF8.GetString(wire.Batch(new ObservationBatchResult{world=null,startFrame=null,endFrame=null,results=new[]{new ObservationItemResult{id="a",error=new BridgeErrorData(ErrorCodes.NotReady,"not ready")}}})));
            Equal(JTokenType.Null,batch["world"].Type);Equal(JTokenType.Null,batch["startFrame"].Type);Equal(null,batch["results"][0]["observation"]);Equal(null,batch["results"][0]["error"]["nativeErrors"]);
        });
        check("actual observation ingress rejects wrong methods query framing and strict body errors before callback",()=>{
            string valid="{\"items\":[{\"id\":\"a\",\"name\":\"state\",\"args\":{}}]}";
            string Header(string body,string path="/v2/observations",string type="application/json")=>"POST "+path+" HTTP/1.1\r\nContent-Type: "+type+"\r\nContent-Length: "+Encoding.UTF8.GetByteCount(body)+"\r\n\r\n"+body;
            foreach(string raw in new[]{"GET /v2/observations HTTP/1.1\r\n\r\n",Header(valid,"/v2/observations?x=1"),Header(valid,type:"text/plain"),Header(valid.Replace("\"args\":{}","\"args\":{\"x\":1}")),Header(valid.Replace("\"state\"","\"cs2_save_game\"")),Header(valid.Replace("\"id\":\"a\"","\"id\":\"a\",\"id\":\"b\""))}){
                bool called=false;try{using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(raw)))HttpIngress.ReadAndDispatch(stream,_=>called=true);throw new Exception("invalid observation accepted");}catch(OperationException){}Equal(false,called);
            }
            bool accepted=false;using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(Header(valid))))HttpIngress.ReadAndDispatch(stream,parsed=>{accepted=true;Equal(true,parsed.Observations!=null);});Equal(true,accepted);
        });
        check("actual group UTF8 cap reserves every remaining ordered error with worst-case IDs and world",()=>{
            var worlds=new WorldIdentity(new string('\u0001',128));worlds.BeginLoad();worlds.CompleteLoad();
            var readers=new Readers{Data=new System.Collections.Generic.Dictionary<string,object>{{"payload",new string('雪',81000)}}};var wire=new ObservationWire();
            var items=Enumerable.Range(0,64).Select(i=>(object)new System.Collections.Generic.Dictionary<string,object>{{"id",new string('\u0001',124)+i.ToString("D4")},{"name","state"},{"args",new System.Collections.Generic.Dictionary<string,object>()}}).ToArray();
            string json=CommandJson.Stringify(new System.Collections.Generic.Dictionary<string,object>{{"items",items}});byte[] bytes=null;
            var scheduler=new ObservationScheduler(worlds,new ObservationCatalog(readers,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,wire);var group=scheduler.Admit(ObservationCatalog.Parse(json),value=>bytes=value);
            for(int i=0;i<8;i++)scheduler.Pump();Equal(64,readers.Count);Equal(true,bytes.Length<=ObservationScheduler.MaxGroupBytes);Equal(true,bytes.Length>3000000);
            var results=group.Result.results;Equal(64,results.Length);Equal(true,results.Any(row=>row.error?.code=="RESULT_TOO_LARGE"));Equal(true,results.Any(row=>row.error?.message==ConstructionReadDiagnostics.GroupBytes));Equal(true,results.Any(row=>row.observation!=null));
            for(int i=0;i<64;i++){Equal(new string('\u0001',124)+i.ToString("D4"),results[i].id);Equal(true,wire.Item(results[i]).Length<=ObservationScheduler.MaxItemBytes);}
            Equal(64,((JArray)JObject.Parse(Encoding.UTF8.GetString(bytes))["results"]).Count);group.Detach();Equal(0,scheduler.Pending);Equal(0,scheduler.ActiveGroups);
        });
        check("actual UTF8 item/group accounting converts oversized data to ordered errors within total cap",()=>{
            var worlds=new WorldIdentity("boot");worlds.BeginLoad();worlds.CompleteLoad();var readers=new Readers{Data=new System.Collections.Generic.Dictionary<string,object>{{"payload",new string('雪',100000)}}};
            var scheduler=new ObservationScheduler(worlds,new ObservationCatalog(readers,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());byte[] bytes=null;
            var group=scheduler.Admit(ObservationCatalog.Parse("{\"items\":[{\"id\":\"a\",\"name\":\"state\",\"args\":{}}]}"),value=>bytes=value);scheduler.Pump();
            Equal("RESULT_TOO_LARGE",group.Result.results[0].error.code);Equal(true,bytes.Length<=ObservationScheduler.MaxGroupBytes);Equal(1,((JArray)JObject.Parse(Encoding.UTF8.GetString(bytes))["results"]).Count);group.Detach();
        });
    }
}
namespace CS2MCP
{
    // Only game-global logging/build metadata are doubled. BridgeRequest, HTTP server and serializer are real production sources.
    internal static class Mod {
        internal static readonly WorldIdentity Worlds=ReadyWorld();
        static WorldIdentity ReadyWorld(){var w=new WorldIdentity("boot");w.BeginLoad();w.CompleteLoad();return w;}
        internal const string Name="test",Version="test";internal static readonly BuildIdentity Identity=new BuildIdentity("test","test",null,null);
        internal static readonly RuntimeSnapshotStore RuntimeSnapshots=new RuntimeSnapshotStore();internal static readonly NativeValidationMetadata NativeValidation=new NativeValidationMetadata(null);
        internal static readonly TestLog Log=new TestLog();internal sealed class TestLog{internal void Warn(string message){}}
    }
}
