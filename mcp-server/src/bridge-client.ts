import {metricFamilySchema,metricCatalogSchema,metricReadRequestSchema,metricQuery,parseMetricObservation,type MetricDefinition,type MetricReadRequest} from "./diagnostic-metric-contracts.js";
import {transitRequestSchema,transitQuery,parseTransitObservation,transitCapabilitiesSchema,type TransitRequest,type TransitPage,type TransitCapabilities} from "./features/transit-contracts.js";
import {observationRequestSchema,parseObservationResult} from "./observation-contracts.js";
import { constructionQuery, type ConstructionRequest } from "./construction-query.js";
import type { ObservationBatchRequest, ObservationBatchResult, Observation, ConstructionSnapshot, BridgeErrorData, CommandRequest, OperationRecord } from "./contracts.js";

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
  metricCatalog(family?:"all"|"city"|"labor"|"budget"|"transit"):Promise<MetricDefinition[]>;
  readMetrics(request:MetricReadRequest):Promise<ReturnType<typeof parseMetricObservation>>;
  transitStatus(request:TransitRequest):Promise<Observation<TransitPage>>;
  transitCapabilities():Promise<TransitCapabilities>;
  observe(request:ObservationBatchRequest,options?:{signal?:AbortSignal}):Promise<ObservationBatchResult>;
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

  async function observe(input: ObservationBatchRequest, options?: {
      signal?: AbortSignal;
  }): Promise<ObservationBatchResult> {
      let request: ObservationBatchRequest, body: string;
      try {
          request = observationRequestSchema.parse(input);
          body = JSON.stringify(request);
          const address = new URL(baseUrl);
          if (address.protocol !== "http:" || !["127.0.0.1", "localhost", "[::1]"].includes(address.hostname) || address.username || address.password || address.pathname !== "/" || address.search || address.hash || Buffer.byteLength(body, "utf8") > 1048576)
              throw new Error("invalid address/size");
      }
      catch {
          throw new BridgeClientError({ code: "INVALID_ARGUMENT", message: "invalid bounded observation request or loopback HTTP address", retryable: false, outcome: "notApplied" });
      }
      const controller = new AbortController(), signal = options?.signal;
      const onAbort = () => controller.abort();
      const timer = setTimeout(() => controller.abort(), defaultTimeoutMs);
      if (signal?.aborted)
          controller.abort();
      else
          signal?.addEventListener("abort", onAbort, { once: true });
      try {
          if (controller.signal.aborted)
              throw new DOMException("observation aborted", "AbortError");
          const response = await fetchImpl(`${baseUrl}/v2/observations`, { method: "POST", body, headers: { "content-type": "application/json" }, signal: controller.signal });
          const reader = response.body?.getReader();
          const bytes = new Uint8Array(4_194_304);
          let length = 0;
          if (reader)
              try {
                  while (true) {
                      const part = await reader.read();
                      if (part.done)
                          break;
                      length += part.value.byteLength;
                      if (length > 4194304) {
                          await reader.cancel();
                          throw new BridgeClientError({ code: "PROTOCOL_ERROR", message: "observation response exceeds 4 MiB", retryable: false, outcome: "notApplied" });
                      }
                      bytes.set(part.value, length - part.value.byteLength);
                  }
              }
              finally {
                  reader.releaseLock();
              }
          let text: string;
          try {
              text = new TextDecoder("utf-8", { fatal: true }).decode(bytes.subarray(0, length));
          }
          catch {
              throw new BridgeClientError({ code: "PROTOCOL_ERROR", message: "invalid observation response UTF8", retryable: false, outcome: "notApplied" });
          }
          if (!response.ok)
              throw httpError(response.status, text);
          try {
              return parseObservationResult(JSON.parse(text), request);
          }
          catch {
              throw new BridgeClientError({ code: "PROTOCOL_ERROR", message: "invalid observation result identity/union/frame evidence", retryable: false, outcome: "notApplied" });
          }
      }
      catch (error) {
          if (controller.signal.aborted)
              throw new BridgeClientError({ code: signal?.aborted ? "READ_CANCELLED" : "TRANSPORT_UNAVAILABLE", message: signal?.aborted ? "observation cancelled by caller" : "observation timed out", retryable: false, outcome: "notApplied" });
          if (error instanceof BridgeClientError)
              throw error;
          throw new BridgeClientError({ ...transportError(error).details, outcome: "notApplied" });
      }
      finally {
          clearTimeout(timer);
          signal?.removeEventListener("abort", onAbort);
      }
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
  // Read all HTTP statuses under the new protocol cap; legacy getResponse is unchanged.
  async function boundedTransit(path: string, query?: URLSearchParams): Promise<unknown> {
    const address = new URL(baseUrl);
    if (address.protocol !== "http:" || !["127.0.0.1", "localhost", "[::1]"].includes(address.hostname) ||
        address.username || address.password || address.pathname !== "/" || address.search || address.hash) {
      throw new BridgeClientError({code: "INVALID_ARGUMENT", message: "selected transit requires loopback HTTP", retryable: false, outcome: "notApplied"});
    }
    const encoded = query ? encodeQuery(query) : "";
    let response: Response;
    try {
      response = await fetchImpl(`${baseUrl}${path}${encoded ? `?${encoded}` : ""}`, {signal: AbortSignal.timeout(defaultTimeoutMs)});
    } catch (error) {
      throw transportError(error);
    }
    const reader = response.body?.getReader();
    if (!reader) throw new Error("missing transit response stream");
    const chunks: Uint8Array[] = [];
    let length = 0;
    try {
      while (true) {
        const part = await reader.read();
        if (part.done) break;
        length += part.value.length;
        if (length > 262144) {
          await reader.cancel();
          throw new Error("transit response exceeds256KiB");
        }
        chunks.push(part.value);
      }
      const text = new TextDecoder("utf-8", {fatal: true}).decode(Buffer.concat(chunks, length));
      if (!response.ok) throw httpError(response.status, text);
      return JSON.parse(text);
    } finally {
      reader.releaseLock();
    }
  }
  async function transitStatus(input: TransitRequest): Promise<Observation<TransitPage>> {
    const request = transitRequestSchema.parse(input);
    try {
      return parseTransitObservation(await boundedTransit("/v2/transit/status", transitQuery(request)), request);
    } catch (error) {
      if (error instanceof BridgeClientError) throw error;
      throw new BridgeClientError({code: "PROTOCOL_ERROR", message: "invalid bounded selected transit response", retryable: false, outcome: "notApplied"});
    }
  }
  async function readMetrics(input:MetricReadRequest){
    let request:MetricReadRequest,query:URLSearchParams;
    try{request=metricReadRequestSchema.parse(input);query=metricQuery(request);}catch{throw new BridgeClientError({code:"INVALID_ARGUMENT",message:"invalid bounded metric request",retryable:false,outcome:"notApplied"});}
    try{return parseMetricObservation(await boundedTransit("/v2/metrics",query),request);}catch(error){if(error instanceof BridgeClientError)throw error;throw new BridgeClientError({code:"PROTOCOL_ERROR",message:"invalid bounded metric observation",retryable:false,outcome:"notApplied"});}
  }
  async function metricCatalog(family:"all"|"city"|"labor"|"budget"|"transit"="all"){
    metricFamilySchema.parse(family);
    try{
      const definitions=metricCatalogSchema.parse(await boundedTransit("/v2/metrics/catalog",new URLSearchParams({family})));
      if(family!=="all"&&definitions.some(d=>!d.key.startsWith(family+".")))throw new Error("catalog family mismatch");
      if(family==="all"&&definitions.length!==16)throw new Error("incomplete metric catalog");
      return definitions;
    }catch(error){if(error instanceof BridgeClientError)throw error;throw new BridgeClientError({code:"PROTOCOL_ERROR",message:"invalid bounded metric catalog",retryable:false,outcome:"notApplied"});}
  }
  return { getJson, getResponse, observe, transitStatus, readMetrics,metricCatalog,
    transitCapabilities: async () => transitCapabilitiesSchema.parse(await boundedTransit("/v2/transit/capabilities")),
    constructionInventory: async request => getJson<Observation<ConstructionSnapshot>>("/v2/construction/inventory",constructionQuery(request)),
    submit: command => operationRequest("POST","/v2/commands",command.operationId,command),
    operation: operationId => operationRequest("GET",`/v2/operations/${encodeURIComponent(operationId)}`,operationId),
    cancelOperation: operationId => operationRequest("POST",`/v2/operations/${encodeURIComponent(operationId)}/cancel`,operationId),
  };
}
