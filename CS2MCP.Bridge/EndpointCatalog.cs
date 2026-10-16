using System;
using System.Collections.Generic;
using System.Linq;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>Fixed route/method classification; completion metadata is not native verification.</summary>
    public sealed class EndpointDescriptor
    {
        public string path { get; }
        public IReadOnlyList<string> legacyHttpVerbs { get; }
        public string access { get; }
        public string capability { get; }
        public string completionMechanism { get; }

        internal EndpointDescriptor(string path, string access, string capability, string completionMechanism, string verb = "GET")
        {
            this.path = path;
            legacyHttpVerbs = Array.AsReadOnly(new[] { verb });
            this.access = access;
            this.capability = capability;
            this.completionMechanism = completionMechanism;
        }
    }

    /// <summary>Investigation metadata only. It grants neither command admission nor native support.</summary>
    public sealed class NativeValidationProbe
    {
        public string id { get; }
        public string fixture => "lifecycle";
        public bool validationOnly => true;
        public string support => "unverified";
        public bool dispatchAvailable => false;
        internal NativeValidationProbe(string id) { this.id = id; }
    }

    public sealed class NativeValidationMetadata
    {
        public bool enabled { get; }
        public bool dispatchAvailable => false;
        public string reason => "operationGuardsNotImplemented";
        public IReadOnlyList<NativeValidationProbe> probes { get; } = Array.AsReadOnly(new[] {
            new NativeValidationProbe("preloadOrder"), new NativeValidationProbe("toolApplyFinish"),
            new NativeValidationProbe("manualToolSwitch"), new NativeValidationProbe("saveIdentity"),
            new NativeValidationProbe("previewOwnership")
        });
        public NativeValidationMetadata(string startupValue) { enabled = startupValue == "true"; }
    }

    public sealed class OperationProtocolMetadata
    {
        public int version => 2;
        public bool dispatchAvailable => false;
        public bool scopedStatusAvailable => false;
        public string reason => "completionAndLeaseGuardsUnavailable";
        public string legacyWrites => "unmigrated";
        public WorldRef world { get; }
        public bool ready { get; }
        public int registryCapacity => OperationRegistry.Capacity;
        public int pendingCapacity => OperationRegistry.PendingCapacity;
        public int maxBodyBytes => HttpIngress.MaxBodyBytes;
        internal OperationProtocolMetadata(WorldSnapshot snapshot) { world = snapshot?.world; ready = snapshot?.ready ?? false; }
    }

    public sealed class ObservationProtocolMetadata
    {
        public string support => "partial";
        public IReadOnlyList<string> names => ObservationCatalog.Names;
        public int maxItems => 64;
        public int activeGroups => ObservationScheduler.GroupCapacity;
        public int sharedPendingItems => ObservationScheduler.PendingCapacity;
        public int sharedReadsPerUpdate => ObservationScheduler.MaxReadsPerUpdate;
        public double additionalWorkMilliseconds => ObservationScheduler.MaxAdditionalMilliseconds;
        public int maxItemBytes => ObservationScheduler.MaxItemBytes;
        public int maxGroupBytes => ObservationScheduler.MaxGroupBytes;
        public string nativePerformance => "notRun";
    }

    public sealed class CapabilityManifest
    {
        public int protocolVersion => 1;
        public int schemaVersion => 1;
        public BuildIdentity identity { get; }
        public RuntimeEvidence runtime { get; }
        public IReadOnlyList<EndpointDescriptor> endpoints => EndpointCatalog.All;
        public IReadOnlyList<Capability> capabilities { get; }
        public IReadOnlyList<Capability> nativeSupport { get; }
        public IReadOnlyList<string> degradedReasons { get; }
        public NativeValidationMetadata nativeValidation { get; }
        public OperationProtocolMetadata operationProtocol { get; }
        public ObservationProtocolMetadata observationProtocol { get; } = new ObservationProtocolMetadata();

        internal CapabilityManifest(BuildIdentity identity, RuntimeEvidence runtime, NativeValidationMetadata validation, WorldSnapshot world = null)
        {
            this.identity = identity;
            this.runtime = runtime;
            nativeValidation = validation;
            operationProtocol = new OperationProtocolMetadata(world);
            capabilities = Array.AsReadOnly(EndpointCatalog.All.Select(endpoint => new Capability {
                name = endpoint.capability, support = endpoint.path.StartsWith("/v2/metrics", StringComparison.Ordinal) || endpoint.path.StartsWith("/v2/transit/", StringComparison.Ordinal) || endpoint.path == "/v2/observations" || endpoint.path == "/v2/construction/inventory" ? "degraded" : EndpointCatalog.IsOperationEndpoint(endpoint) ? "unverified" : "supported",
                reason = endpoint.path.StartsWith("/v2/metrics", StringComparison.Ordinal) ? "fixedTypedMetricsOnlyNativeNotRun" : endpoint.path.StartsWith("/v2/transit/", StringComparison.Ordinal) ? "selectedLineOnlyStructuralFactsNativeNotRun" : endpoint.path == "/v2/observations" ? "fixedBoundedReadersOnlyNativeNotRun" : endpoint.path == "/v2/construction/inventory" ? "explicitEntitiesOnlyRootGeometryNativeNotRun" : EndpointCatalog.IsOperationEndpoint(endpoint) ? "completionAndLeaseGuardsUnavailable" : null
            }).ToArray());
            nativeSupport = Array.AsReadOnly(EndpointCatalog.All
                .Where(endpoint => endpoint.path != "/ping" && endpoint.path != "/capabilities")
                .Select(endpoint => new Capability {
                    name = endpoint.capability, support = "unverified", reason = "nativeFixtureNotRun"
                }).Concat(new[] { new Capability {
                    name = "transitPassengerAirCreation", support = "unsupported",
                    reason = "legacy transit stop and line creation explicitly reject airplane mode"
                } }).ToArray());
            var reasons = new List<string> { "nativeFixtureNotRun", identity.gameVersionReason };
            if (identity.sourceRevisionReason != null) reasons.Add(identity.sourceRevisionReason);
            if (identity.buildIdentityReason != null) reasons.Add(identity.buildIdentityReason);
            if (runtime.reason != null) reasons.Add(runtime.reason);
            else if (runtime.isLoading == true) reasons.Add("gameLoading");
            else if (runtime.cityLoaded == false) reasons.Add("gameNotLoaded");
            degradedReasons = reasons.AsReadOnly();
        }
    }

    /// <summary>Authoritative route/access inventory for this protocol-1 bridge.</summary>
    public static class EndpointCatalog
    {
        public static IReadOnlyList<EndpointDescriptor> All { get; } = Array.AsReadOnly(new[] {
            new EndpointDescriptor("/ping", "read", "cs2_ping", "off-thread-liveness"),
            new EndpointDescriptor("/state", "read", "cs2_game_state", "sync-read"),
            new EndpointDescriptor("/city/overview", "read", "cs2_city_overview", "sync-read"),
            new EndpointDescriptor("/city/demand", "read", "cs2_demand", "sync-read"),
            new EndpointDescriptor("/city/budget", "read", "cs2_budget", "sync-read"),
            new EndpointDescriptor("/city/services", "read", "cs2_city_services", "sync-read"),
            new EndpointDescriptor("/city/labor", "read", "cs2_labor", "sync-read"),
            new EndpointDescriptor("/city/statistics", "read", "cs2_statistics", "sync-read"),
            new EndpointDescriptor("/city/taxes", "read", "cs2_get_taxes", "sync-read"),
            new EndpointDescriptor("/city/taxes/set", "cityWrite", "cs2_set_tax", "sync-set"),
            new EndpointDescriptor("/city/policies", "read", "cs2_policies", "sync-read"),
            new EndpointDescriptor("/city/policies/set", "cityWrite", "cs2_set_policy", "end-frame-ack"),
            new EndpointDescriptor("/city/service-budgets", "read", "cs2_service_budgets", "sync-read"),
            new EndpointDescriptor("/city/service-budgets/set", "cityWrite", "cs2_set_service_budget", "sync-set"),
            new EndpointDescriptor("/prefabs", "read", "cs2_find_prefabs", "sync-read"),
            new EndpointDescriptor("/build/place", "cityWrite", "cs2_place_building", "tool-apply-ack"),
            new EndpointDescriptor("/build/road", "cityWrite", "cs2_build_road", "tool-apply-ack"),
            new EndpointDescriptor("/build/upgrade", "cityWrite", "cs2_upgrade_road", "tool-apply-ack"),
            new EndpointDescriptor("/build/junction", "cityWrite", "cs2_set_junction_control", "tool-apply-ack"),
            new EndpointDescriptor("/zones", "read", "cs2_list_zones", "sync-read"),
            new EndpointDescriptor("/build/zone", "cityWrite", "cs2_zone_area", "sync-set"),
            new EndpointDescriptor("/build/demolish", "cityWrite", "cs2_demolish", "tool-apply-ack"),
            new EndpointDescriptor("/city/buildings", "read", "cs2_list_buildings", "sync-read"),
            new EndpointDescriptor("/city/roads", "read", "cs2_list_roads", "sync-read"),
            new EndpointDescriptor("/city/loan", "read", "cs2_get_loan", "sync-read"),
            new EndpointDescriptor("/city/loan/set", "cityWrite", "cs2_set_loan", "loan-action-queued-ack; pending"),
            new EndpointDescriptor("/city/fees", "read", "cs2_get_fees", "sync-read"),
            new EndpointDescriptor("/city/fees/set", "cityWrite", "cs2_set_fee", "sync-set"),
            new EndpointDescriptor("/city/objects", "read", "cs2_list_objects", "sync-read"),
            new EndpointDescriptor("/camera", "read", "cs2_get_camera", "sync-read"),
            new EndpointDescriptor("/camera/set", "uiWrite", "cs2_set_camera", "sync-set"),
            new EndpointDescriptor("/city/terrain", "read", "cs2_terrain", "sync-read"),
            new EndpointDescriptor("/city/gridmap", "read", "cs2_gridmap", "sync-read"),
            new EndpointDescriptor("/city/zoning", "read", "cs2_zoning", "sync-read"),
            new EndpointDescriptor("/city/notifications", "read", "cs2_notifications", "sync-read"),
            new EndpointDescriptor("/entity/inspect", "read", "cs2_inspect", "sync-read"),
            new EndpointDescriptor("/sim/control", "control", "cs2_set_simulation", "sync-set"),
            new EndpointDescriptor("/sim/run", "control", "cs2_run_simulation", "run-scheduled"),
            new EndpointDescriptor("/game/save", "cityWrite", "cs2_save_game", "save-started"),
            new EndpointDescriptor("/city/tiles", "read", "cs2_tiles_info", "sync-read"),
            new EndpointDescriptor("/districts", "read", "cs2_list_districts", "sync-read"),
            new EndpointDescriptor("/build/district", "cityWrite", "cs2_create_district", "tool-apply-ack"),
            new EndpointDescriptor("/district/policies", "read", "cs2_district_policies", "sync-read"),
            new EndpointDescriptor("/district/policies/set", "cityWrite", "cs2_set_district_policy", "end-frame-ack"),
            new EndpointDescriptor("/screenshot", "read", "cs2_screenshot", "end-frame-capture"),
            new EndpointDescriptor("/transit/stops", "read", "cs2_list_transit_stops", "sync-read"),
            new EndpointDescriptor("/transit/stops/place", "cityWrite", "cs2_place_transit_stop", "transit-finish-readback"),
            new EndpointDescriptor("/transit/lines", "read", "cs2_list_transit_lines", "sync-read"),
            new EndpointDescriptor("/transit/lines/create", "cityWrite", "cs2_create_transit_line", "transit-finish+rename-ack"),
            new EndpointDescriptor("/transit/lines/delete", "cityWrite", "cs2_delete_transit_line", "end-frame-ack"),
            new EndpointDescriptor("/city/tiles/list", "read", "cs2_list_map_tiles", "sync-read"),
            new EndpointDescriptor("/city/tiles/buy", "cityWrite", "cs2_buy_map_tiles", "purchase-readback+refresh-queued"),
            new EndpointDescriptor("/city/traffic", "read", "cs2_traffic", "sync-read"),
            new EndpointDescriptor("/city/vehicles", "read", "cs2_vehicles", "sync-read"),
            new EndpointDescriptor("/city/road-graph", "read", "cs2_road_graph", "sync-read"),
            new EndpointDescriptor("/build/road/replace", "cityWrite", "cs2_replace_road", "tool-finish-readback"),
            new EndpointDescriptor("/build/road/connect", "cityWrite", "cs2_connect_road", "tool-finish-readback"),
            new EndpointDescriptor("/build/place/shoreline", "cityWrite", "cs2_place_shoreline", "tool-apply-ack"),
            new EndpointDescriptor("/build/place/roadside", "cityWrite", "cs2_place_roadside", "tool-apply-ack"),
            new EndpointDescriptor("/city/buildings/near", "read", "cs2_buildings_near", "sync-read"),
            new EndpointDescriptor("/prefabs/info", "read", "cs2_prefab_info", "sync-read"),
            new EndpointDescriptor("/transit/lines/policies", "read", "cs2_line_policies", "sync-read"),
            new EndpointDescriptor("/transit/lines/policies/set", "cityWrite", "cs2_set_line_policy", "end-frame-ack"),
            new EndpointDescriptor("/transit/lines/vehicles", "read", "cs2_line_vehicles", "sync-read"),
            new EndpointDescriptor("/transit/lines/vehicles/set", "cityWrite", "cs2_set_line_vehicles", "sync-set"),
            new EndpointDescriptor("/capabilities", "read", "cs2_capabilities", "off-thread-snapshot"),
            new EndpointDescriptor("/v2/metrics/catalog", "read", "cs2_metric_catalog", "fixed-metric-catalog"),
            new EndpointDescriptor("/v2/metrics", "read", "cs2_observe", "typed-metric-read"),
            new EndpointDescriptor("/v2/observations", "read", "cs2_observe_batch", "bounded-observation-group", "POST"),
            new EndpointDescriptor("/v2/transit/status", "read", "cs2_transit_status", "selected-transit-read"),
            new EndpointDescriptor("/v2/transit/capabilities", "read", "cs2_transit_capabilities", "selected-transit-read"),
            new EndpointDescriptor("/v2/construction/inventory", "read", "cs2_construction_inventory", "explicit-entity-read"),
            new EndpointDescriptor("/v2/commands", "control", "cs2_submit_command", "guarded-admission-disabled", "POST"),
            new EndpointDescriptor("/v2/operations/{operationId}", "read", "cs2_operation", "managed-record"),
            new EndpointDescriptor("/v2/operations/{operationId}/cancel", "control", "cs2_cancel_operation", "cancel-request-ack", "POST"),
        });

        public static bool IsOperationEndpoint(EndpointDescriptor endpoint) => endpoint != null &&
            (endpoint.path == "/v2/commands" || endpoint.path == "/v2/operations/{operationId}" || endpoint.path == "/v2/operations/{operationId}/cancel");

        public static bool IsOperationRoute(string path) => IsOperationEndpoint(Resolve(path, out _));

        public static EndpointDescriptor Resolve(string path, out string operationId)
        {
            operationId = null;
            foreach (EndpointDescriptor endpoint in All)
            {
                if (!endpoint.path.Contains("{operationId}")) { if (endpoint.path == path) return endpoint; continue; }
                const string prefix = "/v2/operations/";
                string suffix = endpoint.path.EndsWith("/cancel", StringComparison.Ordinal) ? "/cancel" : "";
                if (!path.StartsWith(prefix, StringComparison.Ordinal) || !path.EndsWith(suffix, StringComparison.Ordinal)) continue;
                int length = path.Length - prefix.Length - suffix.Length;
                if (length != 36 || !Guid.TryParseExact(path.Substring(prefix.Length, length), "D", out Guid id)) continue;
                operationId = id.ToString("D"); return endpoint;
            }
            return null;
        }

        public static CapabilityManifest CreateOperationManifest(BuildIdentity identity, RuntimeEvidence runtime,
            NativeValidationMetadata validation, WorldSnapshot world)
        {
            if (identity == null || runtime == null || validation == null) throw new ArgumentNullException();
            return new CapabilityManifest(identity, runtime, validation, world);
        }

        public static CapabilityManifest CreateManifest(BuildIdentity identity, RuntimeEvidence runtime)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            return CreateValidationManifest(identity, runtime, new NativeValidationMetadata(null));
        }

        public static CapabilityManifest CreateValidationManifest(BuildIdentity identity, RuntimeEvidence runtime,
            NativeValidationMetadata validation)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (validation == null) throw new ArgumentNullException(nameof(validation));
            return new CapabilityManifest(identity, runtime, validation);
        }
    }
}
