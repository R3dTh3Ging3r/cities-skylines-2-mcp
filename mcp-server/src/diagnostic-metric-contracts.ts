import { z } from "zod";
import { entityRefSchema, worldRefSchema } from "./construction-query.js";
export const legacyMetricKeys = ["city.population", "city.populationWithMoveIn", "city.money", "city.xp", "labor.employed", "labor.homelessCitizens", "labor.jobs.total", "labor.jobs.free", "budget.income", "budget.expenses", "budget.taxIncome", "budget.balance", "transit.waypointCount", "transit.segmentCount", "transit.brokenWaypointCount", "transit.invalidSegmentCount"] as const;
export const workforceMetricKeys = ["labor.jobs.totalByEducation.uneducated", "labor.jobs.totalByEducation.poorlyEducated", "labor.jobs.totalByEducation.educated", "labor.jobs.totalByEducation.wellEducated", "labor.jobs.totalByEducation.highlyEducated", "labor.jobs.freeByEducation.uneducated", "labor.jobs.freeByEducation.poorlyEducated", "labor.jobs.freeByEducation.educated", "labor.jobs.freeByEducation.wellEducated", "labor.jobs.freeByEducation.highlyEducated"] as const;
export const metricKeys = [...legacyMetricKeys, ...workforceMetricKeys] as const;
const metricMeanings = {
    "city.population": {
        "kind": "count",
        "unit": "citizens",
        "source": "Population.m_Population",
        "period": null
    },
    "city.populationWithMoveIn": {
        "kind": "count",
        "unit": "citizens",
        "source": "Population.m_PopulationWithMoveIn",
        "period": null
    },
    "city.money": {
        "kind": "stock",
        "unit": "nativeMoney",
        "source": "CitySystem.moneyAmount",
        "period": null
    },
    "city.xp": {
        "kind": "stock",
        "unit": "XP",
        "source": "CitySystem.XP",
        "period": null
    },
    "labor.employed": {
        "kind": "count",
        "unit": "workers",
        "source": "CountHouseholdDataSystem.CityWorkerCount",
        "period": null
    },
    "labor.homelessCitizens": {
        "kind": "count",
        "unit": "citizens",
        "source": "CountHouseholdDataSystem.HomelessCitizenCount",
        "period": null
    },
    "labor.jobs.total": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetTotalWorkplaces.TotalCount",
        "period": null
    },
    "labor.jobs.free": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetFreeWorkplaces.TotalCount",
        "period": null
    },
    "labor.jobs.totalByEducation.uneducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetTotalWorkplaces.m_Uneducated",
        "period": null
    },
    "labor.jobs.totalByEducation.poorlyEducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetTotalWorkplaces.m_PoorlyEducated",
        "period": null
    },
    "labor.jobs.totalByEducation.educated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetTotalWorkplaces.m_Educated",
        "period": null
    },
    "labor.jobs.totalByEducation.wellEducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetTotalWorkplaces.m_WellEducated",
        "period": null
    },
    "labor.jobs.totalByEducation.highlyEducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetTotalWorkplaces.m_HighlyEducated",
        "period": null
    },
    "labor.jobs.freeByEducation.uneducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetFreeWorkplaces.m_Uneducated",
        "period": null
    },
    "labor.jobs.freeByEducation.poorlyEducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetFreeWorkplaces.m_PoorlyEducated",
        "period": null
    },
    "labor.jobs.freeByEducation.educated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetFreeWorkplaces.m_Educated",
        "period": null
    },
    "labor.jobs.freeByEducation.wellEducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetFreeWorkplaces.m_WellEducated",
        "period": null
    },
    "labor.jobs.freeByEducation.highlyEducated": {
        "kind": "count",
        "unit": "workplaceSlots",
        "source": "CountWorkplacesSystem.GetFreeWorkplaces.m_HighlyEducated",
        "period": null
    },
    "budget.income": {
        "kind": "rate",
        "unit": "nativeMoney",
        "source": "CityServiceBudgetSystem.GetTotalIncome",
        "period": "month"
    },
    "budget.expenses": {
        "kind": "rate",
        "unit": "nativeMoney",
        "source": "CityServiceBudgetSystem.GetTotalExpenses",
        "period": "month"
    },
    "budget.taxIncome": {
        "kind": "rate",
        "unit": "nativeMoney",
        "source": "CityServiceBudgetSystem.GetTotalTaxIncome",
        "period": "month"
    },
    "budget.balance": {
        "kind": "rate",
        "unit": "nativeMoney",
        "source": "CityServiceBudgetSystem.GetBalance",
        "period": "month"
    },
    "transit.waypointCount": {
        "kind": "count",
        "unit": "waypoints",
        "source": "TransitCompleteness.waypointCount",
        "period": null
    },
    "transit.segmentCount": {
        "kind": "count",
        "unit": "segments",
        "source": "TransitCompleteness.segmentCount",
        "period": null
    },
    "transit.brokenWaypointCount": {
        "kind": "count",
        "unit": "waypoints",
        "source": "TransitWaypoint.structuralAssessment",
        "period": null
    },
    "transit.invalidSegmentCount": {
        "kind": "count",
        "unit": "segments",
        "source": "TransitSegment.structuralAssessment",
        "period": null
    }
} as const;
export const metricKeySchema = z.enum(metricKeys);
export const metricFamilySchema = z.enum(["all", "city", "labor", "budget", "transit"]);
export const metricQuerySchema = z.object({ key: metricKeySchema, parameters: z.object({}).strict() }).strict();
export const metricScopeSchema = z.discriminatedUnion("kind", [
    z.object({ kind: z.literal("city") }).strict(),
    z.object({ kind: z.literal("selectedLine"), line: entityRefSchema }).strict(),
]);
const sameWorld = (a: z.infer<typeof worldRefSchema>, b: z.infer<typeof worldRefSchema>) => a.bootId === b.bootId && a.epoch === b.epoch;
const sameRef = (a: z.infer<typeof entityRefSchema>, b: z.infer<typeof entityRefSchema>) => sameWorld(a.world, b.world) && a.index === b.index && a.version === b.version;
export const metricReadRequestSchema = z.object({ world: worldRefSchema, scope: metricScopeSchema, queries: z.array(metricQuerySchema).min(1).max(64) }).strict().superRefine((request, ctx) => {
    if (new Set(request.queries.map(q => q.key)).size !== request.queries.length)
        ctx.addIssue({ code: "custom", message: "duplicate metric key" });
    if (request.queries.some(q => q.key.startsWith("transit.") !== (request.scope.kind === "selectedLine")))
        ctx.addIssue({ code: "custom", message: "metric requires a different scope" });
    if (request.scope.kind === "selectedLine" && !sameWorld(request.world, request.scope.line.world))
        ctx.addIssue({ code: "custom", message: "metric scope world mismatch" });
});
export type MetricReadRequest = z.infer<typeof metricReadRequestSchema>;
const text = z.string().max(128), nullableText = text.nullable();
const warnings = z.array(text).max(32);
const frame = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
export const metricValueSchema = z.object({
    value: z.number().finite().nullable(), unit: nullableText, period: nullableText, source: nullableText, support: z.enum(["supported", "unavailable", "estimated"]), reason: nullableText
}).strict().superRefine((value, ctx) => {
    if ((value.support === "unavailable") !== (value.value === null) || value.value === null && (!value.reason || value.reason.length === 0))
        ctx.addIssue({ code: "custom", message: "unavailable metric requires null and a reason" });
});
export type MetricValue = z.infer<typeof metricValueSchema>;
export const metricDefinitionSchema = z.object({
    key: metricKeySchema, label: text, kind: z.enum(["stock", "count", "rate", "ratio", "state"]), unit: nullableText, period: nullableText, source: text, parameters: z.array(z.never()).length(0), parameterDefinitions: z.array(z.never()).length(0), refresh: text, support: z.object({ name: metricKeySchema, support: z.literal("unverified"), reason: z.literal("nativeDiagnosticMetricParityNotRun") }).strict()
}).strict().superRefine((definition, ctx) => {
    const meaning = metricMeanings[definition.key];
    if (definition.unit !== meaning.unit || definition.period !== meaning.period || definition.source !== meaning.source || definition.kind !== meaning.kind)
        ctx.addIssue({ code: "custom", message: "definition source/unit/period mismatch" });
    if (definition.support.name !== definition.key || (definition.kind === "rate") !== (definition.period !== null))
        ctx.addIssue({ code: "custom", message: "invalid definition meaning" });
});
export type MetricDefinition = z.infer<typeof metricDefinitionSchema>;
export const metricCatalogSchema = z.array(metricDefinitionSchema).min(1).max(26).superRefine((definitions, ctx) => {
    if (new Set(definitions.map(d => d.key)).size !== definitions.length)
        ctx.addIssue({ code: "custom", message: "duplicate metric definition" });
});
const source = z.object({
    id: z.enum(["state", "city.overview", "city.labor", "city.budget", "transit.selectedLine"]), world: worldRefSchema, startFrame: frame, endFrame: frame, sampledAtUtc: z.string().datetime(), freshness: z.enum(["current", "warming", "unavailable"]), warnings, completeness: z.enum(["complete", "incomplete"]), refreshTimeUtc: z.null()
}).strict();
export const metricClockSchema = z.object({
    source: z.literal("TimeSystem"), sourceObservationId: z.enum(["city.overview", "state"]), rawNormalizedTime: z.number().finite().min(0).lt(1), rawNormalizedDate: z.number().finite().min(0).lt(1), rawYear: z.number().int(), rawDaysPerYear: z.number().int().positive(), frameIndex: frame, startFrame: frame, endFrame: frame, frameEvidence: z.literal("sourceObservationEnd"), conversionStatus: z.literal("unverified"), reason: z.literal("nativeCalendarParityNotRun")
}).strict();
const outputScope = z.discriminatedUnion("kind", [z.object({ kind: z.literal("city"), line: z.null() }).strict(), z.object({ kind: z.literal("selectedLine"), line: entityRefSchema }).strict()]);
export const metricBatchSchema = z.object({
    schemaVersion: z.literal(1), definitionRevision: z.literal("diagnosticMetrics.v1"), scope: outputScope, values: z.record(metricValueSchema), members: z.array(entityRefSchema).max(1), perMember: z.array(z.object({ member: entityRefSchema, metrics: z.record(metricValueSchema) }).strict()).max(1), sources: z.array(source).min(1).max(4), clock: metricClockSchema.nullable(), memberEvidence: z.array(z.object({
        member: entityRefSchema, fingerprint: z.string().regex(/^[0-9a-f]{64}$/).nullable(), coverage: z.literal("structuralSampleOnly"), complete: z.boolean(), reasons: warnings
    }).strict()).max(1)
}).strict();
export type MetricBatch = z.infer<typeof metricBatchSchema>;
export const metricObservationSchema = z.object({
    data: z.record(metricValueSchema), world: worldRefSchema, startFrame: frame, endFrame: frame, sampledAtUtc: z.string().datetime(), freshness: z.enum(["current", "warming", "unavailable"]), warnings
}).strict();
export const metricBatchObservationSchema = metricObservationSchema.extend({ data: metricBatchSchema });
function checkValues(values: Record<string, MetricValue>, request: MetricReadRequest): void {
    const keys = request.queries.map(q => q.key);
    if (Object.keys(values).length !== keys.length || keys.some(k => !Object.hasOwn(values, k)))
        throw new Error("metric keys mismatch");
    for (const key of keys) {
        const value = values[key];
        const meaning = metricMeanings[key];
        if (value.unit !== meaning.unit || value.period !== meaning.period || value.source !== meaning.source)
            throw new Error("metric source/unit/period mismatch");
        if ((key.startsWith("transit.") || key.startsWith("labor.") || key.startsWith("city.population")) && value.value !== null && (!Number.isSafeInteger(value.value) || value.value < 0))
            throw new Error("invalid metric count");
    }
}
function checkObservation(observation: z.infer<typeof metricObservationSchema> | z.infer<typeof metricBatchObservationSchema>, request: MetricReadRequest): void {
    if (!sameWorld(observation.world, request.world) || observation.endFrame < observation.startFrame || observation.endFrame !== observation.startFrame && !observation.warnings.includes("multiFrameReadNotAtomic"))
        throw new Error("metric observation world/frame mismatch");
}
export function parseMetricObservation(value: unknown, request: MetricReadRequest) {
    const observation = metricObservationSchema.parse(value);
    checkObservation(observation, request);
    checkValues(observation.data, request);
    return observation;
}
export function parseMetricBatch(value: unknown, input: MetricReadRequest): MetricBatch {
    const request = metricReadRequestSchema.parse(input), batch = metricBatchSchema.parse(value);
    checkValues(batch.values, request);
    if (batch.scope.kind !== request.scope.kind)
        throw new Error("metric scope mismatch");
    const sourceFor = (key: string) => key.startsWith("city.") ? "city.overview" : key.startsWith("labor.") ? "city.labor" : key.startsWith("budget.") ? "city.budget" : "transit.selectedLine";
    const required = new Set<string>(request.queries.map(q => sourceFor(q.key)));
    if (!required.has("city.overview"))
        required.add("state");
    if ([...required].some(id => !batch.sources.some(s => s.id === id)) || batch.sources.some(s => !required.has(s.id) && s.id !== "state"))
        throw new Error("metric source selection mismatch");
    for (const query of request.queries) {
        const source = batch.sources.find(s => s.id === sourceFor(query.key))!;
        if (source.freshness === "unavailable" && batch.values[query.key].support !== "unavailable")
            throw new Error("unavailable source produced supported value");
    }
    if (new Set(batch.sources.map(s => s.id)).size !== batch.sources.length)
        throw new Error("duplicate metric sources");
    for (const source of batch.sources)
        if (!sameWorld(source.world, request.world) || source.endFrame < source.startFrame)
            throw new Error("metric source evidence mismatch");
    if (request.scope.kind === "city") {
        if (batch.members.length || batch.perMember.length || batch.memberEvidence.length)
            throw new Error("city metrics are an explicit empty cohort");
    }
    else {
        if (batch.scope.kind !== "selectedLine" || !sameRef(batch.scope.line, request.scope.line) || batch.members.length !== 1 || !sameRef(batch.members[0], request.scope.line) || batch.perMember.length !== 1 || !sameRef(batch.perMember[0].member, request.scope.line) || batch.memberEvidence.length !== 1 || !sameRef(batch.memberEvidence[0].member, request.scope.line))
            throw new Error("metric member mismatch");
        checkValues(batch.perMember[0].metrics, request);
        const fields = ["value", "unit", "period", "source", "support", "reason"] as const;
        if (request.queries.some(query => fields.some(field => batch.perMember[0].metrics[query.key][field] !== batch.values[query.key][field])))
            throw new Error("member values mismatch");
        const evidence = batch.memberEvidence[0];
        if (evidence.complete !== (evidence.fingerprint !== null))
            throw new Error("fingerprint completeness mismatch");
    }
    if (batch.clock) {
        const clock = batch.clock, clockSource = batch.sources.find(s => s.id === clock.sourceObservationId && s.freshness !== "unavailable");
        if (!clockSource || clock.frameIndex !== clock.endFrame || clock.startFrame !== clockSource.startFrame || clock.endFrame !== clockSource.endFrame || clock.endFrame < clock.startFrame)
            throw new Error("clock source span mismatch");
    }
    return batch;
}
export function parseMetricBatchObservation(value: unknown, request: MetricReadRequest) {
    const observation = metricBatchObservationSchema.parse(value);
    checkObservation(observation, request);
    parseMetricBatch(observation.data, request);
    if (observation.data.sources.some(s => s.startFrame < observation.startFrame || s.endFrame > observation.endFrame))
        throw new Error("source outside metric observation span");
    return observation;
}
export function metricQuery(input: MetricReadRequest): URLSearchParams {
    const query = new URLSearchParams({ query: JSON.stringify(metricReadRequestSchema.parse(input)) });
    if (Buffer.byteLength(query.toString(), "utf8") > 12000)
        throw new Error("metric GET query exceeds12000 bytes; use bounded O7 POST");
    return query;
}
