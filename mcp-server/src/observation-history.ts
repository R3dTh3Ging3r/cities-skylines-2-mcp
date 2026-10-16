import { randomUUID } from "node:crypto";
import { performance } from "node:perf_hooks";
import type { BridgeClient } from "./bridge-client.js";
import { BridgeClientError } from "./bridge-client.js";
import { metricCatalogSchema, parseMetricBatchObservation } from "./diagnostic-metric-contracts.js";
import { parseObservationResult } from "./observation-contracts.js";
import { bytes, clone, freeze, HistoryError, HISTORY_LIMITS as limits, idSchema,
  metricRequest, readHistorySchema, revision, seriesSpecSchema, type GapReason,
  type HistorySample, type SeriesSnapshot, type SeriesSpec } from "./observation-history-contracts.js";
import { historyResult } from "./observation-history-results.js";
import { compareStoredObservations } from "./observation-comparison.js";

export interface HistoryClock {
  nowMs(): number;
  setTimeout(callback: () => void, delayMs: number): unknown;
  clearTimeout(handle: unknown): void;
}
const nativeClock: HistoryClock = {nowMs: () => performance.now(),
  setTimeout: (callback, ms) => setTimeout(callback, ms),
  clearTimeout: handle => clearTimeout(handle as ReturnType<typeof setTimeout>)};

/** A reservation lives until its original network promise settles, even after abort. */
class NetworkGate {
  private active = 0;
  private waiting: {signal: AbortSignal; accept: (release: () => void) => void; reject: (error: Error) => void; abort: () => void}[] = [];
  constructor(private readonly available: () => void) {}
  private reserve(): () => void {
    this.active++;
    let released = false;
    return () => {
      if (released) return;
      released = true;
      this.active--;
      this.drain();
      this.available();
    };
  }
  tryAcquire(): (() => void) | undefined {
    return this.active < limits.network && this.waiting.length === 0 ? this.reserve() : undefined;
  }
  acquire(signal: AbortSignal): Promise<() => void> {
    if (signal.aborted) return Promise.reject(new HistoryError("READ_CANCELLED", "Collection admission cancelled"));
    const immediate = this.tryAcquire();
    if (immediate) return Promise.resolve(immediate);
    return new Promise((accept, reject) => {
      const item = {signal, accept, reject, abort: () => {
        this.waiting = this.waiting.filter(candidate => candidate !== item);
        reject(new HistoryError("READ_CANCELLED", "Collection admission cancelled"));
      }};
      this.waiting.push(item);
      signal.addEventListener("abort", item.abort, {once: true});
    });
  }
  private drain(): void {
    while (this.active < limits.network && this.waiting.length) {
      const item = this.waiting.shift()!;
      item.signal.removeEventListener("abort", item.abort);
      if (item.signal.aborted) item.reject(new HistoryError("READ_CANCELLED", "Collection admission cancelled"));
      else item.accept(this.reserve());
    }
  }
}

type Series = SeriesSnapshot & {generation: number; due: number; sampleBytes: number;
  lastBounds?: Map<string, {startFrame: number; endFrame: number}>;
  flight?: {generation: number; sequence: number; controller: AbortController}};

export class ObservationHistory {
  private readonly series = new Map<string, Series>();
  private readonly admissions = new Set<AbortController>();
  private readonly gate: NetworkGate;
  private readonly clock: HistoryClock;
  private readonly utcNow: () => string;
  private timer?: unknown;
  private disposed = false;
  private lifetime = 0;
  private cursor = 0;
  private pumpQueued = false;

  constructor(private readonly bridge: Pick<BridgeClient, "metricCatalog" | "observe">,
    options: {clock?: HistoryClock; utcNow?: () => string} = {}) {
    this.clock = options.clock ?? nativeClock;
    this.utcNow = options.utcNow ?? (() => new Date().toISOString());
    this.gate = new NetworkGate(() => this.queuePump());
  }

  async startCollection(input: SeriesSpec, options: {signal?: AbortSignal} = {}) {
    const spec = freeze(seriesSpecSchema.parse(input));
    this.requireLive();
    if (this.series.size + this.admissions.size >= limits.series) {
      throw new HistoryError("SERIES_CAPACITY", "Eight histories are retained; stop does not delete histories");
    }
    const controller = new AbortController();
    const lifetime = this.lifetime;
    const abort = () => controller.abort();
    this.admissions.add(controller);
    if (options.signal?.aborted) controller.abort();
    else options.signal?.addEventListener("abort", abort, {once: true});
    let release: (() => void) | undefined;
    try {
      release = await this.gate.acquire(controller.signal);
      const catalog = metricCatalogSchema.parse(await this.bridge.metricCatalog("all", {signal: controller.signal}));
      this.requireLive();
      if (controller.signal.aborted || lifetime !== this.lifetime) {
        throw new HistoryError("READ_CANCELLED", "Collection admission cancelled");
      }
      const definitions = spec.queries.map(query => {
        const definition = catalog.find(candidate => candidate.key === query.key);
        if (!definition) throw new HistoryError("INVALID_ARGUMENT", "Requested metric absent from authoritative catalog");
        return definition;
      });
      const series: Series = {id: randomUUID(), spec, definitions: freeze(definitions), definitionRevision: revision,
        status: "active", stopReason: null, nextSequence: 1, samples: [], gaps: [], agedGaps: {},
        generation: 0, due: this.clock.nowMs(), sampleBytes: 0};
      if (bytes(this.metadata(series)) > limits.metadataBytes) {
        throw new HistoryError("RESULT_TOO_LARGE", "Collection metadata exceeds128KiB");
      }
      this.series.set(series.id, series);
      this.queuePump();
      return this.status(series);
    } finally {
      options.signal?.removeEventListener("abort", abort);
      this.admissions.delete(controller);
      release?.();
    }
  }

