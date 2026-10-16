using System;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>Game-thread-only adapter. Exact roots and fixed direct instance sources; no query or recursive traversal.</summary>
    internal sealed class NativeConstructionReader:IConstructionRelationshipSource
    {
        private readonly Lazy<EntityManager> managerSource;
        private EntityManager manager=>managerSource.Value;
        private readonly Func<Entity,string> assetKey;
        internal NativeConstructionReader(Func<EntityManager> manager,Func<Entity,string> assetKey){managerSource=new Lazy<EntityManager>(manager);this.assetKey=assetKey;}
        private static Point3 Point(float3 p)=>new Point3(p.x,p.y,p.z);
        private static EntityRef Ref(Entity e,WorldRef world)=>e==Entity.Null||e.Index<=0||e.Version<=0?null:new EntityRef{world=world,index=e.Index,version=e.Version};
        internal ConstructionEntity ReadEntity(EntityRef reference)
        {
            var result=new ConstructionEntity{entity=reference};
            var entity=new Entity{Index=reference.index,Version=reference.version};
            // Exists uses the complete Index+Version. Never resolve a recycled index to its new occupant.
            if(!manager.Exists(entity)){result.unavailableReason="missingDeletedOrRecycled";return result;}
            if(manager.HasComponent<Deleted>(entity)){result.unavailableReason="deleted";return result;}
            if(manager.HasComponent<Temp>(entity)){result.unavailableReason="temporary";return result;}
            bool network=manager.HasComponent<Edge>(entity),building=manager.HasComponent<Game.Buildings.Building>(entity);
            if(!network&&!building){result.unavailableReason="unsupportedEntityKind";return result;}
            result.kind=network?"networkEdge":"building";
            if(manager.HasComponent<Owner>(entity)) {
                Entity owner=manager.GetComponentData<Owner>(entity).m_Owner;
                result.owner=Ref(owner,reference.world);
                result.ownerReason=result.owner==null?(owner==Entity.Null?"nullOwner":"invalidNativeOwner"):null;
            }
            Entity prefab=Entity.Null;
            if(manager.HasComponent<PrefabRef>(entity)) {
                prefab=manager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if(prefab!=Entity.Null&&manager.Exists(prefab)) {
                    string name=assetKey(entity);
                    result.assetKey=string.IsNullOrEmpty(name)||name.Length>256?null:name;
                    result.assetKeyReason=result.assetKey!=null?null:name!=null&&name.Length>256?"prefabNameExceedsBound":"prefabNameUnavailable";
                } else prefab=Entity.Null;
            }
            if(network)ReadNetwork(entity,prefab,reference.world,result);
            if(building)ReadRootBounds(entity,prefab,result);
            return result;
        }
        string State(Entity entity)
        {
            if(!manager.Exists(entity))return "missingDeletedOrRecycled";
            if(manager.HasComponent<Deleted>(entity))return "deleted";
            if(manager.HasComponent<Temp>(entity))return "temporary";
            return null;
        }
        public ConstructionRelationSource[] Headers(EntityRef root)
        {
            Entity entity=new Entity{Index=root.index,Version=root.version};
            var headers=ConstructionRelationships.EmptyHeaders(root,State(entity));
            if(headers[0].present==null)return headers;
            // Presence/counts only. Do not read scalar values or buffer elements
            // until the core reserves every requested source against the global cap.
            void Count(int index,bool present,int count) {
                if(!present)return;
                headers[index].present=true;headers[index].observedCount=count;headers[index].reason=null;
            }
            Count(0,manager.HasComponent<Owner>(entity),1);
            Count(1,manager.HasComponent<Game.Objects.Attached>(entity),1);
            if(manager.HasBuffer<Game.Objects.SubObject>(entity))Count(2,true,manager.GetBuffer<Game.Objects.SubObject>(entity,true).Length);
            if(manager.HasBuffer<Game.Buildings.InstalledUpgrade>(entity))Count(3,true,manager.GetBuffer<Game.Buildings.InstalledUpgrade>(entity,true).Length);
            if(manager.HasBuffer<Game.Net.SubNet>(entity))Count(4,true,manager.GetBuffer<Game.Net.SubNet>(entity,true).Length);
            if(manager.HasBuffer<Game.Areas.SubArea>(entity))Count(5,true,manager.GetBuffer<Game.Areas.SubArea>(entity,true).Length);
            return headers;
        }
        public ConstructionReference Slot(EntityRef root,string kind,int slot)
        {
            Entity entity=new Entity{Index=root.index,Version=root.version},target;
            switch(kind) {
                case "owner":target=manager.GetComponentData<Owner>(entity).m_Owner;break;
                case "attached":target=manager.GetComponentData<Game.Objects.Attached>(entity).m_Parent;break;
                case "subObject":target=manager.GetBuffer<Game.Objects.SubObject>(entity,true)[slot].m_SubObject;break;
                case "installedUpgrade":target=manager.GetBuffer<Game.Buildings.InstalledUpgrade>(entity,true)[slot].m_Upgrade;break;
                case "subNet":target=manager.GetBuffer<Game.Net.SubNet>(entity,true)[slot].m_SubNet;break;
                case "subArea":target=manager.GetBuffer<Game.Areas.SubArea>(entity,true)[slot].m_Area;break;
                default:throw CommandJson.Invalid("unsupported relationship source");
            }
            if(target==Entity.Null)return new ConstructionReference{reason="nullReference"};
            if(target.Index<=0||target.Version<=0)return new ConstructionReference{
                reason="invalidNativeReference",rawIndex=target.Index,rawVersion=target.Version};
            return new ConstructionReference{target=Ref(target,root.world)};
        }
        private void ReadNetwork(Entity entity,Entity prefab,WorldRef world,ConstructionEntity result)
        {
            result.networkReason="curveUnavailable";
            if(!manager.HasComponent<Curve>(entity))return;
            var curve=manager.GetComponentData<Curve>(entity);var edge=manager.GetComponentData<Edge>(entity);
            var cubic=new CubicCurve{a=Point(curve.m_Bezier.a),b=Point(curve.m_Bezier.b),c=Point(curve.m_Bezier.c),d=Point(curve.m_Bezier.d)};
            try { ConstructionGeometry.Validate(cubic); }
            catch(OperationException){result.networkReason="nonfiniteNativeCurve";return;}
            if(!ConstructionGeometry.Finite(curve.m_Length)||curve.m_Length<0){result.networkReason="invalidNativeLength";return;}
            var start=Ref(edge.m_Start,world);var end=Ref(edge.m_End,world);
            if(start==null||end==null){result.networkReason="missingNativeEndpoint";return;}
            var geometry=new NetworkGeometry{curve=cubic,start=start,end=end,length=curve.m_Length};
            if(prefab!=Entity.Null&&manager.HasComponent<NetGeometryData>(prefab)) {
                double width=manager.GetComponentData<NetGeometryData>(prefab).m_DefaultWidth;
                if(ConstructionGeometry.Finite(width)&&width>=0){geometry.width=width;geometry.widthReason=null;}
                else geometry.widthReason="invalidNativeWidth";
            }
            if(prefab!=Entity.Null&&manager.HasComponent<NetData>(prefab)) {
                var data=manager.GetComponentData<NetData>(prefab);
                string required=data.m_RequiredLayers.ToString(),connect=data.m_ConnectLayers.ToString();
                if(required.Length<=128&&connect.Length<=128){geometry.requiredLayers=required;geometry.connectLayers=connect;geometry.layersReason=null;}
                else geometry.layersReason="nativeLayerNameExceedsBound";
            }
            result.network=geometry;result.networkReason=null;
        }
        private void ReadRootBounds(Entity entity,Entity prefab,ConstructionEntity result)
        {
            result.rootBoundsReason="instanceTransformUnavailable";
            if(!manager.HasComponent<Game.Objects.Transform>(entity))return;
            result.rootBoundsReason="prefabGeometryUnavailable";
            if(prefab==Entity.Null||!manager.HasComponent<ObjectGeometryData>(prefab))return;
            var transform=manager.GetComponentData<Game.Objects.Transform>(entity);
            var geometry=manager.GetComponentData<ObjectGeometryData>(prefab);
            var q=transform.m_Rotation.value;
            try {
                result.rootBounds=ConstructionGeometry.RootBounds(Point(transform.m_Position),new Rotation4(q.x,q.y,q.z,q.w),Point(geometry.m_Bounds.min),Point(geometry.m_Bounds.max));
                result.rootBoundsReason=null;
            } catch(OperationException) { result.rootBoundsReason="invalidNativeRootGeometry"; }
        }
    }
}
