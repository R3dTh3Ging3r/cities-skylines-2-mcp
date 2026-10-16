import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { BridgeClientError } from "./bridge-client.js";
import { bytes, HistoryError, HISTORY_LIMITS } from "./observation-history-contracts.js";

/** Measure the actual MCP result, including escaped text and error wrappers. */
export function historyResult(data: unknown): CallToolResult {
  const result: CallToolResult = {content: [{type: "text", text: JSON.stringify(data)}]};
  if (bytes(result) > HISTORY_LIMITS.resultBytes) {
    throw new HistoryError("RESULT_TOO_LARGE", "Result exceeds256KiB; request a smaller page or narrower sequence ranges");
  }
  return result;
}
export function historyError(error: unknown): CallToolResult {
  // Do not serialize arbitrary remote error text, Zod issue arrays or caller objects.
  const code = error instanceof HistoryError ? error.code : error instanceof BridgeClientError ?
    ["READ_CANCELLED", "WORLD_MISMATCH", "NOT_READY", "TRANSPORT_UNAVAILABLE", "PROTOCOL_ERROR"].includes(error.details.code) ? error.details.code : "READ_FAILED" : "INVALID_ARGUMENT";
  const message = error instanceof HistoryError ? error.message : error instanceof BridgeClientError ?
    "History admission read failed or was cancelled" : "Invalid history request or unavailable history";
  return {isError: true, content: [{type: "text", text: JSON.stringify({error: {code, message}})}]};
}
