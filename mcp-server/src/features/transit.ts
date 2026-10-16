import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { BridgeClient } from "../bridge-client.js";
import { transitRequestSchema } from "./transit-contracts.js";
import { jsonResult, errorResult } from "../tool-results.js";
export function registerTransitTools(registerTool: McpServer["registerTool"], bridge: BridgeClient): void {
    registerTool("cs2_transit_status", { title: "Inspect a selected transit line", description: "Read one caller-observed full EntityRef. At most128 combined waypoint/segment entries; larger buffers return explicit incomplete evidence. XYZ, immediate stop ownership and structural faults only; native route validity, raw distance units, loads and dispatch remain unverified. No discovery or mutations.", inputSchema: transitRequestSchema }, async (input) => {
        try {
            return jsonResult(await bridge.transitStatus(input));
        }
        catch (error) {
            return errorResult(error);
        }
    });
    registerTool("cs2_transit_capabilities", { title: "Read selected transit support", description: "Partial exact-line diagnostics support and bounds. Native transit fixture notRun; no discovery/load/native validity claim.", inputSchema: z.object({}).strict() }, async () => {
        try {
            return jsonResult(await bridge.transitCapabilities());
        }
        catch (error) {
            return errorResult(error);
        }
    });
}
