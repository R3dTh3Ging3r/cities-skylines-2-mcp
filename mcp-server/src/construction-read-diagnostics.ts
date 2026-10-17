import type {BridgeErrorData} from "./contracts.js";
/** Exact producer diagnostics only; unknown server prose is never echoed or classified. */
export const readMessages={
  relationshipSlotLimit:"relationshipSlotLimit: direct relationships exceed128 slots; use a single-root relationshipPage",
  inventoryByteLimit:"inventoryByteLimit: inventory exceeds256KiB; split roots or read raw relationship pages and chosen targets separately",
  observationItemByteLimit:"observationItemByteLimit: observation exceeds256KiB; reduce the requested read",
  observationGroupByteBudget:"observationGroupByteBudget: group exceeds its4MiB reserved budget; request fewer items",
  mcpResultByteLimit:"mcpResultByteLimit: escaped inventory MCP result exceeds256KiB; use fewer roots or smaller raw relationship pages",
  transportResponseByteLimit:"transportResponseByteLimit: received bounded HTTP data exceeds256KiB; reduce the requested read",
  relationshipOffsetOutOfRange:"relationshipOffsetOutOfRange: request a fresh selected-source page from offset0",
  relationshipSourceChanged:"relationshipSourceChanged: selected source changed during the page; request a fresh page manually",
} as const;
export function constructionFailure(error?:BridgeErrorData){
  let reason:string|undefined;
  if(error) {
    for(const [key,message] of Object.entries(readMessages)) {
      const code=key==="relationshipOffsetOutOfRange"?"INVALID_ARGUMENT":key==="relationshipSourceChanged"?"RELATIONSHIP_SOURCE_CHANGED":"RESULT_TOO_LARGE";
      if(error.code===code&&error.message===message)reason=key;
    }
    // Exact published older producers are also known; no substring guessing.
    if(error.code==="RESULT_TOO_LARGE"&&error.message==="direct relationships exceed128 slots; select fewer roots or omit expansion")reason="relationshipSlotLimit";
    if(error.code==="RESULT_TOO_LARGE"&&error.message==="inventory result exceeds256KiB; select fewer roots or omit expansion")reason="inventoryByteLimit";
  }
  if(reason)return {code:error!.code,reason,message:readMessages[reason as keyof typeof readMessages]};
  const knownCodes=new Set(["INVALID_ARGUMENT","RESULT_TOO_LARGE","RELATIONSHIP_SOURCE_CHANGED","WORLD_MISMATCH","NOT_READY","UNSUPPORTED","PROTOCOL_ERROR","TRANSPORT_UNAVAILABLE","READ_CANCELLED","BRIDGE_BUSY","NATIVE_REJECTED"]);
  return {code:error?(knownCodes.has(error.code)?error.code:"PROTOCOL_ERROR"):"INVALID_ARGUMENT",reason:error?.code==="RESULT_TOO_LARGE"?"unknownLimit":"unknownCause",
    message:"Bounded inventory unavailable; inspect one root, use raw relationshipPage windows and read chosen targets separately. No retry was performed."};
}
