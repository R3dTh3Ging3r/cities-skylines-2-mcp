import {constructionFailure,readMessages} from "./construction-read-diagnostics.js";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { BridgeClientError, type BridgeClient } from "./bridge-client.js";
import { boundedObjectInput } from "./bounded-schema.js";
import { constructionObjectSchema } from "./construction-query.js";

export function constructionResult(data:unknown):CallToolResult {
  const result:CallToolResult={content:[{type:"text",text:JSON.stringify(data)}]};
  if(Buffer.byteLength(JSON.stringify(result),"utf8")>262144)return constructionError("RESULT_TOO_LARGE",readMessages.mcpResultByteLimit);
  return result;
}
function constructionError(code:string,message:string):CallToolResult {
  return {isError:true,content:[{type:"text",text:JSON.stringify({error:constructionFailure({code,message,retryable:false,outcome:"notApplied"})})}]};
}
export function registerConstructionTools(registerTool:McpServer["registerTool"],bridge:BridgeClient):void {
  registerTool("cs2_construction_inventory",{
    title:"Inspect construction geometry and computed network profiles",
    description:"Partial read-only inventory of 1–32 explicit versioned entities in one observed world. Returns full network cubic curves and root building geometry bounds. " +
      "Optional geometryDiagnostics=sampledCubicV1 returns17 computed t/XYZ/grade/3D-curvature/radius samples per root curve; sampled extrema only, unknown physical units and engineering validity. " +
      "Optional single-root relationshipPage requires kind/offset/limit(1–32), returns raw references without target reads; independent live pages are not a stable snapshot. Inspect chosen refs separately. " +
      "Optional relationships=directDepth1 reads at most128 direct source slots and128 additional exact targets, without recursion. " +
      "Family compatibility, complete facility/platform/approach envelopes, connectors, costs and capacity remain unknown; native CON-01 notRun. Spatial scans, stable snapshots and family expansion unsupported.",
    inputSchema:boundedObjectInput(constructionObjectSchema),
  },async input=>{
    try{return constructionResult(await bridge.constructionInventory(input));}
    catch(error){return {isError:true,content:[{type:"text",text:JSON.stringify({error:constructionFailure(error instanceof BridgeClientError?error.details:undefined)})}]};}
  });
}
