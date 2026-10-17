using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CS2MCP;
using CS2MCP.Core;
using Unity.Entities;
using Unity.Mathematics;
using Newtonsoft.Json.Linq;

internal static class ConstructionWireTests
{
    static void Equal(object a,object b){if(!Equals(a,b))throw new Exception($"expected {a}, got {b}");}
    static Entity E(int index,int version=1)=>new Entity{Index=index,Version=version};
    static float3 P(float x,float y,float z)=>new float3(x,y,z);
    static Dictionary<string,string> Query(bool expanded=true)=>expanded?
        new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities","1:1,2:1"},{"relationships","directDepth1"}}:
        new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities","1:1,2:1"}};
    static RequestHandlers Fixture()
    {
        var handler=new RequestHandlers();var m=handler.EntityManager;
        m.Add(E(1),new Game.Common.Owner{m_Owner=E(2)},new Game.Objects.Attached{m_Parent=E(3)});
        m.Add(E(100),new Game.Prefabs.ObjectGeometryData{m_Bounds=new Game.Prefabs.Bounds{min=P(1,0,-2),max=P(5,6,2)}});
        foreach(int index in new[]{2,5})m.Add(E(index),new Game.Buildings.Building(),new Game.Prefabs.PrefabRef{m_Prefab=E(100)},
            new Game.Objects.Transform{m_Position=P(10,20,30),m_Rotation=new Quaternion{value=new float4{y=(float)Math.Sqrt(.5),w=(float)Math.Sqrt(.5)}}});
        m.Add(E(5),new Game.Common.Owner{m_Owner=E(2)});
        m.Add(E(101),new Game.Prefabs.NetGeometryData{m_DefaultWidth=12},new Game.Prefabs.NetData());
        m.Add(E(3),new Game.Net.Edge{m_Start=E(21),m_End=E(22)},new Game.Prefabs.PrefabRef{m_Prefab=E(101)},
            new Game.Net.Curve{m_Length=30,m_Bezier=new Game.Net.Bezier{a=P(1,2,3),b=P(4,20,6),c=P(7,-5,9),d=P(10,11,12)}});
        m.Add(E(6));
        m.Buffer(E(2),new Game.Objects.SubObject{m_SubObject=E(5)},new Game.Objects.SubObject{m_SubObject=E(5)},new Game.Objects.SubObject(),new Game.Objects.SubObject{m_SubObject=E(-3,2)});
        m.Buffer(E(2),new Game.Buildings.InstalledUpgrade{m_Upgrade=E(4)});
        m.Buffer(E(2),new Game.Net.SubNet{m_SubNet=E(3)});
        m.Buffer(E(2),new Game.Areas.SubArea{m_Area=E(6)});
        return handler;
    }
    static BridgeResponse Direct(RequestHandlers handler,Dictionary<string,string> query)=>(BridgeResponse)typeof(RequestHandlers)
        .GetMethod("ConstructionInventoryRequest",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(handler,new object[]{new BridgeRequest{Query=query}});
    static JObject Json(BridgeResponse response)=>JObject.Parse(Encoding.UTF8.GetString(response.Body));
    public static void Run(Action<string,Action> check)
    {
        check("actual native adapter supports stop owner attached edge and station children without recursive headers",()=>{
            var handler=Fixture();var direct=Direct(handler,Query());Equal(200,direct.Status);var data=Json(direct)["data"];
            Equal("unsupportedEntityKind",(string)data["entities"][0]["unavailableReason"]);
            var relationships=data["relationships"];Equal(9,(int)relationships["visitedSlots"]);Equal(4,(int)relationships["additionalTargetReads"]);Equal(2,(int)relationships["headerReads"]);Equal(5,((JArray)relationships["targets"]).Count);
            Equal(7,handler.EntityManager.ElementReads);
            Equal("building",(string)relationships["targets"][0]["kind"]);Equal("rootGeometryOnly",(string)relationships["targets"][0]["rootBounds"]["scope"]);
            Equal(20d,(double)relationships["targets"][1]["network"]["curve"]["b"]["y"]);
            Equal("missingDeletedOrRecycled",(string)relationships["targets"][3]["unavailableReason"]);Equal("unsupportedEntityKind",(string)relationships["targets"][4]["unavailableReason"]);
            var links=(JArray)relationships["links"];Equal("owner",(string)links[0]["kind"]);Equal("attached",(string)links[1]["kind"]);Equal("nullReference",(string)links[4]["reason"]);Equal("invalidNativeReference",(string)links[5]["reason"]);Equal(-3,(int)links[5]["rawIndex"]);
            Equal("fullUpgradeDescriptionNotMeasured",(string)data["entities"][1]["installedUpgradesReason"]);
        });
        check("native header counts refuse129 before reading any buffered reference",()=>{
            var handler=Fixture();handler.EntityManager.Buffer(E(2),Enumerable.Range(0,129).Select(_=>new Game.Objects.SubObject{m_SubObject=E(5)}).ToArray());
            var response=Direct(handler,Query());Equal(413,response.Status);Equal("RESULT_TOO_LARGE",(string)Json(response)["errorData"]["code"]);Equal(0,handler.EntityManager.ElementReads);Equal(0,handler.EntityManager.ComponentReads);
        });
        check("omitted mode preserves native access and actual direct O7 data agree for expanded mode",()=>{
            var baselineHandler=Fixture();var baseline=Direct(baselineHandler,Query(false));Equal(0,baselineHandler.EntityManager.ElementReads);Equal(0,baselineHandler.EntityManager.HeaderReads);Equal(JTokenType.Null,Json(baseline)["data"]["relationships"].Type);
            var handler=Fixture();var request=new Dictionary<string,object>{{"world",Mod.Worlds.Current},{"entities",new[]{new EntityRef{world=Mod.Worlds.Current,index=1,version=1},new EntityRef{world=Mod.Worlds.Current,index=2,version=1}}},{"relationships","directDepth1"}};
            string body=Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string,object>{{"expectedWorld",Mod.Worlds.Current},{"items",new[]{new Dictionary<string,object>{{"id","i"},{"name","construction.inventory"},{"args",request}}}}});
            byte[] encoded=null;var scheduler=new ObservationScheduler(Mod.Worlds,new ObservationCatalog(handler,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());
            var group=scheduler.Admit(ObservationCatalog.Parse(body),bytes=>encoded=bytes);scheduler.Pump();
            var batch=JObject.Parse(Encoding.UTF8.GetString(encoded));var direct=Json(Direct(Fixture(),Query()));
            Equal(true,JToken.DeepEquals(direct["data"],batch["results"][0]["observation"]["data"]));group.Detach();
            string directory=Environment.GetEnvironmentVariable("C1_WIRE_DIR");if(directory!=null){Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"construction.json"),new JObject{{"request",JToken.FromObject(request)},{"direct",direct},{"o7",batch},{"baseline",Json(baseline)}}.ToString());}
        });
        check("actual direct finite serializer and O7 item reject byte pressure without partial inventory",()=>{
            var many=new string('\u0001',128);var worlds=new WorldIdentity(many);worlds.BeginLoad();worlds.CompleteLoad();
            var query=new Dictionary<string,string>{{"bootId",many},{"epoch","1"},{"entities",string.Join(",",Enumerable.Range(1,32).Select(i=>i+":1"))},{"relationships","directDepth1"}};
            var source=new BigSource();int geometryReads=0;ConstructionEntity Read(EntityRef e){geometryReads++;return new ConstructionEntity{entity=e,kind="networkEdge",network=new NetworkGeometry{curve=new CubicCurve{a=new Point3(0,0,0),b=new Point3(1,2,3),c=new Point3(4,5,6),d=new Point3(7,8,9)},start=new EntityRef{world=e.world,index=700,version=1},end=new EntityRef{world=e.world,index=701,version=1},length=1},networkReason=null};}
            var observation=ConstructionInventory.Read(query,worlds,()=>10,Read,source);Equal(128,observation.data.relationships.links.Length);Equal(128,observation.data.relationships.additionalTargetReads);Equal(160,geometryReads);
            var capped=RequestHandlers.BoundedConstructionResponse(observation);Equal(413,capped.Status);Equal(true,capped.Body.Length<262144);Equal("RESULT_TOO_LARGE",(string)Json(capped)["errorData"]["code"]);Equal(ConstructionReadDiagnostics.Bytes,(string)Json(capped)["errorData"]["message"]);
            var wire=new ObservationWire();try{wire.Item(new ObservationItemResult{id="i",observation=new Observation<object>{world=observation.world,data=observation.data,startFrame=10,endFrame=10}});throw new Exception("oversized item accepted");}catch(InvalidOperationException){}
            var readers=new BigReaders{Value=observation};byte[] groupBytes=null;
            var args=new {world=worlds.Current,entities=observation.data.requestedEntities,relationships="directDepth1"};
            string body=Newtonsoft.Json.JsonConvert.SerializeObject(new {items=new[]{new {id="large",name="construction.inventory",args=args}}});
            var scheduler=new ObservationScheduler(worlds,new ObservationCatalog(readers,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,wire);
            var group=scheduler.Admit(ObservationCatalog.Parse(body),bytes=>groupBytes=bytes);scheduler.Pump();
            Equal(1,group.Result.results.Length);Equal("large",group.Result.results[0].id);Equal("RESULT_TOO_LARGE",group.Result.results[0].error.code);Equal(ConstructionReadDiagnostics.ItemBytes,group.Result.results[0].error.message);Equal(null,group.Result.results[0].observation);Equal(true,groupBytes.Length<=ObservationScheduler.MaxGroupBytes);group.Detach();
            observation.data.relationships.targets[0].network.curve.b.x=double.NaN;Equal(413,RequestHandlers.BoundedConstructionResponse(observation).Status);
        });
    }
    sealed class BigReaders:IObservationReaders
    {
        internal Observation<ConstructionSnapshot> Value;
        public bool Warming=>false;
        public object State()=>throw new Exception("unexpected source");public object Overview()=>State();public object Services()=>State();public object Labor()=>State();public object Budget()=>State();
        public Observation<ConstructionSnapshot> Inventory(IDictionary<string,string> query)=>Value;
        public Observation<TransitPage> Transit(TransitQuery query)=>throw new Exception("unexpected source");
        public Observation<MetricBatch> Metrics(MetricReadRequest request)=>throw new Exception("unexpected source");
    }
    sealed class BigSource:IConstructionRelationshipSource
    {
        public ConstructionRelationSource[] Headers(EntityRef root){var headers=ConstructionRelationships.EmptyHeaders(root);if(root.index==1){headers[2].present=true;headers[2].observedCount=128;headers[2].reason=null;}return headers;}
        public ConstructionReference Slot(EntityRef root,string kind,int slot)=>new ConstructionReference{target=new EntityRef{world=root.world,index=1000+slot,version=1}};
    }
}
