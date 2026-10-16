// Fixed native-data boundary doubles only. Explicit interface binding is linked unchanged from production.
using System;
using System.Collections.Generic;
using CS2MCP.Core;
namespace CS2MCP {
    public sealed partial class RequestHandlers:IObservationReaders {
        sealed class TestSystem {internal bool SimulationHasTickedSinceLoad=true;internal long CurrentFrame=10;}
        readonly TestSystem m_System=new TestSystem();
        internal readonly List<string> BoundCalls=new List<string>();
        Observation<TransitPage> ReadTransitData(TransitQuery query){BoundCalls.Add("transit.selectedLine");var w=new WorldIdentity("boot");w.BeginLoad();w.CompleteLoad();return TransitReader.Read(query,w,()=>10,new TestTransitSource());}
        object Record(string name){BoundCalls.Add(name);return new Dictionary<string,object>{{"source",name}};}
        CityStateData ReadStateData(){Record("state");return new CityStateData();}
        CityOverviewData ReadOverviewData(){Record("city.overview");return new CityOverviewData { PopulationAvailable=true,money=9,calendarMetadata=MetricContracts.DescribeCalendar(.2f,.3f,2026,12) };}
        object ReadServicesData()=>Record("city.services");
        CityLaborData ReadLaborData(){Record("city.labor");return new CityLaborData();}
        CityBudgetData ReadBudgetData(){Record("city.budget");return new CityBudgetData();}
#if !CONSTRUCTION_NATIVE_BINDING
        Observation<ConstructionSnapshot> ReadConstructionInventory(IDictionary<string,string> query){BoundCalls.Add("construction.inventory");var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();return ConstructionInventory.Read(query,world,()=>10,e=>new ConstructionEntity{entity=e,unavailableReason="missingDeletedOrRecycled"});}
#endif
    }
}
