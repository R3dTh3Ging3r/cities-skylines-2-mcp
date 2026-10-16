import { z } from "zod";
import { entityRefSchema, worldRefSchema } from "./construction-query.js";
import { metricReadRequestSchema, metricQuerySchema, metricScopeSchema,
  type MetricDefinition, type MetricReadRequest, parseMetricBatchObservation } from "./diagnostic-metric-contracts.js";

export const HISTORY_LIMITS = Object.freeze({series: 8, samples: 512, sampleBytes: 131072,
  seriesBytes: 1048576, totalBytes: 8388608, metadataBytes: 131072, gaps: 64,
  pageSamples: 128, resultBytes: 262144, network: 2});
export const revision = "diagnosticMetrics.v1" as const;
export const same = (a: unknown, b: unknown): boolean => JSON.stringify(a) === JSON.stringify(b);
export const bytes = (value: unknown): number => Buffer.byteLength(JSON.stringify(value), "utf8");
export const clone = <T>(value: T): T => structuredClone(value);
export function freeze<T>(value: T): T {
  if (value && typeof value === "object") {
    for (const child of Object.values(value)) freeze(child);
    Object.freeze(value);
  }
  return value;
}
export class HistoryError extends Error {
  constructor(public readonly code: string, message: string) { super(message); }
}
const worldSame = (a: z.infer<typeof worldRefSchema>, b: z.infer<typeof worldRefSchema>) =>
  a.bootId === b.bootId && a.epoch === b.epoch;
const refSame = (a: z.infer<typeof entityRefSchema>, b: z.infer<typeof entityRefSchema>) =>
  worldSame(a.world, b.world) && a.index === b.index && a.version === b.version;
export const cohortSchema = z.discriminatedUnion("kind", [
  z.object({kind: z.literal("city"), world: worldRefSchema, members: z.array(entityRefSchema).length(0)}).strict(),
  z.object({kind: z.literal("fixedSelectedLine"), world: worldRefSchema, members: z.array(entityRefSchema).length(1)}).strict(),
]);
export const seriesSpecSchema = z.object({world: worldRefSchema, queries: z.array(metricQuerySchema).min(1).max(64),
  scope: metricScopeSchema, cohort: cohortSchema, maxSamples: z.number().int().min(1).max(512),
  intervalMs: z.number().int().min(1000).max(86400000)}).strict().superRefine((spec, context) => {
    const parsed = metricReadRequestSchema.safeParse({world: spec.world, queries: spec.queries, scope: spec.scope});
    if (!parsed.success || !worldSame(spec.world, spec.cohort.world) ||
      (spec.scope.kind === "city" ? spec.cohort.kind !== "city" :
        spec.cohort.kind !== "fixedSelectedLine" || !refSame(spec.scope.line, spec.cohort.members[0]))) {
      context.addIssue({code: "custom", message: "metric request and fixed cohort must agree"});
    }
  });
export type SeriesSpec = z.infer<typeof seriesSpecSchema>;
export const idSchema = z.string().uuid();
export const sequenceSchema = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
export const readHistorySchema = z.object({id: idSchema, afterSequence: sequenceSchema.optional(),
  limit: z.number().int().min(1).max(128).default(128)}).strict();
export const rangeSchema = z.object({id: idSchema, fromSequence: sequenceSchema, toSequence: sequenceSchema}).strict()
  .refine(range => range.toSequence >= range.fromSequence, "reversed sequence range");
export const periodPolicySchema = z.object({mode: z.literal("rawNormalizedTime"),
  phaseTolerance: z.number().finite().min(0).max(.05), maxObservedPhaseStep: z.number().finite().gt(0).lt(1),
  frameDurationTolerance: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),
  maxSourceSpanFrames: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER)}).strict();
export const comparisonSchema = z.object({baseline: rangeSchema, current: rangeSchema, cohort: cohortSchema,
  policy: periodPolicySchema}).strict();
export type ComparisonRequest = z.infer<typeof comparisonSchema>;
export type MetricObservation = ReturnType<typeof parseMetricBatchObservation>;
export type HistorySample = {sequence: number; collectedAtUtc: string; observation: MetricObservation};
export const gapReasons = ["missedInterval", "backpressure", "readError", "invalidSample", "sampleTooLarge",
  "retention", "stopped", "disposed", "worldChanged", "frameReset"] as const;
export type GapReason = typeof gapReasons[number];
export type Gap = {reason: GapReason; fromSequence: number; toSequence: number; count: number};
export type GapSummary = Partial<Record<GapReason, {count: number; firstSequence: number; lastSequence: number}>>;
export type SeriesSnapshot = {id: string; spec: SeriesSpec; definitions: MetricDefinition[];
  definitionRevision: typeof revision; status: "active" | "stopped"; stopReason: string | null;
  nextSequence: number; samples: HistorySample[]; gaps: Gap[]; agedGaps: GapSummary};
export function metricRequest(spec: SeriesSpec): MetricReadRequest {
  return {world: spec.world, queries: spec.queries, scope: spec.scope};
}
