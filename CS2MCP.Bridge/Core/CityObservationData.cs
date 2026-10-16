using System;
using System.Collections.Generic;
namespace CS2MCP.Core
{
    public interface IBoundedObservationData
    {
        void ValidateObservationBounds();
    }
    public sealed class CitySimulationData
    {
        public bool paused { get; set; }
        public double selectedSpeed { get; set; }
        public double effectiveSpeed { get; set; }
        public long frameIndex { get; set; }
        public string gameDateTime { get; set; }
        public CalendarMetadata calendarMetadata { get; set; }
    }
    public sealed class CityStateData : IBoundedObservationData
    {
        public string gameMode { get; set; }
        public bool isLoading { get; set; }
        public bool cityLoaded { get; set; }
        public string cityName { get; set; }
        public CitySimulationData simulation { get; set; }
        public void ValidateObservationBounds()
        {
            CityObservationBounds.Text(cityName, 256);
            CityObservationBounds.Text(gameMode, 128);
        }
    }
    public sealed class CityOverviewData : IObservationDataWarnings, IBoundedObservationData
    {
        public string cityName { get; set; }
        public double population { get; set; }
        public double populationWithMoveIn { get; set; }
        public double averageHappiness { get; set; }
        public double averageHealth { get; set; }
        public double money { get; set; }
        public double xp { get; set; }
        public int gameYear { get; set; }
        public string gameDateTime { get; set; }
        public CalendarMetadata calendarMetadata { get; set; }
        public bool simulationPaused { get; set; }
        public double simulationSpeed { get; set; }
        internal bool PopulationAvailable { get; set; }
        string[] IObservationDataWarnings.ObservationWarnings => PopulationAvailable ? new string[0] : new[] { "populationComponentUnavailableLegacyDefaults" };
        public void ValidateObservationBounds() => CityObservationBounds.Text(cityName, 256);
    }
    public sealed class CityElectricityData
    {
        public double production { get; set; }
        public double consumption { get; set; }
        public double fulfilledConsumption { get; set; }
        public double batteryCharge { get; set; }
        public double batteryCapacity { get; set; }
        public double @import { get; set; }
        public double export { get; set; }
    }
    public sealed class CityWaterData
    {
        public double freshCapacity { get; set; }
        public double freshConsumption { get; set; }
        public double sewageCapacity { get; set; }
        public double sewageConsumption { get; set; }
        public double freshImport { get; set; }
        public double freshExport { get; set; }
        public double sewageExport { get; set; }
    }
    public sealed class CityGarbageData
    {
        public double accumulationRate { get; set; }
    }
    public sealed class CityServicesData
    {
        public CityElectricityData electricity { get; set; }
        public CityWaterData water { get; set; }
        public CityGarbageData garbage { get; set; }
        public string note { get; set; }
    }
    public sealed class EducationCounters
    {
        public int uneducated { get; set; }
        public int poorlyEducated { get; set; }
        public int educated { get; set; }
        public int wellEducated { get; set; }
        public int highlyEducated { get; set; }
    }
    public sealed class CityJobsData
    {
        public int total { get; set; }
        public int free { get; set; }
        public EducationCounters totalByEducation { get; set; }
        public EducationCounters freeByEducation { get; set; }
    }
    public sealed class CityAgeData
    {
        public int children { get; set; }
        public int teens { get; set; }
        public int adults { get; set; }
        public int seniors { get; set; }
        public int totalCitizens { get; set; }
    }
    public sealed class CityLaborData
    {
        public int employed { get; set; }
        public double unemploymentRate { get; set; }
        public int homelessCitizens { get; set; }
        public double homelessnessRate { get; set; }
        public CityJobsData jobs { get; set; }
        public CityAgeData ageStructure { get; set; }
    }
    public sealed class CityBudgetData
    {
        public BudgetMetadata metricMetadata { get; set; }
        public string note { get; set; }
        public double totalIncome { get; set; }
        public double totalExpenses { get; set; }
        public double totalTaxIncome { get; set; }
        public double balance { get; set; }
        public double moneyDelta { get; set; }
        public Dictionary<string, int> income { get; set; }
        public Dictionary<string, int> expenses { get; set; }
    }
    public static class CityObservationBounds
    {
        public const int MaxBudgetCategories = 128;
        public static void Text(string text, int max)
        {
            if (text != null && text.Length > max)
                throw CommandJson.Invalid("native context string exceeds observation bound");
        }
        public static void BudgetCounts(int income, int expense)
        {
            if (income < 0 || expense < 0 || income > MaxBudgetCategories || expense > MaxBudgetCategories)
                throw CommandJson.Invalid("native budget category count unsupported");
        }
    }
}
