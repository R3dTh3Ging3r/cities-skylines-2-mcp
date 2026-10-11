using System;
using System.Collections.Generic;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>
    /// Management endpoints: loans (borrow/repay) and service fees
    /// (electricity/water/education... pricing), plus a generic standalone
    /// object listing (trees etc.) so placed decorations can be found and
    /// demolished again.
    /// </summary>
    public sealed partial class RequestHandlers
    {
        private EntityQuery m_StandaloneObjectQuery;
        private bool m_StandaloneObjectQueryCreated;

        private EntityQuery StandaloneObjectQuery
        {
            get
            {
                if (!m_StandaloneObjectQueryCreated)
                {
                    m_StandaloneObjectQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
                    {
                        All = new[]
                        {
                            ComponentType.ReadOnly<Game.Objects.Static>(),
                            ComponentType.ReadOnly<Game.Objects.Transform>(),
                            ComponentType.ReadOnly<PrefabRef>(),
                        },
                        Any = new[]
                        {
                            ComponentType.ReadOnly<Game.Objects.Tree>(),
                            ComponentType.ReadOnly<Game.Objects.Plant>(),
                        },
                        None = new[]
                        {
                            ComponentType.ReadOnly<Game.Tools.Temp>(),
                            ComponentType.ReadOnly<Game.Common.Deleted>(),
                            ComponentType.ReadOnly<Game.Common.Owner>(),
                        },
                    });
                    m_StandaloneObjectQueryCreated = true;
                }
                return m_StandaloneObjectQuery;
            }
        }

        private BridgeResponse GetLoan()
        {
            if (!TryGetCity(out Entity city, out BridgeResponse error))
            {
                return error;
            }
            LoanSystem loans = World.GetOrCreateSystemManaged<LoanSystem>();
            LoanInfo current = loans.CurrentLoan;
            return BridgeResponse.Json(new
            {
                currentLoan = new
                {
                    amount = current.m_Amount,
                    dailyInterestRate = current.m_DailyInterestRate,
                    dailyPayment = current.m_DailyPayment,
                },
                creditworthiness = loans.Creditworthiness,
                constraints = new { schemaVersion = 1,
                    range = new ValueRange(math.max(0, current.m_Amount - math.max(0, EntityManager.GetComponentData<PlayerMoney>(city).money)),
                        loans.Creditworthiness, "LoanSystem.ClampLoanAmount; treasury-limited repayment"),
                    step = (double?)null, stepReason = "native UI step not verified", nativeParityStatus = "notRun" },
                note = "loan changes are queued; repayment depends on treasury cash; observe currentLoan later for effective principal",
            });
        }

        private BridgeResponse SetLoan(BridgeRequest request)
        {
            if (!TryGetCity(out Entity city, out BridgeResponse error))
            {
                return error;
            }
            if (!request.TryGetInt("amount", out int amount))
            {
                return BridgeResponse.Error(400, "provide ?amount=<int> (new loan principal; 0 repays fully)");
            }
            LoanSystem loans = World.GetOrCreateSystemManaged<LoanSystem>();
            int applied = math.clamp(amount, 0, loans.Creditworthiness);
            LoanInfo before = loans.CurrentLoan;
            int availableCash = math.max(0, EntityManager.GetComponentData<PlayerMoney>(city).money);
            int minPrincipal = math.max(0, before.m_Amount - availableCash);
            LoanInfo offer = loans.RequestLoanOffer(applied);
            loans.ChangeLoan(applied);
            LoanInfo current = loans.CurrentLoan;
            return BridgeResponse.Json(new
            {
                requestedAmount = amount,
                appliedAmount = applied,
                offer = new { offer.m_Amount, offer.m_DailyInterestRate, offer.m_DailyPayment },
                currentLoan = new { current.m_Amount, current.m_DailyInterestRate, current.m_DailyPayment },
                effectiveCandidate = offer.m_Amount,
                observedPrincipal = current.m_Amount,
                observedTiming = "immediateAfterQueue; not completion evidence",
                setterReadback = SetterReadback.Pending(amount, new ValueRange(minPrincipal, loans.Creditworthiness,
                    "LoanSystem.ClampLoanAmount; treasury-limited repayment"), null, "LoanSystem.ChangeLoan queues LoanAction",
                    amount == applied ? "later CurrentLoan observation required; native UI step unknown" : "legacyClamped; later CurrentLoan observation required; native UI step unknown"),
            });
        }

        private BridgeResponse GetFees()
        {
            if (!TryGetCity(out Entity city, out BridgeResponse error))
            {
                return error;
            }
            ServiceFeeSystem feeSystem = World.GetOrCreateSystemManaged<ServiceFeeSystem>();
            DynamicBuffer<ServiceFee> fees = EntityManager.GetBuffer<ServiceFee>(city, isReadOnly: true);
            var result = new Dictionary<string, object>();
            foreach (PlayerResource resource in Enum.GetValues(typeof(PlayerResource)))
            {
                if ((int)resource < 0 || resource >= PlayerResource.Count)
                {
                    continue;
                }
                if (ServiceFeeSystem.TryGetFee(resource, fees, out float fee))
                {
                    int3 totals = feeSystem.GetServiceFees(resource);
                    bool hasParameters = TryGetFeeParameters(resource, out FeeParameters parameters);
                    result[resource.ToString()] = new
                    {
                        fee,
                        estimatedMonthlyIncome = feeSystem.GetServiceFeeIncomeEstimate(resource, fee),
                        sliderRange = new { min = totals.x, max = totals.y, defaultValue = totals.z },
                        sliderRangeDeprecation = "legacy mislabeled financial totals: min=incomeInternal, max=incomeExports, defaultValue=raw signed imports; use constraints",
                        financialRates = new { schemaVersion = 1, incomeInternal = totals.x, incomeExports = totals.y, expenseImports = -(long)totals.z,
                            ratePeriod = "unknown", refreshPeriod = "unknown", source = "ServiceFeeSystem.GetServiceFees; native UI field mapping", nativeParityStatus = "notRun" },
                        constraints = hasParameters ? new {
                            schemaVersion = 1, range = new ValueRange(0, parameters.m_Max, "ServiceFeeParameterData.GetFeeParameters; native UI min=0"),
                            defaultValue = parameters.m_Default, adjustable = parameters.m_Adjustable && resource != PlayerResource.Parking,
                            step = (double?)null, stepReason = "native UI step not verified", nativeParityStatus = "notRun"
                        } : (object)new { schemaVersion = 1, range = (object)null, step = (double?)null, reason = "fee parameters unavailable", nativeParityStatus = "notRun" },
                    };
                }
            }
            return BridgeResponse.Json(new
            {
                note = "set with /city/fees/set?resource=<name>&fee=<float>; fees affect service income and citizen happiness",
                fees = result,
                stalenessWarning = LockStalenessWarning,
            });
        }

        private BridgeResponse SetFee(BridgeRequest request)
        {
            if (!TryGetCity(out Entity city, out BridgeResponse error))
            {
                return error;
            }
            if (!request.Query.TryGetValue("resource", out string resourceName)
                || !Enum.TryParse(resourceName, ignoreCase: true, out PlayerResource resource)
                || !Enum.IsDefined(typeof(PlayerResource), resource) || (int)resource < 0 || resource >= PlayerResource.Count)
            {
                return BridgeResponse.Error(400,
                    $"provide ?resource=<{string.Join("|", Enum.GetNames(typeof(PlayerResource)))}>");
            }
            if (!request.TryGetFloat("fee", out float fee) || !ValueValidation.IsFinite(fee))
            {
                return BridgeResponse.Error(400, "provide ?fee=<float>");
            }
            if (!TryGetFeeParameters(resource, out FeeParameters parameters))
                return BridgeResponse.Error(409, "fee parameters unavailable; no fee changed", ErrorCodes.NotReady);
            if (!parameters.m_Adjustable || resource == PlayerResource.Parking)
                return BridgeResponse.Error(400, $"resource '{resource}' is not adjustable in the native service-fee UI");
            ValueCheck check = ValueValidation.Check(fee, 0, parameters.m_Max, 0);
            if (!check.accepted)
                return BridgeResponse.Error(400, $"fee must be finite and within 0-{parameters.m_Max}: {check.reason}");
            DynamicBuffer<ServiceFee> fees = EntityManager.GetBuffer<ServiceFee>(city);
            if (!ServiceFeeSystem.TryGetFee(resource, fees, out float previous))
            {
                return BridgeResponse.Error(400, $"resource '{resource}' has no adjustable fee in this city");
            }
            ServiceFeeSystem.SetFee(resource, fees, fee);
            bool readback = ServiceFeeSystem.TryGetFee(resource, fees, out float observed);
            return BridgeResponse.Json(new
            {
                resource = resource.ToString(),
                previousFee = previous,
                newFee = fee,
                setterReadback = SetterReadback.Observed(fee, readback ? (object)observed : null,
                    new ValueRange(0, parameters.m_Max, "ServiceFeeParameterData.GetFeeParameters; native UI min=0"), null,
                    "ServiceFeeSystem.TryGetFee", "newFee is the legacy input candidate; native UI step unknown"),
            });
        }

        private bool TryGetFeeParameters(PlayerResource resource, out FeeParameters parameters)
        {
            using (EntityQuery query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ServiceFeeParameterData>()))
            {
                if (query.CalculateEntityCount() != 1) { parameters = default; return false; }
                parameters = query.GetSingleton<ServiceFeeParameterData>().GetFeeParameters(resource);
                return ValueValidation.IsFinite(parameters.m_Max) && parameters.m_Max >= 0;
            }
        }

        private BridgeResponse ListObjects(BridgeRequest request)
        {
            if (!TryGetCity(out _, out BridgeResponse error))
            {
                return error;
            }
            request.Query.TryGetValue("query", out string search);
            int limit = request.TryGetInt("limit", out int rawLimit) ? math.clamp(rawLimit, 1, 500) : 100;
            bool hasCenter = request.TryGetFloat("x", out float x) & request.TryGetFloat("z", out float z);
            float radius = request.TryGetFloat("radius", out float rawRadius) ? math.max(rawRadius, 1f) : 250f;
            float2 center = new float2(x, z);

            PrefabSystem prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            var results = new List<object>();
            int total = 0;
            using (NativeArray<Entity> entities = StandaloneObjectQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity entity in entities)
                {
                    Game.Objects.Transform transform = EntityManager.GetComponentData<Game.Objects.Transform>(entity);
                    if (hasCenter && math.distance(transform.m_Position.xz, center) > radius)
                    {
                        continue;
                    }
                    PrefabBase prefab = prefabSystem.GetPrefab<PrefabBase>(EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab);
                    string name = prefab != null ? prefab.name : "<unknown>";
                    if (!string.IsNullOrEmpty(search) && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    total++;
                    if (results.Count < limit)
                    {
                        results.Add(new
                        {
                            entity = new { index = entity.Index, version = entity.Version },
                            prefab = name,
                            position = new { x = transform.m_Position.x, y = transform.m_Position.y, z = transform.m_Position.z },
                        });
                    }
                }
            }
            return BridgeResponse.Json(new
            {
                totalMatches = total,
                returned = results.Count,
                note = "standalone trees/plants; use entity index+version with /build/demolish",
                objects = results,
            });
        }
    }
}
