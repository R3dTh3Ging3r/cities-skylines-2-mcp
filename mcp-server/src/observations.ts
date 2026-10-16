import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { BridgeClient } from "./bridge-client.js";
import { observationRequestSchema } from "./observation-contracts.js";
import { jsonResult, errorResult } from "./tool-results.js";
export function registerObservationTools(registerTool: McpServer["registerTool"], bridge: BridgeClient): void {
    registerTool("cs2_observe_batch", { title: "Sample bounded city observations", description: "Read 1–64 uniquely named items from the fixed state/city.overview/city.services/city.labor/city.budget/construction.inventory catalog. Sequential samples retain original world and per-item frame/freshness warnings; never atomic. Cached aggregate refresh time and native parity remain unverified. No writers, arbitrary URLs, leases or dependencies.", inputSchema: observationRequestSchema }, async (request, extra) => { try {
        return jsonResult(await bridge.observe(request, { signal: extra.signal }));
    }
    catch (error) {
        return errorResult(error);
    } });
}
