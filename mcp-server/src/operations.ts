import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import type { BridgeClient } from "./bridge-client.js";
import { errorResult, jsonResult } from "./tool-results.js";

/** Explicit v2 tools only. Legacy writers retain their existing route/semantics until O5 migration. */
export function registerOperationTools(registerTool: McpServer["registerTool"], bridge: BridgeClient): void {
  registerTool("cs2_submit_command", {
    title:"Submit a world-scoped operation",
    description:"Submit one fixed catalog command with a caller-retained UUID, observed world and lease. " +
      "Native dispatch remains disabled pending completion and lease guards. Unknown outcomes require status reconciliation; never blindly retry with a new ID.",
    inputSchema: {
      operationId:z.string().uuid(), world:z.object({bootId:z.string().min(1),epoch:z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER)}).strict(),
      leaseId:z.string().min(1).max(128),name:z.string().min(1).max(128),args:z.record(z.unknown()),expectedFingerprint:z.string().optional(),
    },
  },async command => {try {return jsonResult(await bridge.submit(command));} catch(error) {return errorResult(error);} });
  registerTool("cs2_operation", {
    title:"Inspect retained operation",description:"Query the original operation UUID after response loss. Status authority remains disabled until lease guards exist.",
    inputSchema:{operationId:z.string().uuid()},
  },async ({operationId}) => {try {return jsonResult(await bridge.operation(operationId));} catch(error) {return errorResult(error);} });
  registerTool("cs2_cancel_operation", {
    title:"Request operation cancellation",description:"Request cancellation for the original UUID. Cancellation after apply is not proof of no effects. Disabled pending scoped authority.",
    inputSchema:{operationId:z.string().uuid()},
  },async ({operationId}) => {try {return jsonResult(await bridge.cancelOperation(operationId));} catch(error) {return errorResult(error);} });
}
