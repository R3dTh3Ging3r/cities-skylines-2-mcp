/** Encode for the bridge's percent decoder, which preserves literal '+'. */
export function encodeQuery(params: URLSearchParams): string {
  return params.toString().replace(/\+/g, "%20");
}

// Preserve the legacy error messages until the structured error contract lands.
export class BridgeError extends Error {}

export interface BridgeClient {
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

  async function getResponse(
    path: string,
    query?: URLSearchParams,
    timeoutMs = defaultTimeoutMs,
  ): Promise<Response> {
    const encoded = query ? encodeQuery(query) : "";
    const url = `${baseUrl}${path}${encoded ? `?${encoded}` : ""}`;
    try {
      return await fetchImpl(url, { signal: AbortSignal.timeout(timeoutMs) });
    } catch (err) {
      throw new BridgeError(
        `Cannot reach the CS2 bridge at ${baseUrl} (${(err as Error).message}). ` +
          "Make sure Cities: Skylines II is running and the CS2MCP mod is enabled.",
      );
    }
  }

  async function getJson<T = unknown>(
    path: string,
    query?: URLSearchParams,
    timeoutMs = defaultTimeoutMs,
  ): Promise<T> {
    const res = await getResponse(path, query, timeoutMs);
    const text = await res.text();
    let payload: unknown;
    try {
      payload = JSON.parse(text);
    } catch {
      payload = { raw: text };
    }
    if (!res.ok) {
      const message = (payload as { error?: string })?.error ?? `bridge returned HTTP ${res.status}`;
      throw new BridgeError(String(message));
    }
    return payload as T;
  }

  return { getJson, getResponse };
}
