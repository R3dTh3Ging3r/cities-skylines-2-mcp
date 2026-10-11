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
  reason?: string | null;
}

export interface EndpointDescriptor {
  path: string;
  legacyHttpVerbs: string[];
  access: "read" | "cityWrite" | "uiWrite" | "nativePreview" | "control";
  capability: string;
  completionMechanism: string;
}

export interface CapabilityManifest {
  protocolVersion: number;
  schemaVersion: number;
  identity: {
    assemblyVersion: string | null;
    modVersion: string;
    buildIdentity?: string | null;
    buildIdentityReason?: string | null;
    sourceRevision?: string | null;
    sourceRevisionReason?: string | null;
    gameVersion: string | null;
    gameVersionReason: string | null;
  };
  runtime: {
    observedAtUtc: string | null;
    ageMilliseconds: number | null;
    gameMode: string | null;
    isLoading: boolean | null;
    cityLoaded: boolean | null;
    reason: string | null;
  };
  endpoints: EndpointDescriptor[];
  capabilities: Capability[];
  nativeSupport: Capability[];
  degradedReasons: string[];
}

export interface ExposureReport {
  registeredTools: string[];
  clientTools: string[] | null;
  capabilities: (Capability & { registered: boolean; clientExposure: "unknown" | "exposed" | "notExposed" })[];
}
