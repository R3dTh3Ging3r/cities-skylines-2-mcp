using System;
using CS2MCP.Core;

namespace CS2MCP
{
    public sealed partial class RequestHandlers : IDiagnosticMetricSources
    {
        bool IDiagnosticMetricSources.Warming => !m_System.SimulationHasTickedSinceLoad;

        CityStateData IDiagnosticMetricSources.State() => ReadStateData();
        CityOverviewData IDiagnosticMetricSources.Overview() => ReadOverviewData();
        CityLaborData IDiagnosticMetricSources.Labor() => ReadLaborData();
        CityBudgetData IDiagnosticMetricSources.Budget() => ReadBudgetData();
        Observation<TransitPage> IDiagnosticMetricSources.SelectedLine(TransitQuery query) => ReadTransitData(query);
        private Observation<MetricBatch> ReadMetricBatch(MetricReadRequest request) => new DiagnosticMetricReader(this, Mod.Worlds, () => m_System.CurrentFrame, () => DateTime.UtcNow).Read(request);
        private BridgeResponse MetricsRequest(BridgeRequest request)
        {
            try
            {
                if (request.Metrics == null)
                    throw CommandJson.Invalid("validated metric request required");
                var batch = ReadMetricBatch(request.Metrics);
                var dictionary = new Observation<System.Collections.Generic.Dictionary<string, MetricValue>>
                {
                    data = batch.data.values,
                    world = batch.world,
                    startFrame = batch.startFrame,
                    endFrame = batch.endFrame,
                    sampledAtUtc = batch.sampledAtUtc,
                    freshness = batch.freshness,
                    warnings = batch.warnings
                };
                return BoundedMetricResponse(dictionary);
            }
            catch (OperationException error)
            {
                var e = error.Error;
                return BridgeResponse.Error(e.httpStatus ?? 400, e.message, e.code, e.retryable, e.outcome, e.operationId);
            }
        }

        private static BridgeResponse BoundedMetricResponse(object value)
        {
            try
            {
                return new BridgeResponse
                {
                    Body = ObservationWire.Standalone(value)
                };
            }
            catch (InvalidOperationException)
            {
                return BridgeResponse.Error(413, "metric result exceeds bounded response budget", "RESULT_TOO_LARGE");
            }
        }

        private BridgeResponse MetricCatalogRequest(BridgeRequest request)
        {
            var query = request.MetricCatalog;
            if (query == null)
                return BridgeResponse.Error(400, "validated metric catalog request required", ErrorCodes.InvalidArgument);
            var definitions = DiagnosticMetricCatalog.Read(query.TryGetValue("family", out var family) ? family : "all");
            object result = definitions;
            if (query.TryGetValue("envelope", out var envelope) && envelope == "true")
            {
                var snapshot = Mod.Worlds.Snapshot;
                result = new
                {
                    schemaVersion = 1,
                    definitionRevision = DiagnosticMetricCatalog.Revision,
                    definitions,
                    world = snapshot.world,
                    ready = snapshot.ready,
                    sampledAtUtc = DateTime.UtcNow,
                    freshness = snapshot.ready ? "current" : "unavailable",
                    freshnessReason = "managedReadinessOnlyNativeParityNotRun",
                    identity = Mod.Identity,
                    nativeVerification = "diagnostic-metrics:notRun"
                };
            }

            return BoundedMetricResponse(result);
        }
    }
}
