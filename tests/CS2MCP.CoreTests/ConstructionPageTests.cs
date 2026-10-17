using System;
using System.Collections.Generic;
using System.Linq;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class ConstructionPageTests
    {
        internal static void Run()
        {
            Program.Check("actual compact page decoder and producer expose a bounded requested reference window",()=>{
                var query=ConstructionInventory.DecodeQuery("bootId=boot&epoch=1&entities=1:1&relationshipKind=subObject&relationshipOffset=64&relationshipLimit=32");
                var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();
                var source=new Source();
                var observed=ConstructionInventory.Read(query,world,()=>10,source.Read,source);
                var property=observed.data.GetType().GetProperty("relationshipPage");
                Program.Equal(true,property!=null&&property.GetValue(observed.data)!=null);
                Program.Equal(1,source.GeometryReads);Program.Equal(32,source.Slots.Count);
                Program.Equal(true,source.Slots.SequenceEqual(Enumerable.Range(64,32)));
            });
            Program.Check("synthetic single and combined root129 slot refusals precede all geometry and slot work",()=>{
                foreach(int roots in new[]{1,3}) {
                    var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();
                    var source=new Source{Count=129/roots};
                    var query=new Dictionary<string,string>{{"bootId","boot"},{"epoch","1"},{"entities",string.Join(",",Enumerable.Range(1,roots).Select(i=>i+":1"))},{"relationships","directDepth1"}};
                    try {ConstructionInventory.Read(query,world,()=>10,source.Read,source);throw new Exception("expected413");}
                    catch(OperationException e){Program.Equal("RESULT_TOO_LARGE",e.Error.code);Program.Equal(413,e.Error.httpStatus);}
                    Program.Equal(roots,source.HeadersRead);Program.Equal(0,source.GeometryReads);Program.Equal(0,source.Slots.Count);
                }
            });
            Program.Check("page windows cover exact offsets and terminal pages without scanning skipped slots",()=>{
                foreach(int count in new[]{0,1,32,33,128,129,int.MaxValue}) {
                    var s=new Source{Count=count};var w=World();
                    int offset=count==int.MaxValue?int.MaxValue-1:Math.Max(0,count-3);
                    var p=Read(w,s,offset,32).data.relationshipPage;
                    int returned=count-offset;
                    Program.Equal(returned,p.returnedCount);Program.Equal(count,p.endExclusive);Program.Equal(null,p.nextOffset);
                    Program.Equal(offset==0?"complete":"partial",p.sourceCoverage);
                    Program.Equal(true,s.Slots.SequenceEqual(Enumerable.Range(offset,returned)));
                    Program.Equal(1,s.GeometryReads);Program.Equal(2,s.HeadersRead);Program.Equal(0,p.additionalTargetReads);
                    Program.Equal(true,p.sources.Where(x=>x.kind!="subObject").All(x=>!x.coverageComplete&&x.visitedCount==0));
                    var terminal=Read(w,new Source{Count=count},count,1).data.relationshipPage;
                    Program.Equal(0,terminal.returnedCount);Program.Equal(null,terminal.nextOffset);
                    Program.Equal(count==0?"complete":"partial",terminal.sourceCoverage);
                }
                var large=Read(World(),new Source{Count=int.MaxValue},0,32).data.relationshipPage;
                Program.Equal(32,large.nextOffset);Program.Equal(32,large.visitedSlots);
                var source=new Source{Count=129};var first=Read(World(),source,0,32).data.relationshipPage;
                Program.Equal(32,first.nextOffset);Program.Equal(false,first.sources[2].coverageComplete);
                Program.Equal(129,first.observedCount);Program.Equal("livePageNotSnapshot",first.consistency);
            });
            Program.Check("absent unavailable empty and out-of-range pages retain distinct evidence before geometry",()=>{
                foreach(bool? present in new bool?[]{false,null}) {
                    var source=new Source{Present=present};var result=Read(World(),source,0,32);
                    var page=result.data.relationshipPage;
                    Program.Equal(null,page.observedCount);Program.Equal(null,page.endObservedCount);Program.Equal(0,page.returnedCount);
                    Program.Equal(false,page.windowComplete);Program.Equal(present==false?"absent":"unavailable",page.sourceCoverage);
                    Program.Equal(0,source.Slots.Count);Program.Equal(true,result.warnings.Contains("relationshipPagesNotAtomicSnapshot"));
                    var invalid=new Source{Present=present};RejectOffset(invalid,1);Program.Equal(0,invalid.GeometryReads);
                }
                var outOfRange=new Source{Count=2};RejectOffset(outOfRange,3);Program.Equal(0,outOfRange.GeometryReads);Program.Equal(0,outOfRange.Slots.Count);
            });
            Program.Check("all six fixed kinds preserve raw duplicate null and invalid slots without target resolution",()=>{
                foreach(string kind in new[]{"owner","attached","subObject","installedUpgrade","subNet","subArea"}) {
                    var source=new Source{Kind=kind,Count=kind=="owner"||kind=="attached"?1:4};
                    source.Reference=i=>i==0?new ConstructionReference{target=Ref(90)}:i==1?new ConstructionReference{reason="nullReference"}:
                        i==2?new ConstructionReference{reason="invalidNativeReference",rawIndex=-3,rawVersion=2}:new ConstructionReference{target=Ref(90)};
                    var page=Read(World(),source,0,32,kind).data.relationshipPage;
                    Program.Equal(kind,page.kind);Program.Equal("complete",page.sourceCoverage);Program.Equal(1,source.GeometryReads);
                    Program.Equal("referenceObserved",page.links[0].status);Program.Equal(90,page.links[0].target.index);
                    if(source.Count==4){Program.Equal("nullReference",page.links[1].reason);Program.Equal(-3,page.links[2].rawIndex);Program.Equal(90,page.links[3].target.index);}
                }
            });
            Program.Check("changed pages and world transitions refuse publication while same-count replacements stay explicitly live",()=>{
                foreach(string phase in new[]{"header","root","slot"})foreach(bool load in new[]{false,true}) {
                    var w=World();var source=new Source{Count=3};source.During=p=>{if(p==phase){if(load)w.BeginLoad();else w.MarkUnavailable();}};
                    Program.Throws<OperationException>(()=>Read(w,source,0,2));
                    Program.Equal(phase=="header"?0:1,source.GeometryReads);
                    Program.Equal(phase=="slot"?1:0,source.Slots.Count);
                }
                foreach(bool remove in new[]{false,true}) {
                    var source=new Source{Count=3};source.During=p=>{if(p=="slot"){if(remove)source.Present=false;else source.Count=2;}};
                    try{Read(World(),source,0,1);throw new Exception("expected source change");}catch(OperationException e){Program.Equal("RELATIONSHIP_SOURCE_CHANGED",e.Error.code);}
                }
                var sameCount=new Source{Count=3};var world=World();var first=Read(world,sameCount,0,1).data.relationshipPage;
                sameCount.Reference=i=>new ConstructionReference{target=Ref(400+i)};
                var second=Read(world,sameCount,first.nextOffset.Value,2).data.relationshipPage;
                Program.Equal(first.observedCount,second.observedCount);Program.Equal("unverified",second.crossPageConsistency);
                Program.Equal("partial",second.sourceCoverage);Program.Equal(null,second.nextOffset);Program.Equal(401,second.links[0].target.index);
            });
            Program.Check("strict page GET and O7 admission reject mixed incomplete duplicate and unbounded fields",()=>{
                const string good="bootId=boot&epoch=1&entities=1:1&relationshipKind=subObject&relationshipOffset=0&relationshipLimit=32";
                foreach(string raw in new[]{good+"&relationships=directDepth1",good.Replace("1:1&","1:1,2:1&"),good.Replace("&relationshipLimit=32",""),
                    good.Replace("subObject","unknown"),good.Replace("Offset=0","Offset=-1"),good.Replace("Offset=0","Offset=2147483648"),good.Replace("Offset=0","Offset=1e0"),
                    good.Replace("Limit=32","Limit=0"),good.Replace("Limit=32","Limit=33"),good+"&%72elationshipOffset=1",good+"&page=1"})
                    Program.Throws<OperationException>(()=>ConstructionInventory.DecodeQuery(raw));
                var ingress=new CS2MCP.BridgeRequest();
                var parseUrl=typeof(CS2MCP.HttpBridgeServer).GetMethod("ParseUrl",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                parseUrl.Invoke(null,new object[]{"/v2/construction/inventory?"+good,ingress});Program.Equal("32",ingress.Query["relationshipLimit"]);
                try{parseUrl.Invoke(null,new object[]{"/v2/construction/inventory?"+good+"&%72elationshipOffset=1",new CS2MCP.BridgeRequest()});throw new Exception("expected duplicate ingress refusal");}
                catch(System.Reflection.TargetInvocationException e){Program.Equal(true,e.InnerException is OperationException);}
                string body="{\"items\":[{\"id\":\"p\",\"name\":\"construction.inventory\",\"args\":{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"entities\":[{\"world\":{\"bootId\":\"boot\",\"epoch\":1},\"index\":1,\"version\":1}],\"relationshipPage\":{\"kind\":\"subObject\",\"offset\":0,\"limit\":32}}}]}";
                var admission=ObservationCatalog.Parse(body);var copy=admission.Items[0].Inventory();Program.Equal("32",copy["relationshipLimit"]);
                copy["relationshipLimit"]="1";Program.Equal("32",admission.Items[0].Inventory()["relationshipLimit"]);
                foreach(string invalid in new[]{body.Replace(",\"limit\":32",""),body.Replace("\"limit\":32","\"limit\":null"),body.Replace("\"offset\":0","\"offset\":0,\"unknown\":1"),body.Replace("\"relationshipPage\":","\"relationships\":\"directDepth1\",\"relationshipPage\":"),body.Replace("\"kind\":\"subObject\"","\"kind\":17")})
                    Program.Throws<OperationException>(()=>ObservationCatalog.Parse(invalid));
            });
        }
        static WorldIdentity World(){var w=new WorldIdentity("boot");w.BeginLoad();w.CompleteLoad();return w;}
        static EntityRef Ref(int index)=>new EntityRef{world=new WorldRef("boot",1),index=index,version=1};
        static Observation<ConstructionSnapshot> Read(WorldIdentity w,Source source,int offset,int limit,string kind="subObject")=>ConstructionInventory.Read(
            ConstructionInventory.DecodeQuery($"bootId=boot&epoch=1&entities=1:1&relationshipKind={kind}&relationshipOffset={offset}&relationshipLimit={limit}"),w,()=>10,source.Read,source);
        static void RejectOffset(Source source,int offset){try{Read(World(),source,offset,1);throw new Exception("expected invalid offset");}catch(OperationException e){Program.Equal(ErrorCodes.InvalidArgument,e.Error.code);Program.Equal(ConstructionReadDiagnostics.Offset,e.Error.message);}}
        sealed class Source:IConstructionPageSource
        {
            internal int Count=256,HeadersRead,GeometryReads;
            internal string Kind="subObject";
            internal bool? Present=true;
            internal Action<string> During;
            internal Func<int,ConstructionReference> Reference;
            internal readonly List<int> Slots=new List<int>();
            public ConstructionRelationSource[] Headers(EntityRef root){HeadersRead++;During?.Invoke("header");var headers=ConstructionRelationships.EmptyHeaders(root);var selected=headers.Single(x=>x.kind==Kind);selected.present=Present;selected.observedCount=Present==true?Count:(int?)null;selected.reason=Present==true?null:Present==false?"noBuffer":"missingDeletedOrRecycled";return headers;}
            public ConstructionReference Slot(EntityRef root,string kind,int slot){Slots.Add(slot);During?.Invoke("slot");return Reference?.Invoke(slot)??new ConstructionReference{target=new EntityRef{world=root.world,index=slot%100+10,version=1}};}
            public ConstructionReference PageSlot(EntityRef root,string kind,int slot,int expectedCount)=>Slot(root,kind,slot);
            internal ConstructionEntity Read(EntityRef root){GeometryReads++;During?.Invoke("root");return new ConstructionEntity{entity=root};}
        }
    }
}
