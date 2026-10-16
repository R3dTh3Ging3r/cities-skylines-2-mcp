using System;
using System.Collections.Generic;
using System.Linq;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class ConstructionRelationshipTests
    {
        static WorldIdentity World(){var w=new WorldIdentity("boot");w.BeginLoad();w.CompleteLoad();return w;}
        static Dictionary<string,string> Query(int roots=1)=>new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities",string.Join(",",Enumerable.Range(1,roots).Select(i=>i+":1"))},{"relationships","directDepth1"}};
        sealed class Source:IConstructionRelationshipSource
        {
            internal int HeaderReads,SlotReads,TargetReads,Count=4;
            internal Action<string> Changed;
            internal EntityRef[] Refs;
            public ConstructionRelationSource[] Headers(EntityRef entity){HeaderReads++;Changed?.Invoke("header");var headers=ConstructionRelationships.EmptyHeaders(entity);headers[2].present=true;headers[2].observedCount=Count;headers[2].reason=null;return headers;}
            public ConstructionReference Slot(EntityRef entity,string kind,int slot){SlotReads++;Changed?.Invoke("slot");return new ConstructionReference{target=Refs==null?null:Refs[slot],reason=Refs==null||Refs[slot]==null?"nullReference":null};}
            internal ConstructionEntity Read(EntityRef entity){TargetReads++;Changed?.Invoke("target");return new ConstructionEntity{entity=entity,kind=entity.index==1?null:"building",unavailableReason=entity.index==1?"unsupportedEntityKind":null};}
        }
        public static void Run()
        {
            Program.Check("relationship source headers reserve global128 slots before dereference",()=>{
                foreach(int count in new[]{129,int.MaxValue}){var s=new Source{Count=count};var w=World();Program.Throws<OperationException>(()=>ConstructionInventory.Read(Query(),w,()=>10,s.Read,s));Program.Equal(0,s.SlotReads);Program.Equal(0,s.TargetReads);}
                var source=new Source{Count=128};var result=ConstructionInventory.Read(Query(),World(),()=>10,source.Read,source);Program.Equal(128,result.data.relationships.links.Length);Program.Equal(128,source.SlotReads);Program.Equal(0,result.data.relationships.additionalTargetReads);
                var many=new Source{Count=5};Program.Throws<OperationException>(()=>ConstructionInventory.Read(Query(32),World(),()=>10,many.Read,many));Program.Equal(32,many.HeaderReads);Program.Equal(0,many.SlotReads);Program.Equal(0,many.TargetReads);
            });
            Program.Check("slot coverage preserves null duplicates self and recycled versions without recursion",()=>{
                var w=World();EntityRef Ref(int index,int version=1)=>new EntityRef{world=w.Current,index=index,version=version};
                var s=new Source{Refs=new[]{Ref(1),Ref(2),Ref(2),Ref(2,2),null},Count=5};
                var result=ConstructionInventory.Read(Query(),w,()=>10,s.Read,s).data.relationships;
                Program.Equal(1,s.HeaderReads);Program.Equal(3,s.TargetReads);Program.Equal(2,result.additionalTargetReads);Program.Equal(5,result.links.Length);Program.Equal(3,result.targets.Length);
                Program.Equal(0,result.links[0].targetIndex);Program.Equal(result.links[1].targetIndex,result.links[2].targetIndex);Program.Equal(false,result.links[2].targetIndex==result.links[3].targetIndex);Program.Equal("nullReference",result.links[4].reason);
                Program.Equal(true,result.sources[2].coverageComplete);Program.Equal(5,result.sources[2].visitedCount);Program.Equal(null,result.sources[0].observedCount);Program.Equal(false,result.sources[0].coverageComplete);
            });
            Program.Check("empty buffers are complete while omitted expansion performs zero relationship calls",()=>{
                var s=new Source{Count=0};var q=Query();var expanded=ConstructionInventory.Read(q,World(),()=>10,s.Read,s);Program.Equal(true,expanded.data.relationships.sources[2].coverageComplete);Program.Equal(0,expanded.data.relationships.sources[2].visitedCount);
                q.Remove("relationships");s=new Source();var baseline=ConstructionInventory.Read(q,World(),()=>10,s.Read,s);Program.Equal(null,baseline.data.relationships);Program.Equal(0,s.HeaderReads);Program.Equal(0,s.SlotReads);Program.Equal(1,s.TargetReads);
            });
            Program.Check("each relationship phase fences world and readiness before publication",()=>{
                foreach(string phase in new[]{"header","slot","target"})foreach(bool load in new[]{false,true}){
                    var w=World();var r=new EntityRef{world=w.Current,index=9,version=1};var s=new Source{Count=1,Refs=new[]{r}};int roots=0;
                    Func<EntityRef,ConstructionEntity> read=e=>{roots++;if(roots>1&&phase=="target"){if(load)w.BeginLoad();else w.MarkUnavailable();}return new ConstructionEntity{entity=e};};
                    s.Changed=p=>{if(p==phase&&phase!="target"){if(load)w.BeginLoad();else w.MarkUnavailable();}};
                    Program.Throws<OperationException>(()=>ConstructionInventory.Read(Query(),w,()=>10,read,s));
                    Program.Equal(phase=="header"?0:1,s.SlotReads);
                }
            });
            Program.Check("actual O7 admission copies literal inventory expansion and shares original world",()=>{
                string body="{\"items\":[{\"id\":\"i\",\"name\":\"construction.inventory\",\"args\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"entities\":[{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":1,\"version\":1}],\"relationships\":\"directDepth1\"}}]}";
                var parsed=ObservationCatalog.Parse(body);var copy=parsed.Items[0].Inventory();Program.Equal("directDepth1",copy["relationships"]);copy["relationships"]="changed";Program.Equal("directDepth1",parsed.Items[0].Inventory()["relationships"]);Program.Equal("boot",parsed.Items[0].ReferencedWorld.bootId);
                foreach(string value in new[]{"true","1","\"directDepth2\""})Program.Throws<OperationException>(()=>ObservationCatalog.Parse(body.Replace("\"directDepth1\"",value)));
                var request=new CS2MCP.BridgeRequest();typeof(CS2MCP.HttpBridgeServer).GetMethod("ParseUrl",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{"/v2/construction/inventory?bootId=boot&epoch=1&entities=1:1&relationships=directDepth1",request});Program.Equal("directDepth1",request.Query["relationships"]);
            });
            Program.Check("flag decoding rejects equivalent duplicates unsupported modes and unknown depth",()=>{
                foreach(string mode in new[]{"true","1","directDepth2","","directDepth1 "}){var q=Query();q["relationships"]=mode;Program.Throws<OperationException>(()=>ConstructionInventory.Parse(q));}
                Program.Throws<OperationException>(()=>ConstructionInventory.DecodeQuery("bootId=boot&epoch=1&entities=1:1&relationships=directDepth1&%72elationships=directDepth1"));
                var unknown=Query();unknown["depth"]="1";Program.Throws<OperationException>(()=>ConstructionInventory.Parse(unknown));
            });
        }
    }
}
