namespace CS2MCP.Core
{
    /// <summary>Descriptions qualify raw native values; no rescaling or sign normalization.</summary>
    public static class MetricContracts
    {
        public static DemandMetadata DescribeDemand() => new DemandMetadata();
        public static BudgetMetadata DescribeBudget() => new BudgetMetadata();
        public static RouteMetricMetadata DescribeRoute(float nativeLength, bool isCargo, int load)
            => new RouteMetricMetadata(nativeLength, isCargo, load);
        public static CalendarMetadata DescribeCalendar(float normalizedDate, float normalizedTime, int year, int daysPerYear)
            => new CalendarMetadata(normalizedDate, normalizedTime, year, daysPerYear);
        public static ServiceBuildingCountMetadata DescribeServiceBuildingCount(int count)
            => new ServiceBuildingCountMetadata(count);
    }

    public sealed class DemandMetadata
    {
        public int schemaVersion => 1;
        public int buildingDemandMin => 0;
        public int buildingDemandMax => 255;
        public string buildingDemandUnit => "nativeDemand";
        public string companyDemandUnit => "nativeCounter";
        public string refreshPeriod => "simulationUpdates";
        public string factorConvention => "rawSignedContributions";
    }

    public sealed class BudgetMetadata
    {
        public int schemaVersion => 1;
        public string ratePeriod => "month";
        public string refreshPeriod => "gameHour";
        public string totalExpensesConvention => "signedNegativeCosts";
        public string expenseComponentsConvention => "positiveCosts";
        public string balanceConvention => "totalIncome + totalExpenses";
        public string moneyDeltaConvention => "rawNativeValue";
        public string nativeParityStatus => "unverified";
        public string reason => "nativeBudgetParityNotRun";
    }

    public sealed class RouteMetricMetadata
    {
        public int schemaVersion => 1;
        public float rawLength { get; }
        public string lengthSource => "UITransportLineData.length";
        public float? validatedLengthMeters { get; }
        public string lengthStatus => validatedLengthMeters.HasValue ? "valid" : "invalid";
        public string lengthWarning => validatedLengthMeters.HasValue ? null : "native route length is negative or non-finite; no usable metre value";
        public string lengthValidation => "finiteNonnegativeOnly";
        public string nativeParityStatus => "unverified";
        public string nativeParityReason => "nativeRouteParityNotRun";
        public int rawLoad { get; }
        public string loadSource => "UITransportLineData.cargo";
        public string loadKind { get; }
        public string loadUnit { get; }
        public string loadUnitStatus { get; }
        public string loadUnitReason { get; }
        public string[] deprecatedAliases => new[] { "lengthMeters", "passengersOnBoard" };

        internal RouteMetricMetadata(float nativeLength, bool isCargo, int load)
        {
            rawLength = nativeLength;
            validatedLengthMeters = !float.IsNaN(nativeLength) && !float.IsInfinity(nativeLength) && nativeLength >= 0
                ? nativeLength : (float?)null;
            rawLoad = load;
            loadKind = isCargo ? "cargo" : "passengers";
            loadUnit = isCargo ? "unknown" : "passengers";
            loadUnitStatus = isCargo ? "unverified" : "sourceDefined";
            loadUnitReason = isCargo ? "nativeCargoAmountUnitNotVerified" : "passengerRoutePanelLoad";
        }
    }

    public sealed class CalendarMetadata
    {
        public int schemaVersion => 1;
        public float rawNormalizedDate { get; }
        public float rawNormalizedTime { get; }
        public int rawYear { get; }
        public int rawDaysPerYear { get; }
        public string conversionStatus => "unverified";
        public string reason => "nativeCalendarParityNotRun";
        public string source => "TimeSystem";
        public string legacyConversion => "TimeSystem.GetCurrentDateTime";

        internal CalendarMetadata(float normalizedDate, float normalizedTime, int year, int daysPerYear)
        {
            rawNormalizedDate = normalizedDate;
            rawNormalizedTime = normalizedTime;
            rawYear = year;
            rawDaysPerYear = daysPerYear;
        }
    }

    public sealed class ServiceBuildingCountMetadata
    {
        public int schemaVersion => 1;
        public int rawCount { get; }
        public string status => "unverified";
        public string reason => "nativeServiceBuildingCountParityNotRun";
        public string source => "CityServiceBudgetSystem.GetNumberOfServiceBuildings";
        internal ServiceBuildingCountMetadata(int count) { rawCount = count; }
    }
}
