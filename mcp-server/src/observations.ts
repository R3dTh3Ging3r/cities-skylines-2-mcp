import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { BridgeClientError, type BridgeClient } from "./bridge-client.js";
import type { BridgeErrorData, ObservationBatchResult } from "./contracts.js";
import { constructionFailure } from "./construction-read-diagnostics.js";
import { observationRequestObjectSchema } from "./observation-contracts.js";
import { boundedObjectInput } from "./bounded-schema.js";
import { jsonResult, errorResult } from "./tool-results.js";

/** Outer group errors have no attributable item. Preserve only finite known diagnostics. */
function inventoryGroupError(error: unknown): CallToolResult {
    const failure = constructionFailure(error instanceof BridgeClientError ? error.details : undefined);
    if (failure.reason === "unknownLimit" || failure.reason === "unknownCause") {
        failure.message = "Observation group unavailable; failure cause is unknown. Inspect inventory roots individually, use raw relationshipPage windows and read chosen targets separately. No retry was performed.";
    }
    const payload = { error: failure };
    const result: CallToolResult = { ...jsonResult(payload), structuredContent: payload, isError: true };
    // Measure the complete error envelope, including repeated structured/text fields.
    // This cap applies only to failures; successful groups retain their4MiB transport.
    if (Buffer.byteLength(JSON.stringify(result), "utf8") > 262144) {
        return { isError: true, content: [{ type: "text", text: JSON.stringify({ error: {
            code: "PROTOCOL_ERROR", reason: "unknownCause", message: "Bounded observation error unavailable; inspect inventory roots separately."
        } }) }] };
    }
    return result;
}

function inventoryItemError(error: BridgeErrorData): BridgeErrorData {
    const failure = constructionFailure(error);
    return {
        code: failure.code,
        message: failure.reason === "unknownLimit" || failure.reason === "unknownCause"
            ? `${failure.reason}: ${failure.message}` : failure.message,
        retryable: false,
        outcome: error.outcome,
    };
}

export function registerObservationTools(registerTool: McpServer["registerTool"], bridge: BridgeClient): void {
    registerTool("cs2_observe_batch", {
        title: "Sample bounded city observations",
        description: "Read 1–64 uniquely named items from the fixed state/city.overview/city.services/city.labor/" +
            "city.budget/construction.inventory/transit.selectedLine/metrics.read catalog. Sequential samples retain " +
            "original world and per-item frame/freshness warnings; never atomic. Cached aggregate refresh time and " +
            "native parity remain unverified. No writers, arbitrary URLs, leases or dependencies.",
        inputSchema: boundedObjectInput(observationRequestObjectSchema),
    }, async (request, extra) => {
        try {
            // observe applies the authoritative refined admission schema before HTTP.
            const result = await bridge.observe(request, { signal: extra.signal });
            // Keep order and all successful observations/non-inventory rows untouched.
            // A raw error member may contain private legacy diagnostics or unbounded IDs.
            const safe: ObservationBatchResult = { ...result, results: result.results.map((row, index) =>
                request.items[index].name === "construction.inventory" && row.error
                    ? { id: row.id, error: inventoryItemError(row.error) } : row) };
            return jsonResult(safe);
        } catch (error) {
            return request.items.some(item => item.name === "construction.inventory")
                ? inventoryGroupError(error) : errorResult(error);
        }
    });
}
