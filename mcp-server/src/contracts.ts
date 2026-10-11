/** Additive wire contracts; legacy success payloads keep their existing fields. */
export interface BridgeErrorData {
  code: string;
  message: string;
  retryable: boolean;
  outcome: "notApplied" | "unknown" | "partial";
  httpStatus?: number;
  nativeErrors?: string[];
  operationId?: string;
}

export interface Capability {
  name: string;
  support: "supported" | "degraded" | "unsupported" | "unverified";
  reason?: string;
}
