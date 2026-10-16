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
  operationProtocol?: { version: 2; dispatchAvailable: false; scopedStatusAvailable: false;
    reason: string; legacyWrites: "unmigrated"; world: WorldRef | null; ready: boolean;
    registryCapacity: number; pendingCapacity: number; maxBodyBytes: number; };
  degradedReasons: string[];
}

export interface ExposureReport {
  registeredTools: string[];
  clientTools: string[] | null;
  capabilities: (Capability & { registered: boolean; clientExposure: "unknown" | "exposed" | "notExposed" })[];
}

export interface WorldRef { bootId: string; epoch: number; }
export interface EntityRef { world: WorldRef; index: number; version: number; }
export interface CommandRequest {
  operationId: string; world: WorldRef; leaseId: string; name: string;
  args: Record<string, unknown>; expectedFingerprint?: string;
}
export type OperationPhase = "queued" | "validating" | "applying" | "verifying" | "succeeded" |
  "failed" | "cancelled" | "interrupted" | "indeterminate";
export interface MutationReceipt {
  created: EntityRef[]; changed: EntityRef[]; reused: EntityRef[]; removed: EntityRef[];
  actualCost: number | null; costStatus: string; result: unknown; warnings: string[]; verification: string;
}
export interface OperationRecord {
  operationId: string; requestHash: string; world: WorldRef; name: string; phase: OperationPhase;
  error: BridgeErrorData | null; receipt: MutationReceipt | null; createdAtUtc: string; updatedAtUtc: string;
  cancellationRequested: boolean; retrySafe: boolean;
}
