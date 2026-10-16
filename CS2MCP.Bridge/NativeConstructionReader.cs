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
    /// <summary>Game-thread-only adapter. One explicitly versioned root per call, no query or traversal.</summary>
    internal sealed class NativeConstructionReader
    {
        private readonly EntityManager manager;
        private readonly Func<Entity,string> assetKey;
        internal NativeConstructionReader(EntityManager manager,Func<Entity,string> assetKey){this.manager=manager;this.assetKey=assetKey;}
        private static Point3 Point(float3 p)=>new Point3(p.x,p.y,p.z);
        private static EntityRef Ref(Entity e,WorldRef world)=>e==Entity.Null?null:new EntityRef{world=world,index=e.Index,version=e.Version};
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
                result.owner=Ref(manager.GetComponentData<Owner>(entity).m_Owner,reference.world);
                result.ownerReason=result.owner==null?"nullOwner":null;
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
                var data=manager.GetComponentData<NetData>(prefab);geometry.requiredLayers=data.m_RequiredLayers.ToString();geometry.connectLayers=data.m_ConnectLayers.ToString();geometry.layersReason=null;
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
