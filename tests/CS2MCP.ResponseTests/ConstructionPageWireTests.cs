using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CS2MCP;
using CS2MCP.Core;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

internal static class ConstructionPageWireTests
{
    static Entity E(int index,int version=1)=>new Entity{Index=index,Version=version};
    static Entity Child(int slot)=>slot==65?Entity.Null:slot==66?E(-3):slot==64||slot==67?E(2):slot==68?E(99):E(slot%20+10);
    static void Equal(object expected,object actual){if(!Equals(expected,actual))throw new Exception($"expected {expected}, got {actual}");}
    static RequestHandlers Fixture()
    {
        var h=new RequestHandlers();
        h.EntityManager.Add(E(1),new Game.Net.Edge{m_Start=E(200),m_End=E(201)},new Game.Net.Curve{m_Length=99,m_Bezier=new Game.Net.Bezier{
            a=new float3(0,0,0),b=new float3(1,2,0),c=new float3(2,4,0),d=new float3(3,6,0)}},new Game.Common.Owner{m_Owner=E(2)},new Game.Objects.Attached{m_Parent=E(2)});
        h.EntityManager.Buffer(E(1),Enumerable.Range(0,129).Select(i=>new Game.Objects.SubObject{m_SubObject=Child(i)}).ToArray());
        h.EntityManager.Buffer(E(1),new Game.Buildings.InstalledUpgrade{m_Upgrade=E(2)});
        h.EntityManager.Buffer(E(1),new Game.Net.SubNet{m_SubNet=E(2)});
        h.EntityManager.Buffer(E(1),new Game.Areas.SubArea{m_Area=E(2)});
        h.EntityManager.Add(E(2),new Game.Buildings.Building());
        return h;
    }
    static Dictionary<string,string> Query(int offset=64,int limit=5,string kind="subObject")=>new Dictionary<string,string>{
        {"bootId","boot"},{"epoch","1"},{"entities","1:1"},{"relationshipKind",kind},{"relationshipOffset",offset.ToString()},{"relationshipLimit",limit.ToString()},{"geometryDiagnostics","sampledCubicV1"}};
    static BridgeResponse Direct(RequestHandlers h,Dictionary<string,string> query)=>(BridgeResponse)typeof(RequestHandlers).GetMethod("ConstructionInventoryRequest",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(h,new object[]{new BridgeRequest{Query=query}});
    static JObject Json(BridgeResponse response)=>JObject.Parse(Encoding.UTF8.GetString(response.Body));
    static JObject Args(int offset=64,int limit=5)=>new JObject{{"world",JToken.FromObject(Mod.Worlds.Current)},{"entities",new JArray(JToken.FromObject(new EntityRef{world=Mod.Worlds.Current,index=1,version=1}))},
        {"relationshipPage",new JObject{{"kind","subObject"},{"offset",offset},{"limit",limit}}},{"geometryDiagnostics","sampledCubicV1"}};
    internal static void Run(Action<string,Action> check)
    {
        check("actual native page reads selected raw indices only with zero target checks or geometry",()=>{
            var handler=Fixture();var response=Direct(handler,Query());Equal(200,response.Status);var direct=Json(response);var page=direct["data"]["relationshipPage"];
            Equal(129,(int)page["observedCount"]);Equal(129,(int)page["endObservedCount"]);Equal(5,(int)page["returnedCount"]);Equal(69,(int)page["nextOffset"]);
            Equal("partial",(string)page["sourceCoverage"]);Equal("notRequested",(string)page["targetResolution"]);Equal("unverified",(string)page["crossPageConsistency"]);
            Equal(5,handler.EntityManager.ElementReads);Equal(13,handler.EntityManager.HeaderReads);Equal(1,handler.EntityManager.ComponentReadsByType[typeof(Game.Net.Curve)]);
            Equal(true,handler.EntityManager.ExistsIndices.All(i=>i==1));
            Equal(true,handler.EntityManager.ElementVisits.SequenceEqual(Enumerable.Range(64,5).Select(i=>"Game.Objects.SubObject:"+i)));
            Equal("referenceObserved",(string)page["links"][0]["status"]);Equal("nullReference",(string)page["links"][1]["reason"]);Equal(-3,(int)page["links"][2]["rawIndex"]);
            Equal(true,JToken.DeepEquals(page["links"][0]["target"],page["links"][3]["target"]));Equal(99,(int)page["links"][4]["target"]["index"]);
            Equal(17,(int)direct["data"]["geometryDiagnostics"]["evaluations"]);
            byte[] bytes=null;var args=Args();var body=new JObject{{"items",new JArray(new JObject{{"id","page"},{"name","construction.inventory"},{"args",args.DeepClone()}})}};
            var other=Fixture();var scheduler=new ObservationScheduler(Mod.Worlds,new ObservationCatalog(other,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());
            var group=scheduler.Admit(ObservationCatalog.Parse(body.ToString()),value=>bytes=value);scheduler.Pump();group.Detach();
            var batch=JObject.Parse(Encoding.UTF8.GetString(bytes));Equal(true,JToken.DeepEquals(direct["data"],batch["results"][0]["observation"]["data"]));Equal(5,other.EntityManager.ElementReads);
            var targetsQuery=new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities","2:1,99:1"}};
            var targets=Json(Direct(Fixture(),targetsQuery));Equal("building",(string)targets["data"]["entities"][0]["kind"]);Equal("missingDeletedOrRecycled",(string)targets["data"]["entities"][1]["unavailableReason"]);
            var absentHandler=new RequestHandlers();absentHandler.EntityManager.Add(E(3));var absentQuery=Query(0,32);absentQuery["entities"]="3:1";
            var absentArgs=Args(0,32);absentArgs["entities"][0]["index"]=3;
            var missingQuery=Query(0,32);missingQuery["entities"]="1:2";var missingArgs=Args(0,32);missingArgs["entities"][0]["version"]=2;
            string directory=Environment.GetEnvironmentVariable("RECOVERY_WIRE_DIR");
            if(directory!=null){Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"construction-recovery.json"),new JObject{
                {"request",args},{"direct",direct},{"group",batch},{"terminalRequest",Args(129,32)},{"terminal",Json(Direct(Fixture(),Query(129,32)))},
                {"targetsRequest",new JObject{{"world",args["world"].DeepClone()},{"entities",JArray.FromObject(new[]{new EntityRef{world=Mod.Worlds.Current,index=2,version=1},new EntityRef{world=Mod.Worlds.Current,index=99,version=1}})}}},
                {"targets",targets},{"absentRequest",absentArgs},{"absent",Json(Direct(absentHandler,absentQuery))},
                {"unavailableRequest",missingArgs},{"unavailable",Json(Direct(Fixture(),missingQuery))}}.ToString());}
        });
        check("actual native safe slot guards changed counts and exact recycled deleted temporary roots",()=>{
            var handler=Fixture();var reader=new NativeConstructionReader(()=>handler.EntityManager,e=>"asset");var root=new EntityRef{world=Mod.Worlds.Current,index=1,version=1};
            handler.EntityManager.Buffer(E(1),new Game.Objects.SubObject{m_SubObject=E(2)});
            try{reader.PageSlot(root,"subObject",64,129);throw new Exception("expected source changed");}catch(OperationException e){Equal("RELATIONSHIP_SOURCE_CHANGED",e.Error.code);}Equal(0,handler.EntityManager.ElementReads);
            foreach(string state in new[]{"recycled","deleted","temporary"}) {
                var h=Fixture();var q=Query(0,1);
                if(state=="recycled")q["entities"]="1:2";
                else if(state=="deleted")h.EntityManager.Add(E(1),new Game.Common.Deleted());else h.EntityManager.Add(E(1),new Game.Tools.Temp());
                var result=Json(Direct(h,q));Equal("unavailable",(string)result["data"]["relationshipPage"]["sourceCoverage"]);Equal(JTokenType.Null,result["data"]["relationshipPage"]["observedCount"].Type);Equal(0,h.EntityManager.ElementReads);Equal(0,h.EntityManager.ComponentReads);
            }
        });
        check("actual native page binds every fixed scalar or buffer kind and live unsupported roots",()=>{
            foreach(string kind in new[]{"owner","attached","subObject","installedUpgrade","subNet","subArea"}) {
                var h=Fixture();var result=Json(Direct(h,Query(0,32,kind)));
                var page=result["data"]["relationshipPage"];
                Equal(kind,(string)page["kind"]);Equal(kind=="subObject"?32:1,(int)page["returnedCount"]);
                Equal(true,h.EntityManager.ElementVisits.All(v=>v.StartsWith(kind=="subObject"?"Game.Objects.SubObject:":kind=="installedUpgrade"?"Game.Buildings.InstalledUpgrade:":kind=="subNet"?"Game.Net.SubNet:":"Game.Areas.SubArea:",StringComparison.Ordinal)));
                Equal(true,h.EntityManager.ExistsIndices.All(i=>i==1));
            }
            var unsupported=Fixture();unsupported.EntityManager.Buffer(E(3),new Game.Objects.SubObject{m_SubObject=E(2)});
            var q=Query(0,1);q["entities"]="3:1";
            var observed=Json(Direct(unsupported,q));Equal("unsupportedEntityKind",(string)observed["data"]["entities"][0]["unavailableReason"]);
            Equal("referenceObserved",(string)observed["data"]["relationshipPage"]["links"][0]["status"]);Equal("complete",(string)observed["data"]["relationshipPage"]["sourceCoverage"]);
            Equal(0,unsupported.EntityManager.ComponentReads);Equal(true,unsupported.EntityManager.ExistsIndices.All(i=>i==3));
        });
        check("actual source and byte refusals retain finite cause while O7 preserves only known diagnostics",()=>{
            var handler=Fixture();var expanded=new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities","1:1"},{"relationships","directDepth1"}};
            var refused=Direct(handler,expanded);Equal(413,refused.Status);Equal(ConstructionReadDiagnostics.Slots,(string)Json(refused)["errorData"]["message"]);Equal(0,handler.EntityManager.ComponentReads);Equal(0,handler.EntityManager.ElementReads);
            Equal(400,Direct(Fixture(),Query(130,1)).Status);Equal(ConstructionReadDiagnostics.Offset,(string)Json(Direct(Fixture(),Query(130,1)))["errorData"]["message"]);
            var args=Args();args.Remove("relationshipPage");args.Remove("geometryDiagnostics");args["relationships"]="directDepth1";
            byte[] bytes=null;var scheduler=new ObservationScheduler(Mod.Worlds,new ObservationCatalog(Fixture(),()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());
            var group=scheduler.Admit(ObservationCatalog.Parse(new JObject{{"items",new JArray(new JObject{{"id","refused"},{"name","construction.inventory"},{"args",args}})}}.ToString()),value=>bytes=value);scheduler.Pump();group.Detach();
            Equal(ConstructionReadDiagnostics.Slots,(string)JObject.Parse(Encoding.UTF8.GetString(bytes))["results"][0]["error"]["message"]);
        });
    }
}
