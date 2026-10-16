using System.Collections.Generic;
using CS2MCP.Core;
namespace CS2MCP
{
    public sealed partial class RequestHandlers
    {
        bool IObservationReaders.Warming => !m_System.SimulationHasTickedSinceLoad;
        object IObservationReaders.State() => ReadStateData();
        object IObservationReaders.Overview() => ReadOverviewData();
        object IObservationReaders.Services() => ReadServicesData();
        object IObservationReaders.Labor() => ReadLaborData();
        object IObservationReaders.Budget() => ReadBudgetData();
        Observation<ConstructionSnapshot> IObservationReaders.Inventory(IDictionary<string, string> query) => ReadConstructionInventory(query);
    }
}
