import type {TransitRequest} from "./features/transit-contracts.js";
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
  observationProtocol?: {support:"partial";names:string[];maxItems:number;activeGroups:number;sharedPendingItems:number;sharedReadsPerUpdate:number;additionalWorkMilliseconds:number;maxItemBytes:number;maxGroupBytes:number;nativePerformance:"notRun"};
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

export interface Observation<T> {
  data:T; world:WorldRef|null; sampledAtUtc:string; startFrame:number|null; endFrame:number|null;
  freshness:"current"|"warming"|"unavailable"; warnings:string[];
}
export interface Point3 { x:number; y:number; z:number; }
export interface CubicCurve { a:Point3; b:Point3; c:Point3; d:Point3; }
export interface OrientedBounds {
  center:Point3; halfExtents:Point3; axisX:Point3; axisY:Point3; axisZ:Point3; scope:"rootGeometryOnly";
}
export interface NetworkGeometry {
  curve:CubicCurve; start:EntityRef; end:EntityRef; length:number; direction:"startToEndNotLaneDirection";
  coordinates:"worldXYZ"; width:number|null; widthReason:string|null; requiredLayers:string|null; connectLayers:string|null; layersReason:string|null;
  elevationMode:null; elevationModeReason:string; laneDirections:null; laneDirectionsReason:string;
}
export interface ConstructionEntity {
  entity:EntityRef; kind:"networkEdge"|"building"|null; unavailableReason:string|null;
  assetKey:string|null; assetKeyReason:string|null; owner:EntityRef|null; ownerReason:string|null;
  network:NetworkGeometry|null; networkReason:string|null; rootBounds:OrientedBounds|null; rootBoundsReason:string|null;
  family:null; familyReason:string; compatibleFamilies:null; compatibleFamiliesReason:string;
  installedUpgrades:null; installedUpgradesReason:string; facilityEnvelope:null; facilityEnvelopeReason:string;
  childEnvelope:null; childEnvelopeReason:string; platformEnvelope:null; platformEnvelopeReason:string;
  approachEnvelope:null; approachEnvelopeReason:string; connectors:null; connectorsReason:string;
  costs:null; costsReason:string; prerequisites:null; prerequisitesReason:string; capacityEffects:null; capacityEffectsReason:string;
}
export interface ConstructionSnapshot {
  scope:"explicitEntities"; requestedEntities:EntityRef[]; entities:ConstructionEntity[];
  worldBounds:null; worldBoundsReason:"notMeasured"; nextCursor:null; truncated:false;
  support:"partial"; nativeVerification:"CON-01:notRun";
}

export type ObservationBatchItem = {
    id: string;
    name: "state" | "city.overview" | "city.services" | "city.labor" | "city.budget";
    args: Record<string, never>;
} | {
    id: string;
    name: "construction.inventory";
    args: {
        world: WorldRef;
        entities: EntityRef[];
    };
} | {id:string;name:"transit.selectedLine";args:TransitRequest};
export interface ObservationBatchRequest {
    expectedWorld?: WorldRef;
    items: ObservationBatchItem[];
}
export interface ObservationBatchResult {
    world: WorldRef | null;
    startFrame: number | null;
    endFrame: number | null;
    worldChanged: boolean;
    results: ({
        id: string;
        observation: Observation<Record<string, unknown>>;
        error?: never;
    } | {
        id: string;
        error: BridgeErrorData;
        observation?: never;
    })[];
}
