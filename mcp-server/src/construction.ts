import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { BridgeClient } from "./bridge-client.js";
import { constructionInputSchema } from "./construction-query.js";
import { errorResult, jsonResult } from "./tool-results.js";

export function registerConstructionTools(registerTool:McpServer["registerTool"],bridge:BridgeClient):void {
  registerTool("cs2_construction_inventory",{
    title:"Inspect explicit construction geometry",
    description:"Partial read-only inventory of 1–32 explicit versioned entities in one observed world. Returns full network cubic curves and root building geometry bounds only. " +
      "Family compatibility, complete facility/platform/approach envelopes, connectors, costs and capacity remain unknown; native CON-01 notRun. Spatial scans, cursors and family expansion unsupported.",
    inputSchema:constructionInputSchema,
  },async input=>{try{return jsonResult(await bridge.constructionInventory(input));}catch(error){return errorResult(error);}});
}
