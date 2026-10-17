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

internal static class GeometryDiagnosticWireTests
{
    sealed class FixedReaders : IObservationReaders
    {
        internal Observation<ConstructionSnapshot> Observed;
        public bool Warming=>false;
        public object State()=>throw new Exception("unrequested source");
        public object Overview()=>throw new Exception("unrequested source");
        public object Services()=>throw new Exception("unrequested source");
        public object Labor()=>throw new Exception("unrequested source");
        public object Budget()=>throw new Exception("unrequested source");
        public Observation<ConstructionSnapshot> Inventory(IDictionary<string,string> query)=>Observed;
        public Observation<TransitPage> Transit(TransitQuery query)=>throw new Exception("unrequested source");
        public Observation<MetricBatch> Metrics(MetricReadRequest request)=>throw new Exception("unrequested source");
    }
    static Entity E(int index)=>new Entity{Index=index,Version=1};
    static float3 P(float x,float y,float z=0)=>new float3(x,y,z);
    static void Equal(object expected,object actual){if(!Equals(expected,actual))throw new Exception($"expected {expected}, got {actual}");}
    static void Near(double expected,double actual){if(Math.Abs(expected-actual)>1e-10*Math.Max(1,Math.Abs(expected)))throw new Exception($"expected {expected}, got {actual}");}
    static RequestHandlers Fixture()
    {
        var handler=new RequestHandlers();
        handler.EntityManager.Add(E(1),new Game.Net.Edge{m_Start=E(100),m_End=E(101)},new Game.Net.Curve{m_Length=99,m_Bezier=new Game.Net.Bezier{a=P(0,0),b=P(1,2),c=P(2,4),d=P(3,6)}},new Game.Common.Owner{m_Owner=E(6)});
        handler.EntityManager.Buffer(E(1),new Game.Net.SubNet{m_SubNet=E(6)});
        handler.EntityManager.Add(E(2),new Game.Net.Edge{m_Start=E(101),m_End=E(102)},new Game.Net.Curve{m_Length=0,m_Bezier=new Game.Net.Bezier{a=P(4,5,6),b=P(4,5,6),c=P(4,5,6),d=P(4,5,6)}});
        handler.EntityManager.Add(E(3),new Game.Buildings.Building());
        handler.EntityManager.Add(E(4));
        handler.EntityManager.Add(E(6),new Game.Net.Edge{m_Start=E(102),m_End=E(103)},new Game.Net.Curve{m_Length=3,m_Bezier=new Game.Net.Bezier{a=P(0,0),b=P(1,2),c=P(2,-1),d=P(3,0)}});
        handler.EntityManager.Add(E(7),new Game.Net.Edge{m_Start=E(103),m_End=E(104)},new Game.Net.Curve{m_Length=7,m_Bezier=new Game.Net.Bezier{a=P(0,0,0),b=P(1,0,1),c=P(2,1,2),d=P(3,3,3)}});
        return handler;
    }
    static BridgeResponse Direct(RequestHandlers handler,Dictionary<string,string> query)=>(BridgeResponse)typeof(RequestHandlers).GetMethod("ConstructionInventoryRequest",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(handler,new object[]{new BridgeRequest{Query=query}});
    internal static void Run(Action<string,Action> check)
    {
        check("actual native C1 curve read feeds17-sample profiles once and excludes related targets",()=>{
            var query=new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities","1:1,2:1,3:1,4:1,5:1,7:1"},{"relationships","directDepth1"},{"geometryDiagnostics","sampledCubicV1"}};
            var handler=Fixture();var direct=Direct(handler,query);Equal(200,direct.Status);
            var json=JObject.Parse(Encoding.UTF8.GetString(direct.Body));var diagnostics=json["data"]["geometryDiagnostics"];
            Equal(51,(int)diagnostics["evaluations"]);Equal(6,((JArray)diagnostics["profiles"]).Count);
            Equal(4,handler.EntityManager.ComponentReadsByType[typeof(Game.Net.Curve)]); // three roots plus one cached related target
            Equal(99d,(double)json["data"]["entities"][0]["network"]["length"]);
            Equal(2d,(double)diagnostics["profiles"][0]["samples"][8]["grade"]["value"]);
            Equal("stationaryDerivative",(string)diagnostics["profiles"][1]["samples"][0]["curvature"]["reason"]);
            Equal("unavailable",(string)diagnostics["profiles"][2]["status"]);Equal(0,((JArray)diagnostics["profiles"][2]["samples"]).Count);
            var args=new JObject{{"world",JToken.FromObject(Mod.Worlds.Current)},{"entities",new JArray(new[]{1,2,3,4,5,7}.Select(i=>JToken.FromObject(new EntityRef{world=Mod.Worlds.Current,index=i,version=1})))},{"relationships","directDepth1"},{"geometryDiagnostics","sampledCubicV1"}};
            var body=new JObject{{"expectedWorld",args["world"].DeepClone()},{"items",new JArray(new JObject{{"id","g"},{"name","construction.inventory"},{"args",args.DeepClone()}})}};
            byte[] bytes=null;var other=Fixture();var scheduler=new ObservationScheduler(Mod.Worlds,new ObservationCatalog(other,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());
            var group=scheduler.Admit(ObservationCatalog.Parse(body.ToString()),value=>bytes=value);scheduler.Pump();group.Detach();
            var batch=JObject.Parse(Encoding.UTF8.GetString(bytes));Equal(true,JToken.DeepEquals(json["data"],batch["results"][0]["observation"]["data"]));Equal(4,other.EntityManager.ComponentReadsByType[typeof(Game.Net.Curve)]);
            var directory=Environment.GetEnvironmentVariable("GEOMETRY_WIRE_DIR");if(directory!=null){Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"geometry.json"),new JObject{{"request",args},{"direct",json},{"group",batch}}.ToString());}
            query.Remove("geometryDiagnostics");var baseline=Fixture();Equal(200,Direct(baseline,query).Status);Equal(4,baseline.EntityManager.ComponentReadsByType[typeof(Game.Net.Curve)]);
        });
        check("actual finite serializer preserves independent arithmetic failures and unavailable positions",()=>{
            var curves=new[]{
                new CubicCurve{a=new Point3(0,0,0),b=new Point3(0,double.Epsilon,0),c=new Point3(1,double.Epsilon,0),d=new Point3(1,double.Epsilon,0)},
                new CubicCurve{a=new Point3(0,0,0),b=new Point3(1e-310,0,0),c=new Point3(2e-310,1e-310,0),d=new Point3(3e-310,1e-310,0)},
                new CubicCurve{a=new Point3(0,0,0),b=new Point3(2.5e307,0,0),c=new Point3(5e307,1e-12,0),d=new Point3(7.5e307,1e-12,0)}};
            var query=new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities","1:1,2:1,3:1"},{"geometryDiagnostics","sampledCubicV1"}};
            var result=ConstructionInventory.Read(query,Mod.Worlds,()=>10,e=>new ConstructionEntity{entity=e,kind="networkEdge",networkReason=null,network=new NetworkGeometry{start=e,end=e,length=123,curve=curves[e.index-1]}});
            var body=JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(result)));
            Equal(JTokenType.Null,body["data"]["geometryDiagnostics"]["profiles"][0]["samples"][8]["position"].Type);
            Equal("arithmeticOverflow",(string)body["data"]["geometryDiagnostics"]["profiles"][1]["samples"][0]["curvature"]["reason"]);
            Equal(true,(double)body["data"]["geometryDiagnostics"]["profiles"][1]["samples"][0]["radius"]["value"]>0);
            Equal("arithmeticUnderflow",(string)body["data"]["geometryDiagnostics"]["profiles"][2]["samples"][0]["curvature"]["reason"]);
            string directory=Environment.GetEnvironmentVariable("GEOMETRY_WIRE_DIR");
            if(directory!=null) {
                string path=Path.Combine(directory,"geometry.json");var fixture=JObject.Parse(File.ReadAllText(path));
                fixture["arithmeticRequest"]=new JObject{{"world",JToken.FromObject(Mod.Worlds.Current)},{"entities",JArray.FromObject(result.data.requestedEntities)},{"geometryDiagnostics","sampledCubicV1"}};
                fixture["arithmeticDirect"]=body;File.WriteAllText(path,fixture.ToString());
            }
        });
        check("actual producer and serializer preserve translated analytic curves and sampled summaries",()=>{
            var curves=new List<CubicCurve>();
            foreach(double offset in new[]{0d,100000d,1e16}) {
                curves.Add(new CubicCurve{a=new Point3(offset,0,0),b=new Point3(offset+2,2,0),c=new Point3(offset+4,4,0),d=new Point3(offset+6,6,0)});
                curves.Add(new CubicCurve{a=new Point3(offset,0,0),b=new Point3(offset+2,0,2),c=new Point3(offset+4,2,4),d=new Point3(offset+6,6,6)});
            }
            var line=curves[4];var bow=curves[5];
            curves.Add(new CubicCurve{a=line.d,b=line.c,c=line.b,d=line.a});
            curves.Add(new CubicCurve{a=bow.d,b=bow.c,c=bow.b,d=bow.a});
            var query=new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities",string.Join(",",Enumerable.Range(1,curves.Count).Select(i=>i+":1"))},{"geometryDiagnostics","sampledCubicV1"}};
            var observed=ConstructionInventory.Read(query,Mod.Worlds,()=>10,e=>new ConstructionEntity{entity=e,kind="networkEdge",networkReason=null,
                network=new NetworkGeometry{start=e,end=e,length=99,curve=curves[e.index-1]}});
            var direct=JObject.Parse(Encoding.UTF8.GetString(ObservationWire.Standalone(observed)));
            var profiles=(JArray)direct["data"]["geometryDiagnostics"]["profiles"];
            for(int i=0;i<curves.Count;i++) {
                bool reversed=i>=6,straight=i%2==0;var profile=profiles[i];
                for(int j=0;j<17;j++) {
                    double t=reversed?1-j/16d:j/16d;var sample=profile["samples"][j];
                    Near(straight?(reversed?-1:1):(reversed?-1:1)*Math.Sqrt(2)*t,(double)sample["grade"]["value"]);
                    if(straight){Equal(0d,(double)sample["curvature"]["value"]);Equal("zeroCurvatureUnboundedRadius",(string)sample["radius"]["reason"]);}
                    else {Near(1/(6*Math.Pow(1+2*t*t,1.5)),(double)sample["curvature"]["value"]);Near(6*Math.Pow(1+2*t*t,1.5),(double)sample["radius"]["value"]);}
                }
                Equal(17,(int)profile["curvature"]["supportedSamples"]);
                Near(straight?0:1d/6,(double)profile["curvature"]["max"]["value"]);
                Equal(straight?0d:reversed?1d:0d,(double)profile["curvature"]["max"]["t"]);
                Equal(curves[i].a.x,(double)direct["data"]["entities"][i]["network"]["curve"]["a"]["x"]);
                Equal(99d,(double)direct["data"]["entities"][i]["network"]["length"]);
            }
            var args=new JObject{{"world",JToken.FromObject(Mod.Worlds.Current)},{"entities",JArray.FromObject(observed.data.requestedEntities)},{"geometryDiagnostics","sampledCubicV1"}};
            string request=new JObject{{"items",new JArray(new JObject{{"id","translation"},{"name","construction.inventory"},{"args",args.DeepClone()}})}}.ToString();
            byte[] bytes=null;
            var scheduler=new ObservationScheduler(Mod.Worlds,new ObservationCatalog(new FixedReaders{Observed=observed},()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());
            var group=scheduler.Admit(ObservationCatalog.Parse(request),value=>bytes=value);scheduler.Pump();group.Detach();
            var batch=JObject.Parse(Encoding.UTF8.GetString(bytes));Equal(true,JToken.DeepEquals(direct["data"],batch["results"][0]["observation"]["data"]));
            string directory=Environment.GetEnvironmentVariable("GEOMETRY_WIRE_DIR");
            if(directory!=null){string path=Path.Combine(directory,"geometry.json");var fixture=JObject.Parse(File.ReadAllText(path));fixture["translationRequest"]=args;fixture["translationDirect"]=direct;fixture["translationGroup"]=batch;File.WriteAllText(path,fixture.ToString());}
        });
        check("32 selected root curves perform544 pure evaluations and actual serializer refuses whole byte pressure",()=>{
            var world=new WorldIdentity(new string('\u0001',128));world.BeginLoad();world.CompleteLoad();
            var query=new Dictionary<string,string>{{"bootId",world.Current.bootId},{"epoch","1"},{"entities",string.Join(",",Enumerable.Range(1,32).Select(i=>i+":1"))},{"geometryDiagnostics","sampledCubicV1"}};
            int reads=0;
            var observed=ConstructionInventory.Read(query,world,()=>10,e=>{reads++;return new ConstructionEntity{entity=e,kind="networkEdge",assetKey=new string('\u0001',256),assetKeyReason=null,networkReason=null,network=new NetworkGeometry{start=e,end=e,length=123,curve=new CubicCurve{a=new Point3(0,0,0),b=new Point3(1,2,3),c=new Point3(4,5,6),d=new Point3(7,8,9)}}};});
            Equal(32,reads);Equal(544,observed.data.geometryDiagnostics.evaluations);Equal(32,observed.data.geometryDiagnostics.profiles.Length);
            var response=RequestHandlers.BoundedConstructionResponse(observed);Equal(413,response.Status);Equal("RESULT_TOO_LARGE",(string)JObject.Parse(Encoding.UTF8.GetString(response.Body))["errorData"]["code"]);
            bool rejected=false;try{new ObservationWire().Item(new ObservationItemResult{id="g",observation=new Observation<object>{data=observed.data,world=world.Current,startFrame=10,endFrame=10}});}catch(InvalidOperationException){rejected=true;}Equal(true,rejected);
            byte[] bytes=null;
            var args=new {world=world.Current,entities=observed.data.requestedEntities,geometryDiagnostics="sampledCubicV1"};
            string body=Newtonsoft.Json.JsonConvert.SerializeObject(new {items=new[]{new {id="g",name="construction.inventory",args=args}}});
            var scheduler=new ObservationScheduler(world,new ObservationCatalog(new FixedReaders{Observed=observed},()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new ObservationWire());
            var group=scheduler.Admit(ObservationCatalog.Parse(body),value=>bytes=value);scheduler.Pump();group.Detach();
            Equal(1,group.Result.results.Length);Equal("g",group.Result.results[0].id);Equal("RESULT_TOO_LARGE",group.Result.results[0].error.code);Equal(null,group.Result.results[0].observation);Equal(true,bytes.Length<=ObservationScheduler.MaxGroupBytes);
            int frameCalls=0;bool stale=false;
            try{ConstructionInventory.Read(query,world,()=>{if(++frameCalls==2)world.MarkUnavailable();return 10;},e=>observed.data.entities[e.index-1]);}catch(OperationException){stale=true;}Equal(true,stale);
        });
    }
}
