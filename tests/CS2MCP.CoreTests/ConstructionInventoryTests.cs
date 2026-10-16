using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class ConstructionInventoryTests
    {
        static Dictionary<string,string> Query(int count = 1) => new Dictionary<string,string> {
            ["bootId"]="boot", ["epoch"]="1", ["entities"]=string.Join(",", Enumerable.Range(1,count).Select(i=>i+":1")) };
        static void Reject(Dictionary<string,string> query) => Program.Throws<OperationException>(() => ConstructionInventory.Parse(query));
        public static void Run()
        {
            Program.Check("bounded explicit query accepts 1 and 32 exact versioned entities", () => {
                foreach(int n in new[]{1,32}) { var q=ConstructionInventory.Parse(Query(n)); Program.Equal(n,q.entities.Length); Program.Equal(1L,q.world.epoch); }
            });
            Program.Check("invalid counts duplicates malformed integers and unsupported options reject", () => {
                foreach(string refs in new[]{"","1:1,1:1","1:1,1:2","1.0:1","1:1.0","NaN:1","1:Infinity","-1:1","0:1","1:0","2147483648:1","1:1,","1:1:1"," 1:1"}) {
                    var q=Query(); q["entities"]=refs; Reject(q);
                }
                Reject(Query(33));
                foreach(string option in new[]{"limit","spatial","cursor","includeFamilies","x"}) {var q=Query();q[option]="1";Reject(q);}
                foreach(string epoch in new[]{"-1","1.0","NaN","9007199254740992","1e0"}) {var q=Query();q["epoch"]=epoch;Reject(q);}
                foreach(string boot in new[]{"",new string('a',129),"  "}) {var q=Query();q["bootId"]=boot;Reject(q);}
                var absent=Query(); absent.Remove("epoch");Reject(absent);
            });
            Program.Check("compact query decoding rejects duplicates malformed escapes and oversize values", () => {
                var q=ConstructionInventory.DecodeQuery("bootId=boot%20%2B%20%2F%3F&epoch=1&entities=1%3A1");
                Program.Equal("boot + /?",q["bootId"]);Program.Equal("1:1",q["entities"]);
                foreach(string raw in new[]{"bootId=boot&bootId=other&epoch=1&entities=1:1","bootId=boot&%62ootId=other&epoch=1&entities=1:1","bootId=%QQ&epoch=1&entities=1:1","bootId=boot&epoch=1&entities=1:1&","bootId=boot&epoch=1&entities=1:1&limit=1",new string('x',4097)})
                    Program.Throws<OperationException>(()=>ConstructionInventory.DecodeQuery(raw));
            });
            Program.Check("invalid and stale requests read no entities; valid work stays bounded", () => {
                var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();int reads=0;
                Func<EntityRef,ConstructionEntity> read=e=>{reads++;return new ConstructionEntity { entity=e, unavailableReason="missingDeletedOrRecycled" };};
                Program.Throws<OperationException>(()=>ConstructionInventory.Read(Query(33),world,()=>9,read));Program.Equal(0,reads);
                var stale=Query();stale["epoch"]="0";Program.Throws<OperationException>(()=>ConstructionInventory.Read(stale,world,()=>9,read));Program.Equal(0,reads);
                world.MarkUnavailable();Program.Throws<OperationException>(()=>ConstructionInventory.Read(Query(),world,()=>9,read));Program.Equal(0,reads);world.CompleteLoad();
                var result=ConstructionInventory.Read(Query(32),world,()=>9,read);Program.Equal(32,reads);Program.Equal(32,result.value.entities.Length);
                Program.Equal(32,result.value.requestedEntities.Length);Program.Equal("explicitEntities",result.value.scope);Program.Equal(null,result.value.nextCursor);
                Program.Equal(9L,result.startFrame);Program.Equal(9L,result.endFrame);Program.Equal(true,result.endUtc>=result.startUtc);
                Program.Equal(null,result.value.worldBounds);Program.Equal("notMeasured",result.value.worldBoundsReason);
                Program.Equal(null,result.value.entities[0].capacityEffects);Program.Equal("notMeasured",result.value.entities[0].capacityEffectsReason);
            });
            Program.Check("world or readiness changes during native read prevent publication", () => {
                foreach(bool loading in new[]{true,false}) {
                    var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();int reads=0;
                    Program.Throws<OperationException>(()=>ConstructionInventory.Read(Query(32),world,()=>10,e=>{
                        reads++;if(loading)world.BeginLoad();else world.MarkUnavailable();return new ConstructionEntity{entity=e};}));
                    Program.Equal(1,reads);
                }
            });
            Program.Check("root OBB rotates local offset and axes while preserving half extents", () => {
                double s=Math.Sqrt(0.5);var b=ConstructionGeometry.RootBounds(new Point3(10,20,30),new Rotation4(0,s,0,s),new Point3(1,0,-2),new Point3(5,6,2));
                Near(10,b.center.x);Near(23,b.center.y);Near(27,b.center.z);Near(2,b.halfExtents.x);Near(3,b.halfExtents.y);Near(2,b.halfExtents.z);
                Near(0,b.axisX.x);Near(-1,b.axisX.z);Near(1,b.axisZ.x);Program.Equal("rootGeometryOnly",b.scope);
                Program.Throws<OperationException>(()=>ConstructionGeometry.RootBounds(new Point3(double.NaN,0,0),new Rotation4(0,0,0,1),new Point3(0,0,0),new Point3(1,1,1)));
                Program.Throws<OperationException>(()=>ConstructionGeometry.RootBounds(new Point3(0,0,0),new Rotation4(0,0,0,0),new Point3(0,0,0),new Point3(1,1,1)));
                Program.Throws<OperationException>(()=>ConstructionGeometry.RootBounds(new Point3(0,0,0),new Rotation4(0,0,0,1),new Point3(2,0,0),new Point3(1,1,1)));
            });
            Program.Check("network geometry preserves full finite cubic and unknown differs from zero", () => {
                var curve=new CubicCurve{a=new Point3(0,0,0),b=new Point3(1,20,3),c=new Point3(4,-5,6),d=new Point3(7,0,9)};
                ConstructionGeometry.Validate(curve);Near(20,curve.b.y);curve.c.x=double.PositiveInfinity;
                Program.Throws<OperationException>(()=>ConstructionGeometry.Validate(curve));
                var item=new ConstructionEntity();Program.Equal(null,item.rootBounds);Program.Equal("notMeasured",item.facilityEnvelopeReason);Program.Equal(null,item.family);
            });
            Program.Check("inventory manifest reports a partial read and retains disabled operations", () => {
                var manifest=EndpointCatalog.CreateManifest(new BuildIdentity("test","test",null,null),new RuntimeSnapshotStore().Read(DateTime.UtcNow));
                var endpoint=EndpointCatalog.All.Single(e=>e.capability=="cs2_construction_inventory");Program.Equal("read",endpoint.access);
                var support=manifest.capabilities.Single(c=>c.name==endpoint.capability);Program.Equal("degraded",support.support);Program.Equal("explicitEntitiesOnlyRootGeometryNativeNotRun",support.reason);
                Program.Equal("unverified",manifest.nativeSupport.Single(c=>c.name==endpoint.capability).support);
                Program.Equal(false,manifest.operationProtocol.dispatchAvailable);
            });
            Program.Check("actual HTTP ingress accepts bounded inventory query and retains operation query refusal", () => {
                bool reached=false;
                using(var stream=new MemoryStream(Encoding.ASCII.GetBytes("GET /v2/construction/inventory?bootId=boot&epoch=1&entities=1%3A1 HTTP/1.1\r\nHost: localhost\r\n\r\n")))
                    HttpIngress.ReadAndDispatch(stream,r=>{reached=true;Program.Equal("/v2/construction/inventory",r.Path);});
                Program.Equal(true,reached);
                using(var stream=new MemoryStream(Encoding.ASCII.GetBytes("GET /v2/operations/b6f2a37a-5676-40ec-8b56-3553b32c28e4?x=1 HTTP/1.1\r\n\r\n")))
                    Program.Throws<OperationException>(()=>HttpIngress.ReadAndDispatch(stream,r=>{}));
            });
            Program.Check("actual HttpBridgeServer dispatch sends inventory to queue and only operation routes to operation handler", () => {
                var parsed=new BridgeRequest();
                typeof(HttpBridgeServer).GetMethod("ParseUrl",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"/v2/construction/inventory?bootId=boot%20%2B&epoch=1&entities=1%3A1",parsed});
                Program.Equal("boot +",parsed.Query["bootId"]);
                foreach(string duplicate in new[]{"bootId=boot&bootId=other", "bootId=boot&%62ootId=other"}) {
                    try {
                        typeof(HttpBridgeServer).GetMethod("ParseUrl",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"/v2/construction/inventory?"+duplicate+"&epoch=1&entities=1:1",new BridgeRequest()});
                        throw new Exception("duplicate query was overwritten");
                    } catch(TargetInvocationException error) { Program.Equal(true,error.InnerException is OperationException); }
                }
                int queued=0,operations=0;var world=new WorldIdentity("boot");
                var server=new HttpBridgeServer(0,r=>{queued++;r.Complete(new BridgeResponse());return true;},r=>{operations++;return new BridgeResponse();},world);
                var dispatch=typeof(HttpBridgeServer).GetMethod("Dispatch",BindingFlags.NonPublic|BindingFlags.Instance);
                dispatch.Invoke(server,new object[]{new BridgeRequest{Path="/v2/construction/inventory"}});Program.Equal(1,queued);Program.Equal(0,operations);
                foreach(string route in new[]{"/v2/commands","/v2/operations/b6f2a37a-5676-40ec-8b56-3553b32c28e4","/v2/operations/b6f2a37a-5676-40ec-8b56-3553b32c28e4/cancel"})
                    dispatch.Invoke(server,new object[]{new BridgeRequest{Path=route}});
                Program.Equal(1,queued);Program.Equal(3,operations);
            });
        }
        static void Near(double a,double b){if(Math.Abs(a-b)>1e-9)throw new Exception("expected "+a+", got "+b);}
    }
}
