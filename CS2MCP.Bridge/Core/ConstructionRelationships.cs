using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CS2MCP.Core
{
    /// <summary>Fixed instance sources only. Headers never dereference a relationship slot.</summary>
    public interface IConstructionRelationshipSource
    {
        ConstructionRelationSource[] Headers(EntityRef root);
        ConstructionReference Slot(EntityRef root,string kind,int slot);
    }
    public sealed class ConstructionReference
    {
        public EntityRef target { get; set; }
        public int? rawIndex { get; set; }
        public int? rawVersion { get; set; }
        public string reason { get; set; }
    }
    public sealed class ConstructionRelationSource
    {
        public EntityRef source { get; set; }
        public string kind { get; set; }
        public bool? present { get; set; }
        public string status=>present==true?"present":present==false?"absent":"unavailable";
        public int? observedCount { get; set; }
        public int visitedCount { get; set; }
        public bool coverageComplete { get; set; }
        public string reason { get; set; }
    }
    public sealed class ConstructionRelationLink
    {
        public EntityRef source { get; set; }
        public string kind { get; set; }
        public int slot { get; set; }
        public EntityRef target { get; set; }
        public int? rawIndex { get; set; }
        public int? rawVersion { get; set; }
        public int? targetIndex { get; set; }
        public string status { get; set; }
        public string reason { get; set; }
    }
    public sealed class ConstructionRelatedTarget
    {
        public EntityRef entity { get; set; }
        public string kind { get; set; }
        public string unavailableReason { get; set; }
        public string assetKey { get; set; }
        public string assetKeyReason { get; set; }
        public NetworkGeometry network { get; set; }
        public string networkReason { get; set; }
        public OrientedBounds rootBounds { get; set; }
        public string rootBoundsReason { get; set; }
        internal static ConstructionRelatedTarget From(ConstructionEntity value)=>new ConstructionRelatedTarget {
            entity=value.entity,kind=value.kind,unavailableReason=value.unavailableReason,assetKey=value.assetKey,
            assetKeyReason=value.assetKeyReason,network=value.network,networkReason=value.networkReason,
            rootBounds=value.rootBounds,rootBoundsReason=value.rootBoundsReason };
    }
    public sealed class ConstructionRelationshipLimits
    {
        public int maxRoots=>32;public int maxSlots=>128;public int maxAdditionalTargets=>128;
    }
    public sealed class ConstructionRelationshipSnapshot
    {
        public string scope=>"directDepth1";
        public string coverage=>"directSourceSlotsOnly";
        public ConstructionRelationshipLimits limits { get; }=new ConstructionRelationshipLimits();
        public int rootReads { get; set; }
        public int headerReads { get; set; }
        public int visitedSlots { get; set; }
        public int additionalTargetReads { get; set; }
        public ConstructionRelationSource[] sources { get; set; }
        public ConstructionRelationLink[] links { get; set; }
        public ConstructionRelatedTarget[] targets { get; set; }
        public string nativeVerification=>"CON-01:notRun";
    }
    public static class ConstructionRelationships
    {
        public const int MaxSlots=128;
        static readonly string[] kinds={"owner","attached","subObject","installedUpgrade","subNet","subArea"};
        public static ConstructionRelationSource[] EmptyHeaders(EntityRef root,string unavailable=null)=>kinds.Select(kind=>new ConstructionRelationSource {
            source=root,kind=kind,present=unavailable==null?(bool?)false:null,
            reason=unavailable??(kind=="owner"||kind=="attached"?"noComponent":"noBuffer") }).ToArray();
        static string Key(EntityRef value)=>value.index.ToString(CultureInfo.InvariantCulture)+":"+value.version.ToString(CultureInfo.InvariantCulture);
        public static bool Same(EntityRef a,EntityRef b)=>a!=null&&b!=null&&WorldIdentity.Same(a.world,b.world)&&a.index==b.index&&a.version==b.version;
        static OperationException Invalid()=>CommandJson.Invalid("invalid native relationship source evidence");
        internal static void ValidateHeaders(EntityRef root,ConstructionRelationSource[] headers)
        {
            if(headers==null||headers.Length!=kinds.Length)throw Invalid();
            for(int i=0;i<headers.Length;i++) {
                var header=headers[i];
                if(header==null||header.kind!=kinds[i]||!Same(header.source,root)||header.visitedCount!=0||header.coverageComplete||
                    (header.present==true?(header.observedCount==null||header.observedCount<0||header.reason!=null||i<2&&header.observedCount!=1):
                        header.observedCount!=null||string.IsNullOrEmpty(header.reason))||header.reason?.Length>128)throw Invalid();
            }
        }
        public static ConstructionRelationSource[] Reserve(ConstructionQuery request,WorldIdentity worlds,IConstructionRelationshipSource native)
        {
            if(native==null)throw new OperationException(new BridgeErrorData(ErrorCodes.Unsupported,"relationship reader unavailable",httpStatus:501));
            var sources=new List<ConstructionRelationSource>();long total=0;
            // All headers are checked before the first slot/additional-target read.
            foreach(var root in request.entities) {
                worlds.Validate(request.world);
                var headers=native.Headers(root);
                worlds.Validate(request.world);
                ValidateHeaders(root,headers);
                foreach(var header in headers) {total+=header.observedCount??0;sources.Add(header);}
            }
            if(total>MaxSlots)throw new OperationException(new BridgeErrorData("RESULT_TOO_LARGE",
                ConstructionReadDiagnostics.Slots,httpStatus:413));
            return sources.ToArray();
        }
        public static ConstructionRelationshipSnapshot Read(ConstructionQuery request,ConstructionEntity[] roots,
            WorldIdentity worlds,Func<EntityRef,ConstructionEntity> readEntity,IConstructionRelationshipSource native,
            ConstructionRelationSource[] sources)
        {
            var cache=new Dictionary<string,ConstructionEntity>(StringComparer.Ordinal);
            foreach(var root in roots)cache.Add(Key(root.entity),root);
            var targets=new List<ConstructionRelatedTarget>();var targetIndices=new Dictionary<string,int>(StringComparer.Ordinal);
            var links=new List<ConstructionRelationLink>();int additional=0;
            foreach(var source in sources) {
                if(source.present!=true)continue;
                for(int slot=0;slot<source.observedCount;slot++) {
                    worlds.Validate(request.world);
                    var reference=native.Slot(source.source,source.kind,slot);
                    worlds.Validate(request.world);
                    if(reference==null||reference.reason?.Length>128)throw Invalid();
                    var link=new ConstructionRelationLink {source=source.source,kind=source.kind,slot=slot,target=reference.target,
                        rawIndex=reference.rawIndex,rawVersion=reference.rawVersion,status="unavailable",reason=reference.reason};
                    if(reference.target==null) {
                        if(reference.reason!="nullReference"&&reference.reason!="invalidNativeReference"||
                            reference.reason=="nullReference"&&(reference.rawIndex!=null||reference.rawVersion!=null)||
                            reference.reason=="invalidNativeReference"&&(reference.rawIndex==null||reference.rawVersion==null||reference.rawIndex>0&&reference.rawVersion>0))throw Invalid();
                    } else {
                        var target=reference.target;
                        if(!WorldIdentity.Same(target.world,request.world)||target.index<=0||target.version<=0||reference.reason!=null||reference.rawIndex!=null||reference.rawVersion!=null)throw Invalid();
                        string key=Key(target);
                        if(!targetIndices.TryGetValue(key,out int index)) {
                            if(!cache.TryGetValue(key,out var value)) {
                                if(additional>=MaxSlots)throw Invalid();
                                worlds.Validate(request.world);value=readEntity(target);worlds.Validate(request.world);
                                if(value==null||!Same(value.entity,target))throw Invalid();
                                cache.Add(key,value);additional++;
                            }
                            index=targets.Count;targetIndices.Add(key,index);targets.Add(ConstructionRelatedTarget.From(value));
                        }
                        link.targetIndex=index;
                        link.reason=targets[index].unavailableReason;
                        link.status=link.reason==null?"available":"unavailable";
                    }
                    links.Add(link);source.visitedCount++;
                }
                source.coverageComplete=true;
            }
            return new ConstructionRelationshipSnapshot {rootReads=roots.Length,headerReads=roots.Length,visitedSlots=links.Count,
                additionalTargetReads=additional,sources=sources,links=links.ToArray(),targets=targets.ToArray()};
        }
    }
}
