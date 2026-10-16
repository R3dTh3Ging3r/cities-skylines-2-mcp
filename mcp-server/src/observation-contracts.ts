import { z } from "zod";
import { constructionRequestSchema, worldRefSchema } from "./construction-query.js";
import type { ObservationBatchRequest, ObservationBatchResult } from "./contracts.js";
const context = z.object({ id: z.string().min(1).max(128), name: z.enum(["state", "city.overview", "city.services", "city.labor", "city.budget"]), args: z.object({}).strict() }).strict();
const inventory = z.object({ id: z.string().min(1).max(128), name: z.literal("construction.inventory"), args: constructionRequestSchema }).strict();
export const observationRequestSchema = z.object({ expectedWorld: worldRefSchema.optional(), items: z.array(z.discriminatedUnion("name", [context, inventory])).min(1).max(64) }).strict().superRefine((request, ctx) => {
    const ids = new Set<string>();
    for (const [i, item] of request.items.entries()) {
        if (ids.has(item.id))
            ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["items", i, "id"], message: "duplicate observation ID" });
        ids.add(item.id);
    }
});
const frame = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER).nullable();
const observation = z.object({ data: z.record(z.unknown()), world: worldRefSchema.nullable(), sampledAtUtc: z.string().datetime(), startFrame: frame, endFrame: frame, freshness: z.enum(["current", "warming", "unavailable"]), warnings: z.array(z.string().max(128)).max(32) }).strict();
const error = z.object({ code: z.string().min(1).max(128), message: z.string().max(2048), retryable: z.boolean(), outcome: z.enum(["notApplied", "unknown", "partial"]), httpStatus: z.number().int().optional(), nativeErrors: z.array(z.string().max(2048)).max(32).optional(), operationId: z.string().optional() }).strict();
const row = z.object({ id: z.string().min(1).max(128), observation: observation.optional(), error: error.optional() }).strict().refine(r => (r.observation !== undefined) !== (r.error !== undefined), "exactly one observation/error required");
const response = z.object({ world: worldRefSchema.nullable(), startFrame: frame, endFrame: frame, worldChanged: z.boolean(), results: z.array(row).min(1).max(64) }).strict();
const same = (a: {
    bootId: string;
    epoch: number;
} | null, b: {
    bootId: string;
    epoch: number;
} | null) => a !== null && b !== null && a.bootId === b.bootId && a.epoch === b.epoch;
export function parseObservationResult(value: unknown, request: ObservationBatchRequest): ObservationBatchResult {
    const result = response.parse(value);
    if (result.results.length !== request.items.length || (request.expectedWorld && !same(request.expectedWorld, result.world)) ||
        (result.startFrame !== null && result.endFrame !== null && result.endFrame < result.startFrame) ||
        (result.worldChanged && result.endFrame !== null) || (result.startFrame === null && result.endFrame !== null))
        throw new Error("invalid group identity/frame span");
    for (const [i, item] of result.results.entries()) {
        const expected = request.items[i];
        if (item.id !== expected.id)
            throw new Error("missing, duplicated or reordered observation ID");
        if (expected.name === "construction.inventory" && !same(expected.args.world, result.world))
            throw new Error("inventory world mismatch");
        const observation = item.observation;
        if (!observation)
            continue;
        if (!same(observation.world, result.world) || observation.startFrame === null || observation.endFrame === null || result.startFrame === null ||
            observation.endFrame < observation.startFrame || observation.startFrame < result.startFrame ||
            (result.endFrame !== null && observation.endFrame > result.endFrame) ||
            (observation.endFrame !== observation.startFrame && !observation.warnings.includes("multiFrameReadNotAtomic")))
            throw new Error("invalid observation identity/frame evidence");
    }
    return { ...result, results: result.results.map(item => item.observation ? { id: item.id, observation: item.observation } : { id: item.id, error: item.error! }) };
}
