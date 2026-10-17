import { z } from "zod";
import type { EntityRef, WorldRef } from "./contracts.js";

export const worldRefSchema=z.object({bootId:z.string().min(1).max(128).refine(s=>s.trim().length>0),epoch:z.number().int().min(0).max(Number.MAX_SAFE_INTEGER)}).strict();
export const entityRefSchema=z.object({world:worldRefSchema,index:z.number().int().min(1).max(2147483647),version:z.number().int().min(1).max(2147483647)}).strict();
export const relationshipKinds=["owner","attached","subObject","installedUpgrade","subNet","subArea"] as const;
export const relationshipPageQuerySchema=z.object({kind:z.enum(relationshipKinds),offset:z.number().int().min(0).max(2147483647),limit:z.number().int().min(1).max(32)}).strict();
export interface ConstructionRequest { world:WorldRef; entities:EntityRef[]; relationshipPage?:z.infer<typeof relationshipPageQuerySchema>; relationships?:"directDepth1"; geometryDiagnostics?:"sampledCubicV1"; }
const constructionInputSchema={relationshipPage:relationshipPageQuerySchema.optional(),world:worldRefSchema,entities:z.array(entityRefSchema).min(1).max(32),
  relationships:z.literal("directDepth1").optional(),geometryDiagnostics:z.literal("sampledCubicV1").optional()};
export const constructionObjectSchema=z.object(constructionInputSchema).strict();
export const constructionRequestSchema=constructionObjectSchema.superRefine((request,ctx)=>{
  if(request.relationshipPage&&(request.entities.length!==1||request.relationships!==undefined))ctx.addIssue({code:z.ZodIssueCode.custom,path:["relationshipPage"],message:"page requires one root without directDepth1"});
  const seen=new Set<number>();
  for(const [i,entity] of request.entities.entries()) {
    if(entity.world.bootId!==request.world.bootId||entity.world.epoch!==request.world.epoch)
      ctx.addIssue({code:z.ZodIssueCode.custom,path:["entities",i,"world"],message:"entity must belong to the observed request world"});
    if(seen.has(entity.index))ctx.addIssue({code:z.ZodIssueCode.custom,path:["entities",i],message:"duplicate entity index"});
    seen.add(entity.index);
  }
});
export function constructionQuery(input:ConstructionRequest):URLSearchParams {
  const request=constructionRequestSchema.parse(input);
  return new URLSearchParams({bootId:request.world.bootId,epoch:String(request.world.epoch),
    entities:request.entities.map(e=>`${e.index}:${e.version}`).join(","),
    ...(request.relationshipPage?{relationshipKind:request.relationshipPage.kind,relationshipOffset:String(request.relationshipPage.offset),relationshipLimit:String(request.relationshipPage.limit)}:{}),
    ...(request.relationships?{relationships:request.relationships}:{}),
    ...(request.geometryDiagnostics?{geometryDiagnostics:request.geometryDiagnostics}:{})});
}
