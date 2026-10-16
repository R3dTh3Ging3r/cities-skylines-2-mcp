import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { BridgeClientError, type BridgeClient } from "./bridge-client.js";
import { worldRefSchema } from "./construction-query.js";
import { metricScopeSchema, metricKeySchema, metricQuerySchema, metricFamilySchema, metricReadRequestSchema } from "./diagnostic-metric-contracts.js";
import { jsonResult, errorResult } from "./tool-results.js";
import { boundedObjectInput } from "./bounded-schema.js";
export * from "./diagnostic-metric-contracts.js";

const toolRequestObject = z.object({
    world: worldRefSchema,
    scope: metricScopeSchema,
    keys: z.array(metricKeySchema).min(1).max(64).optional(),
    queries: z.array(metricQuerySchema).min(1).max(64).optional(),
}).strict();
const toolRequest = toolRequestObject.refine(
    request => (request.keys !== undefined) !== (request.queries !== undefined),
    "require exactly keys or queries",
);

export function registerDiagnosticMetricTools(registerTool: McpServer["registerTool"], bridge: BridgeClient): void {
    registerTool("cs2_metric_catalog", {
        title: "Read typed diagnostic metric catalog",
        description: "Twenty-six source-defined city and selected-line structural metrics, including total/free " +
            "workplace slots by native education tier. Native parity remains notRun. Vacancies do not measure " +
            "worker supply or prove a labor bottleneck. No utility health or route load/length claim.",
        inputSchema: boundedObjectInput(z.object({family: metricFamilySchema.default("all")}).strict()),
    }, async request => {
        try {
            return jsonResult(await bridge.metricCatalog(request.family));
        } catch (error) {
            return errorResult(error);
        }
    });

    registerTool("cs2_observe", {
        title: "Read selected typed diagnostic metrics",
        description: "Read explicit observed world and city or one full line EntityRef. Require exactly keys or " +
            "queries, with 1–64 distinct known keys; source refresh times, native calendar conversion and parity " +
            "unverified. Sequential source observations are never atomic. No history, scores or gameplay controls.",
        inputSchema: boundedObjectInput(toolRequestObject),
    }, async input => {
        try {
            // Preserve the original keys XOR queries refinement after exposing
            // the object facade. A fixed failure avoids echoing input diagnostics.
            const parsed = toolRequest.safeParse(input);
            if (!parsed.success) {
                throw new BridgeClientError({code: "INVALID_ARGUMENT", message: "require exactly metric keys or queries",
                    retryable: false, outcome: "notApplied"});
            }
            const request = parsed.data;
            const normalized = metricReadRequestSchema.parse({
                world: request.world,
                scope: request.scope,
                queries: request.queries ?? request.keys!.map(key => ({key, parameters: {}})),
            });
            return jsonResult(await bridge.readMetrics(normalized));
        } catch (error) {
            return errorResult(error);
        }
    });
}
