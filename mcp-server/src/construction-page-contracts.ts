import {z} from "zod";
import {entityRefSchema,relationshipKinds,type ConstructionRequest} from "./construction-query.js";
const count=z.number().int().min(0).max(2147483647), visited=z.number().int().min(0).max(32);
const reason=z.string().min(1).max(128).nullable();
const kind=z.enum(relationshipKinds);
const source=z.object({source:entityRefSchema,kind,present:z.boolean().nullable(),status:z.enum(["present","absent","unavailable"]),
  observedCount:count.nullable(),visitedCount:visited,coverageComplete:z.boolean(),reason}).strict();
const raw=z.number().int().min(-2147483648).max(2147483647).nullable();
const link=z.object({source:entityRefSchema,kind,slot:count,target:entityRefSchema.nullable(),rawIndex:raw,rawVersion:raw,
  status:z.enum(["referenceObserved","unavailable"]),reason:z.enum(["nullReference","invalidNativeReference"]).nullable()}).strict();
export const constructionPageSchema=z.object({root:entityRefSchema,kind,offset:count,limit:z.number().int().min(1).max(32),
  observedCount:count.nullable(),endObservedCount:count.nullable(),returnedCount:visited,visitedSlots:visited,
  endExclusive:count,nextOffset:count.nullable(),windowComplete:z.boolean(),sourceCoverage:z.enum(["complete","partial","absent","unavailable"]),
  targetResolution:z.literal("notRequested"),consistency:z.literal("livePageNotSnapshot"),crossPageConsistency:z.literal("unverified"),
  rootReads:z.literal(1),headerReads:z.literal(2),additionalTargetReads:z.literal(0),maxSlots:z.literal(32),
  sources:z.array(source).length(6),links:z.array(link).max(32)}).strict();
export type ConstructionPageSnapshot=z.infer<typeof constructionPageSchema>;
const same=(a:z.infer<typeof entityRefSchema>,b:z.infer<typeof entityRefSchema>)=>a.world.bootId===b.world.bootId&&a.world.epoch===b.world.epoch&&a.index===b.index&&a.version===b.version;
function invalid():never{throw new Error("invalid live relationship page evidence");}
/** Validate a live window; never aggregate separate pages into a stable snapshot. */
export function checkConstructionPage(page:ConstructionPageSnapshot,request:ConstructionRequest):void {
  const expected=request.relationshipPage;if(!expected||request.entities.length!==1)invalid();
  if(!same(page.root,request.entities[0])||page.kind!==expected.kind||page.offset!==expected.offset||page.limit!==expected.limit||
    page.returnedCount!==page.links.length||page.visitedSlots!==page.links.length||page.endObservedCount!==page.observedCount)invalid();
  let selected:typeof page.sources[number]|undefined;
  page.sources.forEach((s,index)=>{
    if(!same(s.source,page.root)||s.kind!==relationshipKinds[index]||s.status!==(s.present===true?"present":s.present===false?"absent":"unavailable"))invalid();
    if(s.present===true){if(s.observedCount===null||s.reason!==null||index<2&&s.observedCount!==1)invalid();}
    else if(s.observedCount!==null||s.reason===null||s.visitedCount!==0||s.coverageComplete)invalid();
    if(s.kind===page.kind)selected=s;
    else if(s.visitedCount!==0||s.coverageComplete)invalid();
  });
  if(!selected||selected.observedCount!==page.observedCount)invalid();
  const s=selected;
  if(s.present!==true){
    if(page.offset!==0||page.endExclusive!==0||page.links.length!==0||page.nextOffset!==null||page.windowComplete||
      page.sourceCoverage!==(s.present===false?"absent":"unavailable"))invalid();
    return;
  }
  const n=s.observedCount!;
  const end=Math.min(n,page.offset+page.limit),complete=page.offset===0&&end===n;
  if(page.offset>n||page.endExclusive!==end||page.links.length!==end-page.offset||!page.windowComplete||
    page.nextOffset!==(end<n?end:null)||page.sourceCoverage!==(complete?"complete":"partial")||
    s.visitedCount!==page.links.length||s.coverageComplete!==complete)invalid();
  page.links.forEach((l,index)=>{
    if(!same(l.source,page.root)||l.kind!==page.kind||l.slot!==page.offset+index)invalid();
    if(l.target===null){
      if(l.status!=="unavailable"||l.reason===null)invalid();
      if(l.reason==="nullReference"&&(l.rawIndex!==null||l.rawVersion!==null))invalid();
      if(l.reason==="invalidNativeReference"&&(l.rawIndex===null||l.rawVersion===null||l.rawIndex>0&&l.rawVersion>0))invalid();
    }else if(l.target.world.bootId!==request.world.bootId||l.target.world.epoch!==request.world.epoch||l.status!=="referenceObserved"||
      l.reason!==null||l.rawIndex!==null||l.rawVersion!==null)invalid();
  });
}
