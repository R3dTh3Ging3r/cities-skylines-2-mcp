import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { BridgeClientError } from "./bridge-client.js";
import type { BridgeErrorData } from "./contracts.js";

export function jsonResult(payload: unknown): CallToolResult {
  return { content: [{ type: "text", text: JSON.stringify(payload, null, 2) }] };
}

export function errorResult(error: unknown): CallToolResult {
  const message = error instanceof Error ? error.message : String(error);
  const errorData: BridgeErrorData = error instanceof BridgeClientError ? error.details : {
    code: "OUTCOME_UNKNOWN", message, retryable: false, outcome: "unknown",
  };
  const payload = { error: message, errorData };
  return { ...jsonResult(payload), structuredContent: payload, isError: true };
}
