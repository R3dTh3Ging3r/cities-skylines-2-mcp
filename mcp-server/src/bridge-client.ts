import { constructionQuery, type ConstructionRequest } from "./construction-query.js";
import type { Observation, ConstructionSnapshot, BridgeErrorData, CommandRequest, OperationRecord } from "./contracts.js";

/** Encode for the bridge's percent decoder, which preserves literal '+'. */
export function encodeQuery(params: URLSearchParams): string {
  return params.toString().replace(/\+/g, "%20");
}

/** @deprecated Catch BridgeClientError to read typed details; legacy construction is retained. */
export class BridgeError extends Error {}

export class BridgeClientError extends BridgeError {
  constructor(public readonly details: BridgeErrorData) {
    super(details.message);
    this.name = "BridgeClientError";
  }
}

/** Status-only fallback: this never classifies native reasons or proves no effects. */
function codeFromStatus(status: number): string {
  switch (status) {
    case 400: return "INVALID_ARGUMENT";
    case 403: return "LOCKED";
    case 404: case 405: case 501: return "UNSUPPORTED";
    case 409: return "BRIDGE_BUSY";
    case 422: return "NATIVE_REJECTED";
    case 503: return "NOT_READY";
    default: return "PROTOCOL_ERROR";
  }
}

function httpError(status: number, text: string): BridgeClientError {
  let payload: { error?: unknown; errorData?: unknown } | null = null;
  try { payload = JSON.parse(text); } catch { /* keep a bounded legacy diagnostic */ }
  const legacyMessage = typeof payload?.error === "string" ? payload.error : null;
  const details = payload?.errorData;
  if (details && typeof details === "object") {
    const data = details as Record<string, unknown>;
    if (typeof data.code === "string" && typeof data.message === "string" &&
        typeof data.retryable === "boolean" &&
        (data.outcome === "notApplied" || data.outcome === "unknown" || data.outcome === "partial")) {
      return new BridgeClientError({ code: data.code, message: legacyMessage ?? data.message,
        retryable: data.retryable, outcome: data.outcome, httpStatus: status,
        ...(Array.isArray(data.nativeErrors) && data.nativeErrors.every((item) => typeof item === "string")
          ? { nativeErrors: data.nativeErrors as string[] } : {}),
        ...(typeof data.operationId === "string" ? { operationId: data.operationId } : {}),
      });
    }
  }
  const excerpt = text.slice(0, 512).replace(/\s+/g, " ").trim();
  return new BridgeClientError({ code: codeFromStatus(status),
    message: legacyMessage ?? `bridge returned HTTP ${status}${excerpt ? `: ${excerpt}` : ""}`,
    retryable: false, outcome: "unknown", httpStatus: status,
  });
}

export interface BridgeClient {
  constructionInventory(request:ConstructionRequest):Promise<Observation<ConstructionSnapshot>>;
  submit(command: CommandRequest): Promise<OperationRecord>;
  operation(operationId: string): Promise<OperationRecord>;
  cancelOperation(operationId: string): Promise<OperationRecord>;
  getJson<T = unknown>(path: string, query?: URLSearchParams, timeoutMs?: number): Promise<T>;
  getResponse(path: string, query?: URLSearchParams, timeoutMs?: number): Promise<Response>;
}

