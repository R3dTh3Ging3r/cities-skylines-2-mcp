using System;
using System.Linq;

namespace CS2MCP.Core
{
    /// <summary>Selected-slot access validates root state and source length before indexing.</summary>
    public interface IConstructionPageSource : IConstructionRelationshipSource
    {
        ConstructionReference PageSlot(EntityRef root,string kind,int slot,int expectedCount);
    }
    public sealed class ConstructionPageQuery
    {
        public string kind { get; internal set; }
        public int offset { get; internal set; }
        public int limit { get; internal set; }
    }
    public sealed class ConstructionPageLink
    {
        public EntityRef source { get; set; }
        public string kind { get; set; }
        public int slot { get; set; }
        public EntityRef target { get; set; }
        public int? rawIndex { get; set; }
        public int? rawVersion { get; set; }
        public string status=>target==null?"unavailable":"referenceObserved";
        public string reason { get; set; }
    }
    public sealed class ConstructionPageSnapshot
    {
        public EntityRef root { get; set; }
        public string kind { get; set; }
        public int offset { get; set; }
        public int limit { get; set; }
        public int? observedCount { get; set; }
        public int? endObservedCount { get; set; }
        public int returnedCount=>links.Length;
        public int visitedSlots=>links.Length;
        public int endExclusive { get; set; }
        public int? nextOffset { get; set; }
        public bool windowComplete { get; set; }
        public string sourceCoverage { get; set; }
        public string targetResolution=>"notRequested";
        public string consistency=>"livePageNotSnapshot";
        public string crossPageConsistency=>"unverified";
        public int rootReads=>1;
        public int headerReads=>2;
        public int additionalTargetReads=>0;
        public int maxSlots=>32;
        public ConstructionRelationSource[] sources { get; set; }
        public ConstructionPageLink[] links { get; set; }
    }
    /// <summary>Finite canonical diagnostics, safe to preserve across the bounded read boundary.</summary>
    public static class ConstructionReadDiagnostics
    {
        public const string Slots="relationshipSlotLimit: direct relationships exceed128 slots; use a single-root relationshipPage";
        public const string Bytes="inventoryByteLimit: inventory exceeds256KiB; split roots or read raw relationship pages and chosen targets separately";
        public const string ItemBytes="observationItemByteLimit: observation exceeds256KiB; reduce the requested read";
        public const string GroupBytes="observationGroupByteBudget: group exceeds its4MiB reserved budget; request fewer items";
        public const string Offset="relationshipOffsetOutOfRange: request a fresh selected-source page from offset0";
        public const string Changed="relationshipSourceChanged: selected source changed during the page; request a fresh page manually";
        public static bool Known(string code,string message) =>
            code=="RESULT_TOO_LARGE"&&(message==Slots||message==Bytes||message==ItemBytes||message==GroupBytes)||
            code==ErrorCodes.InvalidArgument&&message==Offset||code=="RELATIONSHIP_SOURCE_CHANGED"&&message==Changed;
    }
    public static class ConstructionRelationshipPage
    {
        internal static readonly string[] Kinds={"owner","attached","subObject","installedUpgrade","subNet","subArea"};
        static OperationException Invalid()=>CommandJson.Invalid("invalid native relationship page evidence");
        public static OperationException Changed()=>new OperationException(new BridgeErrorData("RELATIONSHIP_SOURCE_CHANGED",ConstructionReadDiagnostics.Changed,httpStatus:409));
        static ConstructionRelationSource Selected(ConstructionPageQuery page,ConstructionRelationSource[] headers)=>headers[Array.IndexOf(Kinds,page.kind)];
        internal static ConstructionRelationSource[] Headers(ConstructionQuery request,WorldIdentity worlds,IConstructionRelationshipSource native)
        {
            if(!(native is IConstructionPageSource))throw new OperationException(new BridgeErrorData(ErrorCodes.Unsupported,"bounded page reader unavailable",httpStatus:501));
            worlds.Validate(request.world);
            var headers=native.Headers(request.entities[0]);
            worlds.Validate(request.world);
            ConstructionRelationships.ValidateHeaders(request.entities[0],headers);
            return headers;
        }
        internal static ConstructionRelationSource[] Prepare(ConstructionQuery request,WorldIdentity worlds,IConstructionRelationshipSource native)
        {
            var headers=Headers(request,worlds,native);
            var selected=Selected(request.relationshipPage,headers);
            if(request.relationshipPage.offset>(selected.observedCount??0))
                throw new OperationException(new BridgeErrorData(ErrorCodes.InvalidArgument,ConstructionReadDiagnostics.Offset,httpStatus:400));
            return headers;
        }
        internal static ConstructionPageSnapshot Read(ConstructionQuery request,WorldIdentity worlds,
            IConstructionRelationshipSource native,ConstructionRelationSource[] headers)
        {
            var page=request.relationshipPage;var selected=Selected(page,headers);
            int end=selected.observedCount.HasValue?(int)Math.Min(selected.observedCount.Value,(long)page.offset+page.limit):0;
            var links=new ConstructionPageLink[end-page.offset];
            var source=(IConstructionPageSource)native;
            for(int i=0;i<links.Length;i++)
            {
                worlds.Validate(request.world);
                var reference=source.PageSlot(request.entities[0],page.kind,page.offset+i,selected.observedCount.Value);
                worlds.Validate(request.world);
                if(reference==null)throw Invalid();
                if(reference.target==null)
                {
                    if(reference.reason!="nullReference"&&reference.reason!="invalidNativeReference"||
                        reference.reason=="nullReference"&&(reference.rawIndex!=null||reference.rawVersion!=null)||
                        reference.reason=="invalidNativeReference"&&(reference.rawIndex==null||reference.rawVersion==null||reference.rawIndex>0&&reference.rawVersion>0))throw Invalid();
                }
                else if(!WorldIdentity.Same(reference.target.world,request.world)||reference.target.index<=0||reference.target.version<=0||
                    reference.reason!=null||reference.rawIndex!=null||reference.rawVersion!=null)throw Invalid();
                links[i]=new ConstructionPageLink{source=request.entities[0],kind=page.kind,slot=page.offset+i,target=reference.target,
                    rawIndex=reference.rawIndex,rawVersion=reference.rawVersion,reason=reference.reason};
            }
            var after=Selected(page,Headers(request,worlds,native));
            if(after.present!=selected.present||after.observedCount!=selected.observedCount||after.reason!=selected.reason)throw Changed();
            selected.visitedCount=links.Length;
            selected.coverageComplete=selected.present==true&&page.offset==0&&end==selected.observedCount;
            return new ConstructionPageSnapshot{root=request.entities[0],kind=page.kind,offset=page.offset,limit=page.limit,
                observedCount=selected.observedCount,endObservedCount=after.observedCount,endExclusive=end,links=links,sources=headers,
                nextOffset=selected.observedCount.HasValue&&end<selected.observedCount?end:(int?)null,
                windowComplete=selected.present==true,sourceCoverage=selected.present==true?(selected.coverageComplete?"complete":"partial"):selected.present==false?"absent":"unavailable"};
        }
    }
}
