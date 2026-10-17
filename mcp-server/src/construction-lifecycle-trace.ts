import { z } from "zod";
import { worldRefSchema } from "./construction-query.js";
const integer = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
export const lifecycleTraceQuerySchema = z.object({
    bootId: z.string().min(1).max(128).refine(value => value.trim().length > 0).optional(),
    afterSequence: integer.optional(),
    limit: z.number().int().min(1).max(32).optional(),
}).strict().superRefine((query, ctx) => {
    if (query.afterSequence !== undefined && query.bootId === undefined)
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["bootId"], message: "trace cursor requires bootId" });
});
export type LifecycleTraceQuery = z.infer<typeof lifecycleTraceQuerySchema>;
const reference = z.object({
    role: z.enum(["prefab", "target", "start", "end", "stop", "line"]),
    world: worldRefSchema.nullable(), index: z.number().int().min(-2147483648).max(2147483647),
    version: z.number().int().min(-2147483648).max(2147483647),
}).strict();
const event = z.object({
    route: z.enum(["unknown", "cityTaxesSet", "cityPoliciesSet", "cityServiceBudgetsSet", "buildPlace", "buildRoad", "buildUpgrade", "buildJunction", "buildZone", "buildDemolish", "cityLoanSet", "cityFeesSet", "cameraSet", "simControl", "simRun", "gameSave", "buildDistrict", "districtPoliciesSet", "transitStopsPlace", "transitLinesCreate", "transitLinesDelete", "cityTilesBuy", "buildRoadReplace", "buildRoadConnect", "buildPlaceShoreline", "buildPlaceRoadside", "transitLinesPoliciesSet", "transitLinesVehiclesSet"]),
    sequence: integer.min(1), traceId: z.string().regex(/^legacy:[a-f0-9]{32}$/), identityKind: z.literal("legacyDiagnosticOnly"),
    ingressWorld: worldRefSchema.nullable(), observedWorld: worldRefSchema, observedReady: z.boolean(),
    frame: integer.nullable(), frameReason: z.literal("notSampled").nullable(), recordedAtUtc: z.string().datetime(),
    adapter: z.enum(["envelope", "generic", "road", "transit", "rename"]),
    kind: z.enum(["direct", "placement", "net", "demolish", "upgrade", "area", "nodeUpgrade", "roadBuild", "roadReplace", "stop", "line"]),
    phase: z.enum(["dispatch", "queued", "busy", "definitions", "validation", "validationAccepted", "validationRejected", "applyObserved", "finishObserved", "replyAttempt", "deactivateObserved", "restorationAttempt", "stopObserved", "displacedObserved", "exceptionObserved", "renameQueued", "renameDequeued", "setterAttempt", "responseBuilderAttempt", "guardedWorkRefused", "destroyedObserved"]),
    status: z.number().int().min(100).max(599).nullable(), ownsActiveTool: z.boolean().nullable(), completionAccepted: z.boolean().nullable(),
    references: z.array(reference).max(4), omittedReferences: z.number().int().min(0).max(2147483647),
    effects: z.literal("unknown"), cleanup: z.literal("unverified"), authority: z.literal("unverified"),
}).strict();
const pageSchema = z.object({
    provenance: z.literal("managedPassiveHistory"), consistency: z.literal("independentPageNotSnapshot"), completion: z.literal("notEstablished"),
    bootId: z.string().min(1).max(128), readWorld: worldRefSchema, readReady: z.boolean(), readAtUtc: z.string().datetime(),
    highWater: integer, retainedFloor: integer.min(1).nullable(), retainedCount: z.number().int().min(0).max(256), retainedBytes: integer.max(2097152),
    evictedEvents: integer, lostObservations: integer, saturated: z.boolean(), tailOmitted: integer,
    evictedFrom: integer.min(1).nullable(), evictedThrough: integer.min(1).nullable(), nextSequence: integer.nullable(), hasMore: z.boolean(),
    limit: z.number().int().min(1).max(32), events: z.array(event).max(32),
}).strict();
const sameWorld = (a: {
    bootId: string;
    epoch: number;
} | null, b: {
    bootId: string;
    epoch: number;
} | null) => a === null ? b === null : b !== null && a.bootId === b.bootId && a.epoch === b.epoch;
/** History event worlds/frames belong to their original callbacks, not the outer O7 read. */
export function parseLifecycleTracePage(value: unknown, input: LifecycleTraceQuery) {
    const query = lifecycleTraceQuerySchema.parse(input);
    const page = pageSchema.parse(value);
    if (Buffer.byteLength(JSON.stringify(page), "utf8") > 262144 || page.limit !== (query.limit ?? 16) ||
        (query.bootId !== undefined && query.bootId !== page.bootId) || page.readWorld.bootId !== page.bootId ||
        page.events.length > page.limit || page.events.length > page.retainedCount ||
        ((page.retainedCount === 0) !== (page.retainedFloor === null)) ||
        (page.retainedFloor !== null && page.retainedFloor !== page.highWater - page.retainedCount + 1) ||
        page.events.some(e => e.sequence > page.highWater) ||
        page.evictedEvents !== page.highWater - page.retainedCount || page.retainedBytes + page.retainedCount + 1 > 2097152 || page.retainedBytes > page.retainedCount * 8192 || page.retainedBytes < page.retainedCount || (page.retainedCount === 0 && page.retainedBytes !== 0))
        throw new Error("invalid bounded lifecycle page metadata");
    let previous = query.afterSequence ?? 0;
    for (const entry of page.events) {
        if (Buffer.byteLength(JSON.stringify(entry), "utf8") > 8192 || entry.sequence <= previous ||
            (page.retainedFloor !== null && entry.sequence < page.retainedFloor) ||
            entry.observedWorld.bootId !== page.bootId || (entry.ingressWorld !== null && entry.ingressWorld.bootId !== page.bootId) ||
            ((entry.frame === null) !== (entry.frameReason === "notSampled")) ||
            (entry.phase === "replyAttempt" ? entry.completionAccepted === null || entry.frame !== null : entry.completionAccepted !== null) ||
            entry.references.some(ref => !sameWorld(ref.world, entry.ingressWorld)))
            throw new Error("invalid lifecycle event evidence");
        previous = entry.sequence;
    }
    const after = query.afterSequence;
    const expectedNext = page.events.length ? previous : after ?? null;
    const expectedGap = after !== undefined && page.retainedFloor !== null && after < page.retainedFloor - 1;
    const expectedLength = after === undefined ? Math.min(page.limit, page.retainedCount)
        : Math.min(page.limit, Math.max(0, page.highWater - Math.max(after, (page.retainedFloor ?? 1) - 1)));
    if (after !== undefined && after > page.highWater || page.events.length !== expectedLength ||
        page.nextSequence !== expectedNext || page.hasMore !== (expectedNext !== null && expectedNext < page.highWater) ||
        page.tailOmitted !== (after === undefined ? page.retainedCount - page.events.length : 0) ||
        page.evictedFrom !== (expectedGap ? after! + 1 : null) ||
        page.evictedThrough !== (expectedGap ? page.retainedFloor! - 1 : null) ||
        (page.events.length > 0 && (page.events[0].sequence !== (after === undefined ? page.highWater - page.events.length + 1 : Math.max(after + 1, page.retainedFloor!)) ||
            page.events.some((e, i) => i > 0 && e.sequence !== page.events[i - 1].sequence + 1))))
        throw new Error("invalid lifecycle page cursor evidence");
    return page;
}
export function lifecycleTraceFailure(details?: {
    code?: string;
    message?: string;
}) {
    const known = new Set(["INVALID_ARGUMENT", "WORLD_MISMATCH", "NOT_READY", "BRIDGE_BUSY", "RESULT_TOO_LARGE", "PROTOCOL_ERROR", "TRANSPORT_UNAVAILABLE"]);
    const code = details?.code && known.has(details.code) ? details.code : "PROTOCOL_ERROR";
    const reason = details?.message === "trace cursor boot mismatch" ? "bootMismatch"
        : details?.message === "trace cursor is ahead of journal" ? "futureCursor"
            : details?.message === "lifecycle trace page exceeds byte limit" ? "pageByteLimit" : "unknownCause";
    return { code, reason, message: `Lifecycle history unavailable (${reason}); no retry was performed.`, retryable: false, outcome: "unknown" as const };
}
