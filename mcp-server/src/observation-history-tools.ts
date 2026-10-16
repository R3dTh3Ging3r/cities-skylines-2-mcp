import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { ObservationHistory } from "./observation-history.js";
import { comparisonSchema, idSchema, readHistorySchema, seriesSpecSchema } from "./observation-history-contracts.js";
import { historyError, historyResult } from "./observation-history-results.js";

export const historyToolNames = ["cs2_collect_observations", "cs2_stop_observation_collection",
  "cs2_read_observation_history", "cs2_compare_observations"] as const;

// SDK validation happens before our handler. Its default Zod diagnostics echo
// unknown property names, which can themselves exceed the entire result budget.
// Keep an actual object schema for SDK discovery. Invalid fields become a private
// sentinel rejected by one bounded issue. Unlike a throwing catch callback, this
// lets the JSON-schema converter probe undefined without losing requiredness.
// Original optional/default behavior and nested schemas remain intact; the store
// still checks cross-field invariants before admission/reads.
function boundedInput<S extends z.ZodRawShape>(schema: z.ZodObject<S>) {
  const invalid = Symbol("invalid history input");
  const fields = Object.fromEntries(Object.entries(schema.shape).map(([name, field]) =>
    [name, field.catch(() => invalid as never).refine(value => value !== invalid,
      "Invalid bounded history arguments")])) as {[K in keyof S]: z.ZodEffects<z.ZodCatch<S[K]>>};
  return z.object(fields).strict("Invalid bounded history arguments");
}

export function registerHistoryTools(registerTool: McpServer["registerTool"], history: ObservationHistory): void {
  registerTool("cs2_collect_observations", {
    title: "Collect bounded diagnostic observations",
    description: "Start process-local collection for an explicit world and fixed city or versioned selected-line cohort. " +
      "Eight retained histories including stopped,512 samples maximum; raw observations, no native history or simulation controls.",
    inputSchema: boundedInput(seriesSpecSchema.innerType()),
  }, async (request, extra) => {
    try {return historyResult(await history.startCollection(request, {signal: extra.signal}));}
    catch (error) {return historyError(error);}
  });
  registerTool("cs2_stop_observation_collection", {
    title: "Stop diagnostic observation collection",
    description: "Idempotently abort pending collection and retain its history. Returns bounded status and gap summary; use paged history reads for samples.",
    inputSchema: boundedInput(z.object({id: idSchema}).strict()),
  }, async ({id}) => {
    try {return historyResult(history.stopCollection(id));} catch (error) {return historyError(error);}
  });
  registerTool("cs2_read_observation_history", {
    title: "Read collected diagnostic history",
    description: "Read1–128 retained samples under256KiB including provenance/gaps. afterSequence is exclusive; " +
      "continue with afterSequence=nextSequence-1 when nextSequence is non-null. No native history or persistence.",
    inputSchema: boundedInput(readHistorySchema),
  }, async ({id, afterSequence, limit}) => {
    try {return historyResult(history.readSeries(id, {afterSequence, limit}));} catch (error) {return historyError(error);}
  });
  registerTool("cs2_compare_observations", {
    title: "Compare retained diagnostic observation ranges",
    description: "Compare explicit retained ranges with fixed cohorts and aligned raw normalized-time evidence. " +
      "Qualified deltas only, improvement remains null. No interpolation, rate integration or reconciliation receipts. " +
      "Results over256KiB require narrower ranges.",
    inputSchema: boundedInput(comparisonSchema),
  }, async request => {
    try {return historyResult(history.compareObservations(request));} catch (error) {return historyError(error);}
  });
}
