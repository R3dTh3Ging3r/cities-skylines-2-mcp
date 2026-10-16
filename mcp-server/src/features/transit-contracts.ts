import { z } from "zod";
import { entityRefSchema, worldRefSchema } from "../construction-query.js";
import type { WorldRef } from "../contracts.js";
export const transitRequestSchema = z.object({ line: entityRefSchema, mode: z.enum(["bus", "tram", "metro", "train", "airplane"]).optional(), purpose: z.enum(["passenger", "cargo"]).optional() }).strict();
export type TransitRequest = z.infer<typeof transitRequestSchema>;
const text = z.string().max(128);
const nullableText = text.nullable();
const reasons = z.array(text).max(128);
const point = z.object({ x: z.number().finite(), y: z.number().finite(), z: z.number().finite() }).strict().nullable();
const reference = entityRefSchema.nullable();
const flags = z.object({ passenger: z.boolean().nullable(), cargo: z.boolean().nullable() }).strict();
const stop = z.object({ entity: reference, position: point, positionReason: nullableText, owner: reference, ownerReason: nullableText, attachedParent: reference, attachedParentReason: nullableText, isAttachedToEdge: z.boolean().nullable(), mode: nullableText, purposeFlags: flags, active: z.boolean().nullable(), activeReason: nullableText, outsideConnection: z.boolean() }).strict();
const waypoint = z.object({ order: z.number().int().min(1).max(128), entity: reference, state: text, connection: reference, connectionState: text, position: point, positionReason: nullableText, waitingPassengers: z.number().int().min(0).max(2147483647).nullable(), waitingReason: nullableText, stop: stop.nullable(), structuralAssessment: z.enum(["knownFault", "noKnownFault", "unassessed"]), issues: reasons }).strict();
const segment = z.object({ order: z.number().int().min(1).max(128), entity: reference, state: text, rawDistance: z.number().finite().nullable(), distanceReason: nullableText, structuralAssessment: z.enum(["knownFault", "noKnownFault", "unassessed"]), issues: reasons }).strict();
const count = z.number().int().min(0).max(2147483647).nullable();
const view = z.object({
    entity: entityRefSchema, assetKey: z.string().max(256).nullable(), assetKeyReason: nullableText, owner: reference, ownerReason: nullableText, routeNumber: z.number().int().nullable(), mode: nullableText, modeReason: nullableText,
    purpose: z.enum(["passenger", "cargo", "mixed", "unknown"]), purposeFlags: flags, purposeReason: nullableText,
    name: z.null(), nameReason: z.literal("labelReaderNotBounded"), active: z.null(), activeReason: z.literal("lineActiveReaderNotVerified"), vehicles: z.null(), vehiclesReason: z.literal("boundedFleetReaderUnavailable"),
    load: z.object({ kind: z.enum(["passengers", "cargo"]).nullable(), value: z.null(), capacity: z.null(), unit: z.null(), occupancyRatio: z.null(), reason: z.literal("boundedLoadReaderUnavailable") }).strict(),
    completeness: z.object({ state: z.enum(["complete", "incomplete"]), waypointCount: count, segmentCount: count, readWaypoints: z.number().int().min(0).max(128), readSegments: z.number().int().min(0).max(128), reasons }).strict(),
    waypoints: z.array(waypoint).max(128), segments: z.array(segment).max(128),
    health: z.object({ state: z.enum(["invalid", "unknown"]), reasons, brokenWaypoints: z.array(entityRefSchema).max(128), invalidSegments: z.array(entityRefSchema).max(128), rawLength: z.number().finite().nonnegative().nullable(), lengthUnit: z.null(), lengthUnitReason: z.literal("nativeSegmentDistanceUnitNotVerified"), lengthValidation: z.literal("finiteNonnegativeSegmentsOnly"), nativeValidity: z.literal("unknown"), nativeValidityReason: z.literal("committedRouteFixtureNotRun"), assessment: z.literal("structuralFactsOnly") }).strict(),
    candidateDuplicates: z.null(), candidateDuplicatesReason: z.literal("discoveryUnsupported"),
}).strict();
const filter = z.object({ state: z.enum(["notRequested", "matched", "excluded", "unverifiable"]), reasons }).strict();
export const transitCapabilitiesSchema = z.object({ scope: z.literal("explicitLine"), support: z.literal("partial"), maxRouteEntries: z.literal(128), modes: z.array(z.enum(["bus", "tram", "metro", "train", "airplane"])).length(5), nativeVerification: z.literal("transit-readback-v1:notRun"), discovery: z.literal(false), load: z.literal(false), nativeValidity: z.literal(false) }).strict();
const page = z.object({ scope: z.literal("explicitLine"), requestedLine: entityRefSchema, items: z.array(view).max(1), returned: z.number().int().min(0).max(1), matched: z.number().int().min(0).max(1).nullable(), filter, nextCursor: z.null(), truncated: z.literal(false), nativeVerification: z.literal("transit-readback-v1:notRun"), capabilities: transitCapabilitiesSchema }).strict();
export type TransitPage = z.infer<typeof page>;
const frame = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const observation = z.object({ data: page, world: worldRefSchema, startFrame: frame, endFrame: frame, sampledAtUtc: z.string().datetime(), freshness: z.enum(["current", "warming"]), warnings: z.array(text).max(32) }).strict();
export type TransitCapabilities = z.infer<typeof transitCapabilitiesSchema>;
export function sameWorld(a: WorldRef, b: WorldRef): boolean { return a.bootId === b.bootId && a.epoch === b.epoch; }
export function transitQuery(input: TransitRequest): URLSearchParams {
    const request = transitRequestSchema.parse(input);
    const query = new URLSearchParams({ bootId: request.line.world.bootId, epoch: String(request.line.world.epoch), line: `${request.line.index}:${request.line.version}` });
    if (request.mode)
        query.set("mode", request.mode);
    if (request.purpose)
        query.set("purpose", request.purpose);
    return query;
}
export function parseTransitObservation(value: unknown, input: TransitRequest): z.infer<typeof observation> {
    const request = transitRequestSchema.parse(input);
    const result = observation.parse(value);
    const data = result.data;
    if (!sameWorld(result.world, request.line.world) || result.endFrame < result.startFrame ||
        data.requestedLine.index !== request.line.index || data.requestedLine.version !== request.line.version ||
        data.returned !== data.items.length ||
        ((data.filter.state === "notRequested") !== (request.mode === undefined && request.purpose === undefined)) || (data.filter.state === "unverifiable" ? (data.matched !== null || data.returned !== 1) : data.filter.state === "excluded" ? (data.matched !== 0 || data.returned !== 0) : (data.matched !== 1 || data.returned !== 1)) ||
        (result.endFrame !== result.startFrame && !result.warnings.includes("multiFrameReadNotAtomic")))
        throw new Error("invalid selected transit identity/filter/frame evidence");
    function refs(value: unknown): void {
        if (!value || typeof value !== "object")
            return;
        const object = value as Record<string, unknown>;
        if ("world" in object && "index" in object && "version" in object && !sameWorld(object.world as WorldRef, request.line.world))
            throw new Error("transit reference belongs to another world");
        for (const child of Object.values(object))
            refs(child);
    }
    refs(data);
    for (const line of data.items) {
        const complete = line.completeness;
        const entries = [...line.waypoints, ...line.segments];
        const hasKnownFault = entries.some(entry => entry.structuralAssessment === "knownFault");
        if ((line.health.state === "invalid") !== hasKnownFault ||
            line.entity.index !== request.line.index || line.entity.version !== request.line.version || line.waypoints.length + line.segments.length > 128 || complete.readWaypoints !== line.waypoints.length || complete.readSegments !== line.segments.length ||
            (complete.state === "complete" && (complete.waypointCount !== line.waypoints.length || complete.segmentCount !== line.segments.length)) ||
            (line.health.rawLength !== null && (complete.state !== "complete" || line.health.state !== "unknown" || line.health.reasons.length !== 0 || entries.some(entry => entry.structuralAssessment !== "noKnownFault"))))
            throw new Error("invalid bounded transit facts");
    }
    return result;
}
