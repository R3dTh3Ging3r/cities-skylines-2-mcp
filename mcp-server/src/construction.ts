import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { BridgeClientError, type BridgeClient } from "./bridge-client.js";
import { boundedObjectInput } from "./bounded-schema.js";
import { constructionObjectSchema } from "./construction-query.js";

export function constructionResult(data:unknown):CallToolResult {
  const result:CallToolResult={content:[{type:"text",text:JSON.stringify(data)}]};
  if(Buffer.byteLength(JSON.stringify(result),"utf8")>262144)return constructionError("RESULT_TOO_LARGE","Inventory MCP result exceeds256KiB; select fewer roots or omit expansion");
  return result;
}
function constructionError(code:string,message:string):CallToolResult {
  return {isError:true,content:[{type:"text",text:JSON.stringify({error:{code,message}})}]};
}
export function registerConstructionTools(registerTool:McpServer["registerTool"],bridge:BridgeClient):void {
  registerTool("cs2_construction_inventory",{
    title:"Inspect construction geometry and computed network profiles",
    description:"Partial read-only inventory of 1–32 explicit versioned entities in one observed world. Returns full network cubic curves and root building geometry bounds. " +
      "Optional geometryDiagnostics=sampledCubicV1 returns17 computed t/XYZ/grade/3D-curvature/radius samples per root curve; sampled extrema only, unknown physical units and engineering validity. " +
      "Optional relationships=directDepth1 reads at most128 direct source slots and128 additional exact targets, without recursion. " +
      "Family compatibility, complete facility/platform/approach envelopes, connectors, costs and capacity remain unknown; native CON-01 notRun. Spatial scans, cursors and family expansion unsupported.",
    inputSchema:boundedObjectInput(constructionObjectSchema),
  },async input=>{
    try{return constructionResult(await bridge.constructionInventory(input));}
    catch(error){return constructionError(error instanceof BridgeClientError?error.details.code.slice(0,128):"INVALID_ARGUMENT",
      "Invalid inventory request or unavailable bounded response");}
  });
}
