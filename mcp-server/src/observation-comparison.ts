import { parseMetricBatchObservation } from "./diagnostic-metric-contracts.js";
import { comparisonSchema, same, metricRequest, type ComparisonRequest,
  type HistorySample, type SeriesSnapshot } from "./observation-history-contracts.js";
import { historyResult } from "./observation-history-results.js";

type Window = {series: SeriesSnapshot; samples: HistorySample[]};
const qualifications = ["rawNormalizedOnly", "nativeCalendarParityNotRun", "sourceRefreshTimesUnknown",
  "observedSamplesNotNativeHistory", "sparseSamplesCannotProveContinuousCoverage"];
const ordered = <T extends {key: string}>(items: T[]) => [...items].sort((a, b) => a.key.localeCompare(b.key));
function evidence(sample: HistorySample) {
  const observation = sample.observation;
  return {sequence: sample.sequence, collectedAtUtc: sample.collectedAtUtc,
    world: observation.world, startFrame: observation.startFrame, endFrame: observation.endFrame,
    sampledAtUtc: observation.sampledAtUtc, freshness: observation.freshness, warnings: observation.warnings,
    clock: observation.data.clock, sources: observation.data.sources, memberEvidence: observation.data.memberEvidence};
}
function retainedWindow(series: SeriesSnapshot, range: ComparisonRequest["baseline"]): Window | string {
  const samples = series.samples.filter(sample => sample.sequence >= range.fromSequence && sample.sequence <= range.toSequence);
  if (samples.length < 2 || samples.length !== range.toSequence - range.fromSequence + 1 ||
    samples.some((sample, index) => sample.sequence !== range.fromSequence + index)) return "rangeNotFullyRetained";
  const overlap = (start: number, end: number) => start <= range.toSequence && end >= range.fromSequence;
  if (series.gaps.some(gap => overlap(gap.fromSequence, gap.toSequence)) ||
    Object.values(series.agedGaps).some(gap => gap && overlap(gap.firstSequence, gap.lastSequence))) return "gapCrossesRange";
  return {series, samples};
}
function timeEvidence(window: Window, policy: ComparisonRequest["policy"]): string | null {
  let previous: NonNullable<HistorySample["observation"]["data"]["clock"]> | undefined;
  let year: number | undefined, days: number | undefined;
  for (const sample of window.samples) {
    const observation = sample.observation;
    try {parseMetricBatchObservation(observation, metricRequest(window.series.spec));}
    catch {return "invalidStoredEvidence";}
    const clock = observation.data.clock;
    if (!clock) return "clockUnavailable";
    if (!Number.isSafeInteger(clock.rawYear) || !Number.isSafeInteger(clock.rawDaysPerYear)) return "unsafeClock";
    if (year !== undefined && (year !== clock.rawYear || days !== clock.rawDaysPerYear)) return "calendarDiscontinuity";
    year = clock.rawYear;
    days = clock.rawDaysPerYear;
    const bounds = [...observation.data.sources, clock];
    if (bounds.some(source => !Number.isSafeInteger(source.startFrame) || !Number.isSafeInteger(source.endFrame) || source.endFrame < source.startFrame)) return "invalidSourceSpan";
    if (Math.max(...bounds.map(source => source.endFrame)) - Math.min(...bounds.map(source => source.startFrame)) > policy.maxSourceSpanFrames) return "sourceSpanTooWide";
    if (observation.freshness !== "current" || observation.data.sources.some(source =>
      source.freshness !== "current" || source.completeness !== "complete")) return "incompleteSourceEvidence";
    if (previous && (clock.frameIndex <= previous.frameIndex || clock.rawNormalizedTime <= previous.rawNormalizedTime ||
      clock.rawNormalizedDate < previous.rawNormalizedDate ||
      clock.rawNormalizedTime - previous.rawNormalizedTime > policy.maxObservedPhaseStep)) return "observedClockDiscontinuity";
    previous = clock;
  }
  return null;
}

