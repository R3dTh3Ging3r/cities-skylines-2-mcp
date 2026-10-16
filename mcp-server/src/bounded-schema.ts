import { z } from "zod";

/** Preserve discoverable object fields/requiredness while bounding SDK validation diagnostics. */
export function boundedObjectInput<S extends z.ZodRawShape>(schema: z.ZodObject<S>) {
  const invalid = Symbol("invalid bounded input");
  const fields = Object.fromEntries(Object.entries(schema.shape).map(([name, field]) =>
    [name, field.catch(() => invalid as never).refine(value => value !== invalid,
      "Invalid bounded arguments")])) as {[K in keyof S]: z.ZodEffects<z.ZodCatch<S[K]>>};
  return z.object(fields).strict("Invalid bounded arguments");
}
