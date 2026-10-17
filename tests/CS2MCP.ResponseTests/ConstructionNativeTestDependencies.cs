// Minimal ECS-shaped boundary data for the EXACT production NativeConstructionReader
// and RequestHandlers.Construction sources. This is field-binding/work evidence,
// not installed Game/Unity signature or runtime validation.
using System;
using System.Collections.Generic;
namespace Unity.Entities
{
    public struct Entity:IEquatable<Entity>
    {
        public int Index,Version;public static Entity Null=>default;
        public bool Equals(Entity other)=>Index==other.Index&&Version==other.Version;
        public override bool Equals(object other)=>other is Entity e&&Equals(e);
        public override int GetHashCode()=>Index*397^Version;
        public static bool operator ==(Entity a,Entity b)=>a.Equals(b);
        public static bool operator !=(Entity a,Entity b)=>!a.Equals(b);
    }
    public sealed class EntityManager
    {
        readonly Dictionary<Entity,Dictionary<Type,object>> values=new Dictionary<Entity,Dictionary<Type,object>>();
        public int ElementReads,ComponentReads,HeaderReads;
        public readonly Dictionary<Type,int> ComponentReadsByType=new Dictionary<Type,int>();
        public void Add(Entity entity,params object[] components){if(!values.ContainsKey(entity))values.Add(entity,new Dictionary<Type,object>());foreach(var item in components)values[entity][item.GetType()]=item;}
        public void Buffer<T>(Entity entity,params T[] elements){Add(entity);values[entity][typeof(List<T>)]=new List<T>(elements);}
        public bool Exists(Entity entity)=>values.ContainsKey(entity);
        public bool HasComponent<T>(Entity entity)=>values.TryGetValue(entity,out var row)&&row.ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity entity){ComponentReads++;ComponentReadsByType[typeof(T)]=ComponentReadsByType.TryGetValue(typeof(T),out int count)?count+1:1;return (T)values[entity][typeof(T)];}
        public bool HasBuffer<T>(Entity entity)=>values.TryGetValue(entity,out var row)&&row.ContainsKey(typeof(List<T>));
        public DynamicBuffer<T> GetBuffer<T>(Entity entity,bool isReadOnly){if(!isReadOnly)throw new Exception("writable buffer requested");HeaderReads++;return new DynamicBuffer<T>((List<T>)values[entity][typeof(List<T>)],()=>ElementReads++);}
    }
    public struct DynamicBuffer<T>
    {
        readonly List<T> items;readonly Action read;public DynamicBuffer(List<T> items,Action read){this.items=items;this.read=read;}
        public int Length=>items.Count;public T this[int index]{get{read();return items[index];}}
    }
}
namespace Unity.Mathematics
{
    public struct float3 {public float x,y,z;public float3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct float4 {public float x,y,z,w;}
    public struct Quaternion {public float4 value;}
}
namespace Game.Common {public struct Owner{public Unity.Entities.Entity m_Owner;}public struct Deleted {}}
namespace Game.Tools {public struct Temp {}}
namespace Game.Buildings {public struct Building {}public struct InstalledUpgrade{public Unity.Entities.Entity m_Upgrade;}}
namespace Game.Objects {
    public struct SubObject{public Unity.Entities.Entity m_SubObject;}public struct Attached{public Unity.Entities.Entity m_Parent;}
    public struct Transform{public Unity.Mathematics.float3 m_Position;public Unity.Mathematics.Quaternion m_Rotation;}
}
namespace Game.Areas {public struct SubArea{public Unity.Entities.Entity m_Area;}}
namespace Game.Net {
    public struct Edge{public Unity.Entities.Entity m_Start,m_End;}
    public struct SubNet{public Unity.Entities.Entity m_SubNet;}
    public struct Bezier{public Unity.Mathematics.float3 a,b,c,d;}
    public struct Curve{public Bezier m_Bezier;public float m_Length;}
}
namespace Game.Prefabs {
    public struct PrefabRef{public Unity.Entities.Entity m_Prefab;}
    public struct NetGeometryData{public float m_DefaultWidth;}
    public enum Layer {Road}
    public struct NetData{public Layer m_RequiredLayers,m_ConnectLayers;}
    public struct Bounds{public Unity.Mathematics.float3 min,max;}
    public struct ObjectGeometryData{public Bounds m_Bounds;}
}
namespace CS2MCP {
    public sealed partial class RequestHandlers {
        internal Unity.Entities.EntityManager EntityManager=new Unity.Entities.EntityManager();
        string PrefabNameOf(Unity.Entities.Entity entity)=>"asset-"+entity.Index;
    }
}
