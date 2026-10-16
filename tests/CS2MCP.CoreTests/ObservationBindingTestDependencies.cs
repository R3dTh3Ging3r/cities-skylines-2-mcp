// Fixed native-data boundary doubles only. Explicit interface binding is linked unchanged from production.
using System;
using System.Collections.Generic;
using CS2MCP.Core;
namespace CS2MCP {
    public sealed partial class RequestHandlers:IObservationReaders {
        sealed class TestSystem {internal bool SimulationHasTickedSinceLoad=true;}
        readonly TestSystem m_System=new TestSystem();
        internal readonly List<string> BoundCalls=new List<string>();
        object Record(string name){BoundCalls.Add(name);return new Dictionary<string,object>{{"source",name}};}
        object ReadStateData()=>Record("state");object ReadOverviewData()=>Record("city.overview");object ReadServicesData()=>Record("city.services");object ReadLaborData()=>Record("city.labor");object ReadBudgetData()=>Record("city.budget");
        Observation<ConstructionSnapshot> ReadConstructionInventory(IDictionary<string,string> query){BoundCalls.Add("construction.inventory");var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();return ConstructionInventory.Read(query,world,()=>10,e=>new ConstructionEntity{entity=e,unavailableReason="missingDeletedOrRecycled"});}
    }
}