  stopCollection(id: string) {
    const series = this.find(id);
    this.stop(series, "stopped");
    this.clearTimer();
    this.queuePump();
    return this.status(series);
  }

  readSeries(id: string, options: {afterSequence?: number; limit?: number} = {}) {
    const request = readHistorySchema.parse({id, ...options});
    const series = this.find(request.id);
    const candidates = series.samples.filter(sample => sample.sequence > (request.afterSequence ?? 0));
    const page = {...this.status(series), samples: [] as HistorySample[], nextSequence: null as number | null};
    for (const sample of candidates.slice(0, request.limit)) {
      page.samples.push(sample);
      page.nextSequence = sample.sequence < (candidates.at(-1)?.sequence ?? 0) ? sample.sequence + 1 : null;
      try { historyResult(page); }
      catch (error) {
        page.samples.pop();
        page.nextSequence = sample.sequence;
        if (!page.samples.length) throw error;
        break;
      }
    }
    historyResult(page);
    return clone(page);
  }

  compareObservations(input: Parameters<typeof compareStoredObservations>[0]) {
    return compareStoredObservations(input, id => this.snapshot(this.find(id)));
  }

  dispose(): void {
    if (this.disposed) return;
    this.disposed = true;
    this.lifetime++;
    this.clearTimer();
    for (const controller of this.admissions) controller.abort();
    for (const series of this.series.values()) this.stop(series, "disposed");
  }

  private find(id: string): Series {
    idSchema.parse(id);
    const series = this.series.get(id);
    if (!series) throw new HistoryError("SERIES_NOT_FOUND", "History is not retained in this process");
    return series;
  }
  private requireLive(): void {
    if (this.disposed) throw new HistoryError("DISPOSED", "Collection connection is closed");
  }
  private snapshot(series: Series): SeriesSnapshot {
    return {...this.metadata(series), samples: series.samples};
  }
  private metadata(series: Series): Omit<SeriesSnapshot, "samples"> {
    return {id: series.id, spec: series.spec, definitions: series.definitions, definitionRevision: series.definitionRevision,
      status: series.status, stopReason: series.stopReason, nextSequence: series.nextSequence,
      gaps: series.gaps, agedGaps: series.agedGaps};
  }
  private status(series: Series) {
    const metadata = this.metadata(series);
    return clone({...metadata, sampleCount: series.samples.length,
      firstSequence: series.samples[0]?.sequence ?? null, lastSequence: series.samples.at(-1)?.sequence ?? null,
      retainedBytes: bytes(metadata) + series.sampleBytes + series.samples.length + 14,
      limits, qualifications: ["observedSamplesNotNativeHistory", "sourceRefreshTimesUnknown", "nativeCalendarParityNotRun"]});
  }
  private clearTimer(): void {
    if (this.timer !== undefined) this.clock.clearTimeout(this.timer);
    this.timer = undefined;
  }
  private queuePump(): void {
    if (this.disposed || this.pumpQueued) return;
    this.pumpQueued = true;
    queueMicrotask(() => {this.pumpQueued = false; this.pump();});
  }
  private pump(): void {
    this.clearTimer();
    if (this.disposed) return;
    const all = [...this.series.values()];
    const now = this.clock.nowMs();
    const cursor = this.cursor;
    for (let offset = 0; offset < all.length; offset++) {
      const index = (cursor + offset) % all.length;
      const series = all[index];
      if (series.status !== "active" || series.due > now) continue;
      const elapsed = Math.floor((now - series.due) / series.spec.intervalMs) + 1;
      series.due += elapsed * series.spec.intervalMs;
      if (!Number.isSafeInteger(series.nextSequence + elapsed)) {
        this.stop(series, "sequenceLimit");
        continue;
      }
      const release = series.flight ? undefined : this.gate.tryAcquire();
      if (!release) {
        this.gap(series, series.flight ? "missedInterval" : "backpressure", series.nextSequence, elapsed);
        series.nextSequence += elapsed;
        continue;
      }
      if (elapsed > 1) {
        this.gap(series, "missedInterval", series.nextSequence, elapsed - 1);
        series.nextSequence += elapsed - 1;
      }
      const flight = {generation: series.generation, sequence: series.nextSequence++, controller: new AbortController()};
      series.flight = flight;
      // Advance fairness only when granting work. Native getters are not preemptible;
      // cancellation detaches pending read work and fences publication, not native execution.
      this.cursor = (index + 1) % all.length;
      void this.poll(series, flight).finally(() => {
        if (series.flight === flight) series.flight = undefined;
        release();
      });
    }
    const active = all.filter(series => series.status === "active");
    if (active.length) this.timer = this.clock.setTimeout(() => this.pump(),
      Math.max(0, Math.min(...active.map(series => series.due)) - this.clock.nowMs()));
  }

