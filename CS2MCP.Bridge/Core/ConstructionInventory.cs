using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CS2MCP.Core
{
    /// <summary>Bounded explicit-ID parsing and the production world-fenced reader orchestration.</summary>
    public static class ConstructionInventory
    {
        public const int MaxEntities = 32;
        public const long MaxSafeEpoch = 9007199254740991;
        public static Dictionary<string,string> DecodeQuery(string text)
        {
            if(string.IsNullOrEmpty(text)||text.Length>4096)throw CommandJson.Invalid("missing or oversized explicit inventory query");
            var query=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(string part in text.Split('&')) {
                int split=part.IndexOf('=');
                if(split<=0)throw CommandJson.Invalid("malformed explicit inventory query");
                for(int i=0;i<part.Length;i++) {
                    if(part[i]!='%')continue;
                    if(i+2>=part.Length||!Uri.IsHexDigit(part[i+1])||!Uri.IsHexDigit(part[i+2]))throw CommandJson.Invalid("malformed query escape");
                    i+=2;
                }
                string key=Uri.UnescapeDataString(part.Substring(0,split));
                if(query.ContainsKey(key))throw CommandJson.Invalid("duplicate explicit inventory query field");
                query.Add(key,Uri.UnescapeDataString(part.Substring(split+1)));
            }
            Parse(query);return query;
        }
        public static ConstructionQuery Parse(IDictionary<string,string> query)
        {
            if (query == null || (query.Count < 3 || query.Count > 7) || query.Keys.Any(k=>k!="bootId" && k!="epoch" && k!="entities" && k!="relationships" && k!="geometryDiagnostics" && k!="relationshipKind" && k!="relationshipOffset" && k!="relationshipLimit") ||
                (query.TryGetValue("relationships",out string mode) && mode!="directDepth1") ||
                (query.TryGetValue("geometryDiagnostics",out string diagnostics) && diagnostics!="sampledCubicV1") ||
                !query.TryGetValue("bootId",out string boot) || string.IsNullOrWhiteSpace(boot) || boot.Length>128 ||
                !query.TryGetValue("epoch",out string epochText) || !long.TryParse(epochText,NumberStyles.None,CultureInfo.InvariantCulture,out long epoch) || epoch>MaxSafeEpoch ||
                !query.TryGetValue("entities",out string raw) || string.IsNullOrEmpty(raw) || raw.Length>704)
                throw CommandJson.Invalid("require bounded bootId, safe epoch and 1–32 explicit index:version entities; other options unsupported");
            ConstructionPageQuery page=null;
            if(query.Keys.Any(k=>k=="relationshipKind"||k=="relationshipOffset"||k=="relationshipLimit")) {
                if(query.ContainsKey("relationships")||!query.TryGetValue("relationshipKind",out string kind)||!ConstructionRelationshipPage.Kinds.Contains(kind)||
                    !query.TryGetValue("relationshipOffset",out string offsetText)||!int.TryParse(offsetText,NumberStyles.None,CultureInfo.InvariantCulture,out int offset)||
                    !query.TryGetValue("relationshipLimit",out string limitText)||!int.TryParse(limitText,NumberStyles.None,CultureInfo.InvariantCulture,out int limit)||limit<1||limit>32)
                    throw CommandJson.Invalid("require single-root relationshipKind/relationshipOffset/relationshipLimit without directDepth1");
                page=new ConstructionPageQuery{kind=kind,offset=offset,limit=limit};
            }
            var parts=raw.Split(',');
            if(parts.Length<1 || parts.Length>MaxEntities || page!=null&&parts.Length!=1)throw CommandJson.Invalid("require 1–32 explicit entities");
            var world=new WorldRef(boot,epoch);var entities=new EntityRef[parts.Length];var seen=new HashSet<int>();
            for(int i=0;i<parts.Length;i++) {
                var pair=parts[i].Split(':');
                if(pair.Length!=2 || !int.TryParse(pair[0],NumberStyles.None,CultureInfo.InvariantCulture,out int index) || index<=0 ||
                    !int.TryParse(pair[1],NumberStyles.None,CultureInfo.InvariantCulture,out int version) || version<=0 || !seen.Add(index))
                    throw CommandJson.Invalid("invalid or duplicate explicit entity index/version");
                entities[i]=new EntityRef{world=world,index=index,version=version};
            }
            return new ConstructionQuery{relationshipPage=page,world=world,entities=entities,relationships=query.TryGetValue("relationships",out string selected)?selected:null,geometryDiagnostics=query.TryGetValue("geometryDiagnostics",out string diagnosticMode)?diagnosticMode:null};
        }
        public static Observation<ConstructionSnapshot> Read(IDictionary<string,string> query,WorldIdentity worlds,
            Func<long> frame,Func<EntityRef,ConstructionEntity> readEntity,IConstructionRelationshipSource relationships=null)
        {
            ConstructionQuery request=Parse(query);
            worlds.Validate(request.world);
            var observation=new Observation<ConstructionSnapshot>{world=request.world,startFrame=frame()};
            var headers=request.relationships==null?null:ConstructionRelationships.Reserve(request,worlds,relationships);
            var pageHeaders=request.relationshipPage==null?null:ConstructionRelationshipPage.Prepare(request,worlds,relationships);
            var entities=new ConstructionEntity[request.entities.Length];
            for(int i=0;i<entities.Length;i++) {
                worlds.Validate(request.world);
                entities[i]=readEntity(request.entities[i]);
                worlds.Validate(request.world);
            }
            ConstructionRelationshipSnapshot expanded=null;
            if(request.relationships!=null) {
                expanded=ConstructionRelationships.Read(request,entities,worlds,readEntity,relationships,headers);
                foreach(var entity in entities) {
                    entity.installedUpgradesReason="fullUpgradeDescriptionNotMeasured";
                    var ownerSource=expanded.sources.First(source=>ConstructionRelationships.Same(source.source,entity.entity)&&source.kind=="owner");
                    if(ownerSource.present==true) {
                        var ownerLink=expanded.links.First(link=>ConstructionRelationships.Same(link.source,entity.entity)&&link.kind=="owner");
                        entity.owner=ownerLink.target;
                        entity.ownerReason=entity.owner!=null?null:ownerLink.reason=="nullReference"?"nullOwner":"invalidNativeOwner";
                    } else if(ownerSource.present==null)entity.ownerReason=ownerSource.reason;
                }
            }
            var page=request.relationshipPage==null?null:ConstructionRelationshipPage.Read(request,worlds,relationships,pageHeaders);
            var geometry=request.geometryDiagnostics==null?null:NetworkGeometryDiagnostics.Read(entities,worlds,request.world);
            observation.endFrame=frame();observation.sampledAtUtc=DateTime.UtcNow;
            worlds.Validate(request.world);
            observation.data=new ConstructionSnapshot{requestedEntities=request.entities,entities=entities,relationships=expanded,relationshipPage=page,geometryDiagnostics=geometry};
            observation.freshness="current";
            var warnings=new List<string>{"partialExplicitEntityInventory","rootGeometryOnly","nativeConstructionFixtureNotRun"};
            if(page!=null)warnings.Add("relationshipPagesNotAtomicSnapshot");
            if(geometry!=null)warnings.Add("computedGeometrySamplesNotEngineeringValidation");
            if(expanded!=null)warnings.Add("directRelationshipsNotCompleteFacilityGeometry");
            if(observation.startFrame!=observation.endFrame)warnings.Add("multiFrameReadNotAtomic");
            observation.warnings=warnings.ToArray();
            return observation;
        }
    }
    /// <summary>Only geometry consumed by the explicit reader; no spatial scan/selection claims.</summary>
    public static class ConstructionGeometry
    {
        public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static void Validate(Point3 p) {if(p==null || !Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw CommandJson.Invalid("nonfinite geometry");}
        public static void Validate(CubicCurve c){if(c==null)throw CommandJson.Invalid("missing curve");Validate(c.a);Validate(c.b);Validate(c.c);Validate(c.d);}
        public static OrientedBounds RootBounds(Point3 position,Rotation4 q,Point3 min,Point3 max)
        {
            Validate(position);Validate(min);Validate(max);
            if(q==null || !Finite(q.x)||!Finite(q.y)||!Finite(q.z)||!Finite(q.w) || min.x>max.x || min.y>max.y || min.z>max.z)
                throw CommandJson.Invalid("invalid root geometry");
            double norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
            if(!Finite(norm)||Math.Abs(norm-1)>0.001)throw CommandJson.Invalid("invalid instance rotation");
            // Normalize the source quaternion only within its float rounding tolerance.
            double n=Math.Sqrt(norm),x=q.x/n,y=q.y/n,z=q.z/n,w=q.w/n;
            Point3 Rotate(Point3 p)=>new Point3((1-2*y*y-2*z*z)*p.x+(2*x*y-2*z*w)*p.y+(2*x*z+2*y*w)*p.z,
                (2*x*y+2*z*w)*p.x+(1-2*x*x-2*z*z)*p.y+(2*y*z-2*x*w)*p.z,
                (2*x*z-2*y*w)*p.x+(2*y*z+2*x*w)*p.y+(1-2*x*x-2*y*y)*p.z);
            var offset=Rotate(new Point3(min.x/2+max.x/2,min.y/2+max.y/2,min.z/2+max.z/2));
            var bounds=new OrientedBounds{center=new Point3(position.x+offset.x,position.y+offset.y,position.z+offset.z),
                halfExtents=new Point3(max.x/2-min.x/2,max.y/2-min.y/2,max.z/2-min.z/2),
                axisX=Rotate(new Point3(1,0,0)),axisY=Rotate(new Point3(0,1,0)),axisZ=Rotate(new Point3(0,0,1))};
            Validate(bounds.center);Validate(bounds.halfExtents);return bounds;
        }
    }
}
