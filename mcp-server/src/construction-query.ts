import { z } from "zod";
import type { EntityRef, WorldRef } from "./contracts.js";

const worldSchema=z.object({bootId:z.string().min(1).max(128).refine(s=>s.trim().length>0),epoch:z.number().int().min(0).max(Number.MAX_SAFE_INTEGER)}).strict();
const entitySchema=z.object({world:worldSchema,index:z.number().int().min(1).max(2147483647),version:z.number().int().min(1).max(2147483647)}).strict();
export interface ConstructionRequest { world:WorldRef; entities:EntityRef[]; }
export const constructionInputSchema={world:worldSchema,entities:z.array(entitySchema).min(1).max(32),
  spatial:z.never().optional(),cursor:z.never().optional(),includeFamilies:z.never().optional(),limit:z.never().optional()};
export const constructionRequestSchema=z.object(constructionInputSchema).strict().superRefine((request,ctx)=>{
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
    entities:request.entities.map(e=>`${e.index}:${e.version}`).join(",")});
}