/** Consume retained facts only. This function never fetches or fills missing points. */
export function compareStoredObservations(input: ComparisonRequest, find: (id: string) => SeriesSnapshot) {
  const request = comparisonSchema.parse(input);
  const baseline = find(request.baseline.id), current = find(request.current.id);
  const spans = [baseline, current].map(series => ({id: series.id, retainedFrom: series.samples[0]?.sequence ?? null,
    retainedTo: series.samples.at(-1)?.sequence ?? null, nextSequence: series.nextSequence}));
  const refused = (reason: string) => ({status: "notComparable" as const, reasons: [reason], improvement: null,
    memberClassification: reason === "cohortMismatch" ? "missingOrNew" : reason === "structuralEvidenceChanged" ? "modified" : "unknown",
    requested: request, retained: spans, qualifications, pairs: []});
  const finish = <T>(result: T): T => {historyResult(result); return result;};
  if (!same(baseline.spec.world, current.spec.world) || !same(request.cohort, baseline.spec.cohort) ||
    !same(request.cohort, current.spec.cohort) || !same(baseline.spec.scope, current.spec.scope)) return finish(refused("cohortMismatch"));
  if (baseline.definitionRevision !== current.definitionRevision ||
    !same(ordered(baseline.definitions), ordered(current.definitions)) ||
    !same(ordered(baseline.spec.queries), ordered(current.spec.queries))) return finish(refused("metricMeaningMismatch"));
  const a = retainedWindow(baseline, request.baseline), b = retainedWindow(current, request.current);
  if (typeof a === "string" || typeof b === "string") return finish(refused(typeof a === "string" ? a : b as string));
  const error = timeEvidence(a, request.policy) ?? timeEvidence(b, request.policy);
  if (error) return finish(refused(error));
  if (request.cohort.kind === "fixedSelectedLine") {
    const all = [...a.samples, ...b.samples].map(sample => sample.observation.data.memberEvidence[0]);
    if (all.some(member => !member?.complete || !member.fingerprint)) return finish(refused("structuralEvidenceUnavailable"));
    if (all.some(member => member.fingerprint !== all[0].fingerprint)) return finish(refused("structuralEvidenceChanged"));
  }
  const clock = (sample: HistorySample) => sample.observation.data.clock!;
  const duration = (window: Window) => clock(window.samples.at(-1)!).frameIndex - clock(window.samples[0]).frameIndex;
  if (Math.abs(duration(a) - duration(b)) > request.policy.frameDurationTolerance) return finish(refused("frameDurationMismatch"));
  if (a.samples.length !== b.samples.length) return finish(refused("unpairedObservations"));
  for (let index = 0; index < a.samples.length; index++) {
    const phase = clock(a.samples[index]).rawNormalizedTime;
    const matches = b.samples.filter(sample => Math.abs(clock(sample).rawNormalizedTime - phase) <= request.policy.phaseTolerance);
    if (matches.length !== 1 || matches[0] !== b.samples[index]) return finish(refused("unpairedOrAmbiguousPhase"));
  }
  let complete = true;
  const definitions = ordered(baseline.definitions);
  const pairs = a.samples.map((left, index) => {
    const right = b.samples[index];
    const metrics = Object.fromEntries(definitions.map(definition => {
      const before = left.observation.data.values[definition.key], after = right.observation.data.values[definition.key];
      const supported = before.support === "supported" && after.support === "supported" &&
        before.value !== null && after.value !== null && definition.unit !== null;
      const delta = supported ? after.value! - before.value! : null;
      const relative = supported && before.value! > 0 ? delta! / before.value! : null;
      const valid = supported && Number.isFinite(delta) && (relative === null || Number.isFinite(relative));
      if (!valid) complete = false;
      return [definition.key, {status: valid ? "comparable" : "unavailable", baseline: before, current: after,
        delta: valid ? delta : null, relativeChange: valid ? relative : null, improvement: null}];
    }));
    return {baselineEvidence: evidence(left), currentEvidence: evidence(right), metrics};
  });
  const changes = Object.fromEntries(definitions.filter(definition => definition.kind === "stock" || definition.kind === "count")
    .map(definition => {
      const change = (window: Window) => {
        const points = window.samples.map(sample => sample.observation.data.values[definition.key]);
        if (points.some(value => value.support !== "supported" || value.value === null)) return null;
        const value = points.at(-1)!.value! - points[0].value!;
        return Number.isFinite(value) ? value : null;
      };
      const baselineChange = change(a), currentChange = change(b);
      if (baselineChange === null || currentChange === null) complete = false;
      return [definition.key, {baselineChange, currentChange, improvement: null}];
    }));
  return finish({status: complete ? "comparable" : "notComparable", reasons: complete ? [] : ["unavailableValueOrArithmetic"],
    improvement: null, memberClassification: request.cohort.kind === "city" ? "city" : "sameStructuralSample",
    requested: request, retained: spans, definitions, qualifications, pairs, changes});
}