  private async poll(series: Series, flight: NonNullable<Series["flight"]>): Promise<void> {
    const live = () => !this.disposed && series.status === "active" && series.generation === flight.generation;
    try {
      const request = {expectedWorld: series.spec.world,
        items: [{id: "metrics", name: "metrics.read" as const, args: metricRequest(series.spec)}]};
      const raw = await this.bridge.observe(request, {signal: flight.controller.signal});
      if (!live()) return;
      if (raw.worldChanged) {
        this.gap(series, "worldChanged", flight.sequence, 1);
        this.stop(series, "worldChanged", false);
        return;
      }
      const result = parseObservationResult(raw, request);
      const item = result.results[0];
      if (item.error) {
        if (item.error.code === "WORLD_MISMATCH") throw new HistoryError("WORLD_MISMATCH", "Observed world changed");
        this.gap(series, "readError", flight.sequence, 1);
        return;
      }
      const observation = parseMetricBatchObservation(item.observation, metricRequest(series.spec));
      const bounds = [{id: "observation", startFrame: observation.startFrame, endFrame: observation.endFrame}, ...observation.data.sources,
        ...(observation.data.clock ? [{id: "clock", ...observation.data.clock}] : [])];
      if (bounds.some(bound => {
        const previous = series.lastBounds?.get(bound.id);
        return previous && (bound.startFrame < previous.startFrame || bound.endFrame < previous.endFrame);
      })) {
        this.gap(series, "frameReset", flight.sequence, 1);
        this.stop(series, "frameReset", false);
        return;
      }
      series.lastBounds ??= new Map();
      for (const bound of bounds) series.lastBounds.set(bound.id,
        {startFrame: bound.startFrame, endFrame: bound.endFrame});
      const sample = freeze({sequence: flight.sequence, collectedAtUtc: this.utcNow(), observation});
      const size = bytes(sample);
      if (size > limits.sampleBytes) this.gap(series, "sampleTooLarge", flight.sequence, 1);
      else {
        series.samples.push(sample);
        series.sampleBytes += size;
        while (series.samples.length > series.spec.maxSamples ||
          series.sampleBytes + series.samples.length + 14 > limits.seriesBytes - limits.metadataBytes) {
          const oldest = series.samples.shift()!;
          series.sampleBytes -= bytes(oldest);
          this.gap(series, "retention", oldest.sequence, 1);
        }
      }
    } catch (error) {
      if (!live()) return;
      const code = error instanceof BridgeClientError ? error.details.code : error instanceof HistoryError ? error.code : "";
      if (code === "WORLD_MISMATCH") {
        this.gap(series, "worldChanged", flight.sequence, 1);
        this.stop(series, "worldChanged", false);
      } else this.gap(series, code === "" || code === "PROTOCOL_ERROR" ? "invalidSample" : "readError", flight.sequence, 1);
    }
  }
  private stop(series: Series, reason: string, recordFlight = true): void {
    if (series.status === "stopped") return;
    series.status = "stopped";
    series.stopReason = reason;
    series.generation++;
    if (series.flight) {
      if (recordFlight) this.gap(series, reason === "disposed" ? "disposed" : "stopped", series.flight.sequence, 1);
      series.flight.controller.abort();
    }
  }
  private gap(series: Series, reason: GapReason, fromSequence: number, count: number): void {
    series.gaps.push({reason, fromSequence, toSequence: fromSequence + count - 1, count});
    series.gaps.sort((a, b) => a.fromSequence - b.fromSequence);
    const merged: typeof series.gaps = [];
    for (const item of series.gaps) {
      const previous = merged.at(-1);
      if (previous && previous.reason === item.reason && previous.toSequence + 1 === item.fromSequence) {
        previous.toSequence = item.toSequence;
        previous.count += item.count;
      } else merged.push({...item});
    }
    series.gaps = merged;
    while (series.gaps.length > limits.gaps) {
      const oldest = series.gaps.shift()!;
      const previous = series.agedGaps[oldest.reason];
      series.agedGaps[oldest.reason] = {count: Math.min(Number.MAX_SAFE_INTEGER, (previous?.count ?? 0) + oldest.count),
        firstSequence: Math.min(previous?.firstSequence ?? oldest.fromSequence, oldest.fromSequence),
        lastSequence: Math.max(previous?.lastSequence ?? oldest.toSequence, oldest.toSequence)};
    }
  }
}