export function createBridgeClient(options: {
  baseUrl: string;
  timeoutMs?: number;
  fetchImpl?: typeof fetch;
}): BridgeClient {
  const baseUrl = options.baseUrl.replace(/\/+$/, "");
  const defaultTimeoutMs = options.timeoutMs ?? 12_000;
  const fetchImpl = options.fetchImpl ?? fetch;

  function transportError(error: unknown, httpStatus?: number): BridgeClientError {
    const source = error instanceof Error ? error : new Error(String(error));
    const timedOut = source.name === "AbortError" || source.name === "TimeoutError";
    return new BridgeClientError({ code: timedOut ? "OUTCOME_UNKNOWN" : "TRANSPORT_UNAVAILABLE",
      message: `Cannot reach the CS2 bridge at ${baseUrl} (${source.message}). ` +
        "Make sure Cities: Skylines II is running and the CS2MCP mod is enabled.",
      retryable: false, outcome: "unknown", ...(httpStatus !== undefined ? { httpStatus } : {}),
    });
  }

  async function readText(response: Response): Promise<string> {
    try { return await response.text(); }
    catch (error) { throw transportError(error, response.status); }
  }

  async function getResponse(
    path: string,
    query?: URLSearchParams,
    timeoutMs = defaultTimeoutMs,
  ): Promise<Response> {
    const encoded = query ? encodeQuery(query) : "";
    const url = `${baseUrl}${path}${encoded ? `?${encoded}` : ""}`;
    let response: Response;
    try { response = await fetchImpl(url, { signal: AbortSignal.timeout(timeoutMs) }); }
    catch (error) { throw transportError(error); }
    if (!response.ok) throw httpError(response.status, await readText(response));
    return response;
  }

  async function getJson<T = unknown>(
    path: string,
    query?: URLSearchParams,
    timeoutMs = defaultTimeoutMs,
  ): Promise<T> {
    const res = await getResponse(path, query, timeoutMs);
    const text = await readText(res);
    let payload: unknown;
    try {
      payload = JSON.parse(text);
    } catch {
      throw new BridgeClientError({ code: "PROTOCOL_ERROR",
        message: `bridge returned invalid JSON at HTTP ${res.status}: ${text.slice(0, 512)}`,
        retryable: false, outcome: "unknown", httpStatus: res.status,
      });
    }
    return payload as T;
  }

  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  async function operationRequest(method: "GET" | "POST", path: string, operationId: string,
    command?: CommandRequest): Promise<OperationRecord> {
    // Constrain the new protocol without silently changing legacy URL configuration behavior.
    const address = new URL(baseUrl);
    if (address.protocol !== "http:" || !["127.0.0.1", "localhost", "[::1]"].includes(address.hostname) ||
        address.username || address.password || address.pathname !== "/" || address.search || address.hash || !uuid.test(operationId)) {
      throw new BridgeClientError({code:"INVALID_ARGUMENT",message:"v2 requires a loopback HTTP base URL and UUID operationId",
        retryable:false,outcome:"notApplied",operationId});
    }
    // Serialize once before the await; there is no automatic retry or generated replacement ID.
    const body = command === undefined ? "" : JSON.stringify(command);
    if (Buffer.byteLength(body, "utf8") > 1_048_576) throw new BridgeClientError({code:"INVALID_ARGUMENT",
      message:"v2 command exceeds 1 MiB",retryable:false,outcome:"notApplied",operationId});
    try {
      let response: Response;
      try { response = await fetchImpl(`${baseUrl}${path}`, {method, signal:AbortSignal.timeout(defaultTimeoutMs),
        ...(method === "POST" ? {body, headers:{"content-type":"application/json"}} : {})}); }
      catch (error) { throw transportError(error); }
      const text = await readText(response);
      if (!response.ok) throw httpError(response.status,text);
      let record: OperationRecord;
      try { record = JSON.parse(text) as OperationRecord; }
      catch { throw new BridgeClientError({code:"PROTOCOL_ERROR",message:"bridge returned invalid operation JSON",
        retryable:false,outcome:"unknown",httpStatus:response.status}); }
      if (!record || record.operationId?.toLowerCase() !== operationId.toLowerCase() ||
          !["queued","validating","applying","verifying","succeeded","failed","cancelled","interrupted","indeterminate"].includes(record.phase)) {
        throw new BridgeClientError({code:"PROTOCOL_ERROR",message:"bridge returned a mismatched/invalid operation record",
          retryable:false,outcome:"unknown",httpStatus:response.status});
      }
      return record;
    } catch (error) {
      if (error instanceof BridgeClientError) throw new BridgeClientError({...error.details,operationId,retryable:false});
      throw new BridgeClientError({...transportError(error).details,operationId,retryable:false});
    }
  }
  return { getJson, getResponse,
    constructionInventory: async request => getJson<Observation<ConstructionSnapshot>>("/v2/construction/inventory",constructionQuery(request)),
    submit: command => operationRequest("POST","/v2/commands",command.operationId,command),
    operation: operationId => operationRequest("GET",`/v2/operations/${encodeURIComponent(operationId)}`,operationId),
    cancelOperation: operationId => operationRequest("POST",`/v2/operations/${encodeURIComponent(operationId)}/cancel`,operationId),
  };
}
