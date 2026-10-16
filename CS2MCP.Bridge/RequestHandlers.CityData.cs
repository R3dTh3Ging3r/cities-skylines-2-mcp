using System;
using CS2MCP.Core;
using System.Collections.Generic;
using Game.City;
using Game.Companies;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

namespace CS2MCP
{
    /// <summary>
    /// Read endpoints: service infrastructure, labor market, statistics history.
    /// Data sources mirror the vanilla infoview UI systems.
    /// </summary>
    public sealed partial class RequestHandlers
    {
        private BridgeResponse GetServices()
        {
            if (!TryGetCity(out _, out BridgeResponse error)) return error;
            return BridgeResponse.Json(ReadServicesData());
        }

        private CityServicesData ReadServicesData()
        {
            if (!TryGetCity(out _, out BridgeResponse error))
            {
                throw new OperationException(new BridgeErrorData(ErrorCodes.NotReady,"city unavailable",httpStatus:409));
            }

            ElectricityStatisticsSystem electricity = World.GetOrCreateSystemManaged<ElectricityStatisticsSystem>();
            ElectricityTradeSystem electricityTrade = World.GetOrCreateSystemManaged<ElectricityTradeSystem>();
            WaterStatisticsSystem water = World.GetOrCreateSystemManaged<WaterStatisticsSystem>();
            WaterTradeSystem waterTrade = World.GetOrCreateSystemManaged<WaterTradeSystem>();
            GarbageAccumulationSystem garbage = World.GetOrCreateSystemManaged<GarbageAccumulationSystem>();

            return new CityServicesData
            {
                electricity = new CityElectricityData
                {
                    production = electricity.production,
                    consumption = electricity.consumption,
                    fulfilledConsumption = electricity.fulfilledConsumption,
                    batteryCharge = electricity.batteryCharge,
                    batteryCapacity = electricity.batteryCapacity,
                    @import = electricityTrade.import,
                    export = electricityTrade.export,
                },
                water = new CityWaterData
                {
                    freshCapacity = water.freshCapacity,
                    freshConsumption = water.freshConsumption,
                    sewageCapacity = water.sewageCapacity,
                    sewageConsumption = water.sewageConsumption,
                    freshImport = waterTrade.freshImport,
                    freshExport = waterTrade.freshExport,
                    sewageExport = waterTrade.sewageExport,
                },
                garbage = new CityGarbageData
                {
                    accumulationRate = garbage.garbageAccumulation,
                },
                note = "healthcare/education coverage: query /city/statistics (e.g. type=EducationCount) until dedicated endpoints land",
            };
        }

        private BridgeResponse GetLabor()
        {
            if (!TryGetCity(out _, out BridgeResponse error)) return error;
            return BridgeResponse.Json(ReadLaborData());
        }

        private CityLaborData ReadLaborData()
        {
            if (!TryGetCity(out _, out BridgeResponse error))
            {
                throw new OperationException(new BridgeErrorData(ErrorCodes.NotReady,"city unavailable",httpStatus:409));
            }

            CountHouseholdDataSystem households = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            CountWorkplacesSystem workplaces = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();

            Workplaces total = workplaces.GetTotalWorkplaces();
            Workplaces free = workplaces.GetFreeWorkplaces();

            return new CityLaborData
            {
                employed = households.CityWorkerCount,
                unemploymentRate = households.UnemploymentRate,
                homelessCitizens = households.HomelessCitizenCount,
                homelessnessRate = households.HomelessnessRate,
                jobs = new CityJobsData
                {
                    total = total.TotalCount,
                    free = free.TotalCount,
                    totalByEducation = new EducationCounters
                    {
                        uneducated = total.m_Uneducated,
                        poorlyEducated = total.m_PoorlyEducated,
                        educated = total.m_Educated,
                        wellEducated = total.m_WellEducated,
                        highlyEducated = total.m_HighlyEducated,
                    },
                    freeByEducation = new EducationCounters
                    {
                        uneducated = free.m_Uneducated,
                        poorlyEducated = free.m_PoorlyEducated,
                        educated = free.m_Educated,
                        wellEducated = free.m_WellEducated,
                        highlyEducated = free.m_HighlyEducated,
                    },
                },
                ageStructure = new CityAgeData
                {
                    children = households.ChildrenCount,
                    teens = households.TeenCount,
                    adults = households.AdultCount,
                    seniors = households.SeniorCount,
                    totalCitizens = households.MovedInCitizenCount,
                },
            };
        }

        private BridgeResponse GetStatistics(BridgeRequest request)
        {
            if (!TryGetCity(out _, out BridgeResponse error))
            {
                return error;
            }

            if (!request.Query.TryGetValue("type", out string typeName))
            {
                return BridgeResponse.Error(400,
                    "provide ?type=<StatisticType>, e.g. Population, Money, Income, Expense, Unemployed, CrimeRate; " +
                    "optional &parameter=<int> and &samples=<1-512>");
            }
            if (!Enum.TryParse(typeName, ignoreCase: true, out StatisticType type)
                || type == StatisticType.Invalid || type == StatisticType.Count)
            {
                return BridgeResponse.Error(400,
                    $"unknown statistic type '{typeName}'; valid types: {string.Join(", ", Enum.GetNames(typeof(StatisticType)))}");
            }

            request.TryGetInt("parameter", out int parameter);
            int samples = request.TryGetInt("samples", out int requested)
                ? Math.Max(1, Math.Min(requested, 512))
                : 64;

            CityStatisticsSystem statistics = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            long current = statistics.GetStatisticValueLong(type, parameter);

            NativeArray<int> data = statistics.GetStatisticDataArray(type, parameter);
            int count = data.Length;
            int start = Math.Max(0, count - samples);
            var values = new List<int>(count - start);
            var frames = new List<uint>(count - start);
            for (int i = start; i < count; i++)
            {
                values.Add(data[i]);
                frames.Add(statistics.GetSampleFrameIndex(i));
            }

            return BridgeResponse.Json(new
            {
                type = type.ToString(),
                parameter,
                current,
                samplesReturned = values.Count,
                totalSamples = count,
                note = "samples are taken 32 times per in-game day (262144 frames = 1 day); frames[i] is the simulation frame of values[i]",
                frames,
                values,
            });
        }
    }
}
