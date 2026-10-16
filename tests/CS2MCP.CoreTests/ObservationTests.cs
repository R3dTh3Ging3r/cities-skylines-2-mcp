using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CS2MCP.Core;
namespace CS2MCP.CoreTests
{
    internal static class ObservationTests
    {
        static string Json(int count=1,string name="state")=>"{\"items\":["+string.Join(",",Enumerable.Range(0,count).Select(i=>"{\"id\":\""+i+"\",\"name\":\""+name+"\",\"args\":{}}"))+"]}";
        sealed class Readers:IObservationReaders {
            internal int Calls;internal Action During;
            public Observation<TransitPage> Transit(TransitQuery query) => TransitReader.Read(query,Ready(),()=>10,new TestTransitSource());
            static WorldIdentity Ready(){var w=new WorldIdentity("boot");w.BeginLoad();w.CompleteLoad();return w;}
            public bool Warming=>false;
            public object State(){Calls++;During?.Invoke();return new Dictionary<string,object>{{"ok",true}};}
            public object Overview()=>State();public object Services()=>State();public object Labor()=>State();public object Budget()=>State();
            public Observation<ConstructionSnapshot> Inventory(IDictionary<string,string> query){Calls++;return new Observation<ConstructionSnapshot>{data=new ConstructionSnapshot(),world=ConstructionInventory.Parse(query).world};}
        }
        sealed class Encoder:IObservationEncoder {
            internal int ItemSize=16;public byte[] Item(ObservationItemResult item)=>new byte[item.error==null?ItemSize:128];
            public byte[] Batch(ObservationBatchResult batch)=>new byte[100+batch.results.Length*128];
        }
        sealed class Fixture {
            internal WorldIdentity World=new WorldIdentity("boot");internal Readers Readers=new Readers();internal Encoder Encoder=new Encoder();internal double Millis;internal long Frame=10;internal int FrameReads;internal Action FrameAccess;
            long ReadFrame(){FrameReads++;FrameAccess?.Invoke();return Frame;}internal ObservationScheduler Scheduler;
            internal Fixture(){World.BeginLoad();World.CompleteLoad();Scheduler=new ObservationScheduler(World,new ObservationCatalog(Readers,ReadFrame,()=>DateTime.UtcNow),ReadFrame,()=>Millis,Encoder);}
            internal ObservationGroup Admit(int n=1,Action<byte[]> done=null)=>Scheduler.Admit(ObservationCatalog.Parse(Json(n)),done??(_=>{}));
        }
        public static void Run(){
            Program.Check("all observation names bind to actual fixed reader delegates and reject malformed groups atomically",()=>{
                var f=new Fixture();foreach(string name in new[]{"state","city.overview","city.services","city.labor","city.budget"})f.Scheduler.Admit(ObservationCatalog.Parse(Json(1,name)),_=>{});
                f.Scheduler.Pump();Program.Equal(5,f.Readers.Calls);
                foreach(string json in new[]{Json(0),Json(65),Json(1,"cs2_save_game"),Json(1,"/state"),Json().Replace("\"args\":{}","\"args\":{\"x\":1}"),Json().Replace("\"items\"","\"lease\":\"x\",\"items\""),Json(2).Replace("\"id\":\"1\"","\"id\":\"0\"")})Program.Throws<OperationException>(()=>ObservationCatalog.Parse(json));
                Program.Equal(0,f.Scheduler.Pending);Program.Equal(5,f.Readers.Calls);
            });
            Program.Check("actual native adapter interface bindings call each selected typed source and retain inventory bounds",()=>{
                var readers=new CS2MCP.RequestHandlers();var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();
                var scheduler=new ObservationScheduler(world,new ObservationCatalog(readers,()=>10,()=>DateTime.UtcNow),()=>10,()=>0,new Encoder());
                foreach(string name in new[]{"state","city.overview","city.services","city.labor","city.budget"})scheduler.Admit(ObservationCatalog.Parse(Json(1,name)),_=>{});
                string inventory="{\"items\":[{\"id\":\"i\",\"name\":\"construction.inventory\",\"args\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"entities\":[{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":1,\"version\":1}]}}]}";
                var group=scheduler.Admit(ObservationCatalog.Parse(inventory),_=>{});scheduler.Pump();
                Program.Equal("state,city.overview,city.services,city.labor,city.budget,construction.inventory",string.Join(",",readers.BoundCalls));
                var snapshot=(ConstructionSnapshot)group.Result.results[0].observation.data;Program.Equal(1,snapshot.entities.Length);Program.Equal(true,group.Result.results[0].observation.warnings.Contains("rootGeometryOnly"));
                Program.Throws<OperationException>(()=>ObservationCatalog.Parse(inventory.Replace("\"version\":1","\"version\":0")));
            });
            Program.Check("native category and context bounds retain missing population evidence",()=>{
                CityObservationBounds.BudgetCounts(128,128);Program.Throws<OperationException>(()=>CityObservationBounds.BudgetCounts(129,1));
                Program.Throws<OperationException>(()=>new CityStateData{cityName=new string('x',257)}.ValidateObservationBounds());
                var absent=new CityOverviewData();Program.Equal(false,absent.PopulationAvailable);Program.Equal(true,((IObservationDataWarnings)absent).ObservationWarnings.Contains("populationComponentUnavailableLegacyDefaults"));
            });
            Program.Check("whole groups reserve pending capacity and active transports atomically",()=>{
                var f=new Fixture();for(int i=0;i<255;i++)Program.Equal(true,f.Scheduler.TryOrdinary(false,()=>{}));
                Program.Throws<OperationException>(()=>f.Admit(2));Program.Equal(255,f.Scheduler.Pending);Program.Equal(0,f.Scheduler.ActiveGroups);
                var other=new Fixture();var groups=Enumerable.Range(0,8).Select(_=>other.Admit()).ToArray();Program.Throws<OperationException>(()=>other.Admit());
                other.Scheduler.Pump();Program.Equal(8,other.Scheduler.ActiveGroups);groups[0].Detach();groups[0].Detach();Program.Equal(7,other.Scheduler.ActiveGroups);Program.Equal(0,other.Scheduler.Pending);
            });
            Program.Check("ordinary work and two groups consume one fair global eight-read budget",()=>{
                var f=new Fixture();int ordinary=0,writers=0;var a=f.Admit(10);var b=f.Admit(10);
                f.Scheduler.TryOrdinary(true,()=>writers++);for(int i=0;i<10;i++)f.Scheduler.TryOrdinary(false,()=>ordinary++);
                f.Scheduler.Pump();Program.Equal(1,writers);Program.Equal(8,ordinary+f.Readers.Calls);Program.Equal(true,ordinary>0);Program.Equal(true,a.Result.results.Any(r=>r!=null));Program.Equal(true,b.Result.results.Any(r=>r!=null));
                Program.Equal(22,f.Scheduler.Pending);
                a.Detach();b.Detach();Program.Equal(10-ordinary,f.Scheduler.Pending);
            });
            Program.Check("time budget stops after the current nonpreemptible reader",()=>{
                var f=new Fixture();f.Readers.During=()=>f.Millis=2;f.Admit(5);f.Scheduler.Pump();Program.Equal(1,f.Readers.Calls);Program.Equal(4,f.Scheduler.Pending);
            });
            Program.Check("per-item errors continue and metadata spans actual reads",()=>{
                var f=new Fixture();int done=0;f.Readers.During=()=>{f.Frame++;if(f.Readers.Calls==1)throw new InvalidOperationException("bounded error");};var g=f.Admit(3,_=>done++);f.Scheduler.Pump();
                Program.Equal(3,f.Readers.Calls);Program.Equal(1,done);Program.Equal(true,g.Result.results[0].error!=null);Program.Equal(true,g.Result.results[1].observation!=null);Program.Equal(10L,g.Result.startFrame);Program.Equal(13L,g.Result.endFrame);
                Program.Equal("current",g.Result.results[1].observation.freshness);Program.Equal(true,g.Result.results[1].observation.warnings.Contains("multiFrameReadNotAtomic"));
            });
            Program.Check("ordinary first and final OperationException retain actual sampled frame span",()=>{
                foreach(int failAt in new[]{1,3}){var f=new Fixture();f.Readers.During=()=>{f.Frame++;if(f.Readers.Calls==failAt)throw new OperationException(new BridgeErrorData(ErrorCodes.Unsupported,"unavailable native data"));};
                    var group=f.Admit(3);f.Scheduler.Pump();Program.Equal(3,f.Readers.Calls);Program.Equal(10L,group.Result.startFrame);Program.Equal(13L,group.Result.endFrame);Program.Equal(ErrorCodes.Unsupported,group.Result.results[failAt-1].error.code);
                }
            });
            Program.Check("original ready world and inventory worlds reject before any dispatch",()=>{
                var f=new Fixture();var wrong=ObservationCatalog.Parse(Json().Replace("{\"items\"","{\"expectedWorld\":{\"bootId\":\"boot\",\"epoch\":0},\"items\""));Program.Throws<OperationException>(()=>f.Scheduler.Admit(wrong,_=>{}));
                f.World.MarkUnavailable();Program.Throws<OperationException>(()=>f.Admit());Program.Equal(0,f.Readers.Calls);Program.Equal(0,f.Scheduler.Pending);
            });
            Program.Check("world change at final native frame capture is fenced before publication",()=>{
                var f=new Fixture();f.FrameAccess=()=>{if(f.FrameReads==4)f.World.BeginLoad();};var group=f.Admit();f.Scheduler.Pump();
                Program.Equal(true,group.Result.worldChanged);Program.Equal(null,group.Result.endFrame);Program.Equal(ErrorCodes.WorldMismatch,group.Result.results[0].error.code);
            });
            Program.Check("mid-load stops remaining reads and retains original completed observations",()=>{
                foreach(bool changed in new[]{true,false}){var f=new Fixture();f.Readers.During=()=>{if(f.Readers.Calls==2){if(changed)f.World.BeginLoad();else f.World.MarkUnavailable();}};var g=f.Admit(4);f.Scheduler.Pump();
                    Program.Equal(2,f.Readers.Calls);Program.Equal(1L,g.Result.world.epoch);Program.Equal(changed,g.Result.worldChanged);Program.Equal(null,g.Result.endFrame);Program.Equal(true,g.Result.results[0].observation!=null);
                    foreach(var row in g.Result.results.Skip(1))Program.Equal(changed?ErrorCodes.WorldMismatch:ErrorCodes.NotReady,row.error.code);Program.Equal(0,f.Scheduler.Pending);
                }
            });
            Program.Check("detach releases each unstarted slot once and fences completion during a reader",()=>{
                var f=new Fixture();int done=0;var g=f.Admit(5,_=>done++);g.Detach();g.Detach();f.Scheduler.Pump();Program.Equal(0,f.Readers.Calls);Program.Equal(0,f.Scheduler.Pending);Program.Equal(0,f.Scheduler.ActiveGroups);Program.Equal(0,done);
                var second=new Fixture();ObservationGroup active=null;second.Readers.During=()=>active.Detach();active=second.Admit(5,_=>done++);second.Scheduler.Pump();Program.Equal(1,second.Readers.Calls);Program.Equal(0,second.Scheduler.Pending);Program.Equal(0,done);
            });
            Program.Check("item and group limits replace oversize items with ordered explicit errors",()=>{
                var f=new Fixture();f.Encoder.ItemSize=ObservationScheduler.MaxItemBytes+1;var g=f.Admit(2);f.Scheduler.Pump();Program.Equal(2,g.Result.results.Length);Program.Equal("RESULT_TOO_LARGE",g.Result.results[0].error.code);
                var second=new Fixture();second.Encoder.ItemSize=ObservationScheduler.MaxItemBytes;var many=second.Admit(64);for(int i=0;i<8;i++)second.Scheduler.Pump();Program.Equal(64,many.Result.results.Length);Program.Equal(true,many.Result.results.Any(r=>r.error?.code=="RESULT_TOO_LARGE"));Program.Equal(0,second.Scheduler.Pending);
            });
        }
    }
}
