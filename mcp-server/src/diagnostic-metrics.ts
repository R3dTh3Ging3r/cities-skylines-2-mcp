import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { BridgeClient } from "./bridge-client.js";
import { worldRefSchema } from "./construction-query.js";
import { metricScopeSchema, metricKeySchema, metricQuerySchema, metricFamilySchema, metricReadRequestSchema } from "./diagnostic-metric-contracts.js";
import { jsonResult, errorResult } from "./tool-results.js";
export * from "./diagnostic-metric-contracts.js";
const toolRequest = z.object({
    world: worldRefSchema, scope: metricScopeSchema, keys: z.array(metricKeySchema).min(1).max(64).optional(), queries: z.array(metricQuerySchema).min(1).max(64).optional()
}).strict().refine(r => (r.keys !== undefined) !== (r.queries !== undefined), "require exactly keys or queries");
export function registerDiagnosticMetricTools(registerTool: McpServer["registerTool"], bridge: BridgeClient): void {
    registerTool("cs2_metric_catalog", { title: "Read typed diagnostic metric catalog", description: "Sixteen source-defined city and selected-line structural metrics. Raw units and monthly budget periods; native parity remains notRun. No utility health or route load/length claim.", inputSchema: z.object({ family: metricFamilySchema.default("all") }).strict() }, async (request) => {
        try {
            return jsonResult(await bridge.metricCatalog(request.family));
        }
        catch (error) {
            return errorResult(error);
        }
    });
    registerTool("cs2_observe", { title: "Read selected typed diagnostic metrics", description: "Read explicit observed world and city or one full line EntityRef. 1–64 distinct known keys; source refresh times, native calendar conversion and parity unverified. Sequential source observations are never atomic. No history, scores or gameplay controls.", inputSchema: toolRequest }, async (request) => {
        try {
            return jsonResult(await bridge.readMetrics(metricReadRequestSchema.parse({ world: request.world, scope: request.scope, queries: request.queries ?? request.keys!.map(key => ({ key, parameters: {} })) })));
        }
        catch (error) {
            return errorResult(error);
        }
    });
}
