import {constructionPageSchema,checkConstructionPage} from "./construction-page-contracts.js";
import {geometryDiagnosticSchema,checkGeometryDiagnostics} from "./geometry-diagnostic-contracts.js";
import { z } from "zod";
import { constructionRequestSchema, entityRefSchema, worldRefSchema, type ConstructionRequest } from "./construction-query.js";
const text=z.string().max(128), reason=text.nullable();
const frame=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const point=z.object({x:z.number().finite(),y:z.number().finite(),z:z.number().finite()}).strict();
const bounds=z.object({center:point,halfExtents:point,axisX:point,axisY:point,axisZ:point,scope:z.literal("rootGeometryOnly")}).strict()
  .superRefine((value,context)=>{
    const axes=[value.axisX,value.axisY,value.axisZ];
    const dot=(a:z.infer<typeof point>,b:z.infer<typeof point>)=>a.x*b.x+a.y*b.y+a.z*b.z;
    if(Object.values(value.halfExtents).some(v=>v<0)||axes.some(axis=>Math.abs(dot(axis,axis)-1)>.001)||
      Math.abs(dot(axes[0],axes[1]))>.001||Math.abs(dot(axes[0],axes[2]))>.001||Math.abs(dot(axes[1],axes[2]))>.001)
      context.addIssue({code:"custom",message:"invalid oriented geometry"});
  });
const network=z.object({curve:z.object({a:point,b:point,c:point,d:point}).strict(),start:entityRefSchema,end:entityRefSchema,
  length:z.number().finite().nonnegative(),direction:z.literal("startToEndNotLaneDirection"),coordinates:z.literal("worldXYZ"),
  width:z.number().finite().nonnegative().nullable(),widthReason:reason,requiredLayers:reason,connectLayers:reason,layersReason:reason,
  elevationMode:z.null(),elevationModeReason:text,laneDirections:z.null(),laneDirectionsReason:text}).strict();
const geometryFields={entity:entityRefSchema,kind:z.enum(["networkEdge","building"]).nullable(),unavailableReason:reason,
  assetKey:z.string().max(256).nullable(),assetKeyReason:reason,network:network.nullable(),networkReason:reason,
  rootBounds:bounds.nullable(),rootBoundsReason:reason};
const target=z.object(geometryFields).strict();
const entity=z.object({...geometryFields,owner:entityRefSchema.nullable(),ownerReason:reason,
  family:z.null(),familyReason:text,compatibleFamilies:z.null(),compatibleFamiliesReason:text,
  installedUpgrades:z.null(),installedUpgradesReason:text,facilityEnvelope:z.null(),facilityEnvelopeReason:text,
  childEnvelope:z.null(),childEnvelopeReason:text,platformEnvelope:z.null(),platformEnvelopeReason:text,
  approachEnvelope:z.null(),approachEnvelopeReason:text,connectors:z.null(),connectorsReason:text,
  costs:z.null(),costsReason:text,prerequisites:z.null(),prerequisitesReason:text,capacityEffects:z.null(),capacityEffectsReason:text}).strict();
export const relationshipKinds=["owner","attached","subObject","installedUpgrade","subNet","subArea"] as const;
const kind=z.enum(relationshipKinds);
const count=z.number().int().min(0).max(128);
const source=z.object({source:entityRefSchema,kind,present:z.boolean().nullable(),status:z.enum(["present","absent","unavailable"]),observedCount:count.nullable(),
  visitedCount:count,coverageComplete:z.boolean(),reason}).strict();
const raw=z.number().int().min(-2147483648).max(2147483647).nullable();
const link=z.object({source:entityRefSchema,kind,slot:count,target:entityRefSchema.nullable(),rawIndex:raw,rawVersion:raw,
  targetIndex:count.nullable(),status:z.enum(["available","unavailable"]),reason}).strict();
const relationshipSnapshot=z.object({scope:z.literal("directDepth1"),coverage:z.literal("directSourceSlotsOnly"),
  limits:z.object({maxRoots:z.literal(32),maxSlots:z.literal(128),maxAdditionalTargets:z.literal(128)}).strict(),
  rootReads:z.number().int().min(1).max(32),headerReads:z.number().int().min(1).max(32),visitedSlots:count,additionalTargetReads:count,
  sources:z.array(source).min(6).max(192),links:z.array(link).max(128),targets:z.array(target).max(128),nativeVerification:z.literal("CON-01:notRun")}).strict();
export type ConstructionRelationshipSnapshot=z.infer<typeof relationshipSnapshot>;
export const constructionObservationSchema=z.object({data:z.object({scope:z.literal("explicitEntities"),
  requestedEntities:z.array(entityRefSchema).min(1).max(32),entities:z.array(entity).min(1).max(32),worldBounds:z.null(),
  worldBoundsReason:z.literal("notMeasured"),nextCursor:z.null(),truncated:z.literal(false),support:z.literal("partial"),
  nativeVerification:z.literal("CON-01:notRun"),relationshipPage:constructionPageSchema.nullable().optional(),geometryDiagnostics:geometryDiagnosticSchema.nullable().optional(),relationships:relationshipSnapshot.nullable().optional()}).strict(),
  world:worldRefSchema,sampledAtUtc:z.string().max(64).datetime(),startFrame:frame,endFrame:frame,
  freshness:z.enum(["current","warming","unavailable"]),warnings:z.array(text).max(32)}).strict();
const sameWorld=(a:z.infer<typeof worldRefSchema>,b:z.infer<typeof worldRefSchema>)=>a.bootId===b.bootId&&a.epoch===b.epoch;
const sameRef=(a:z.infer<typeof entityRefSchema>,b:z.infer<typeof entityRefSchema>)=>sameWorld(a.world,b.world)&&a.index===b.index&&a.version===b.version;
const key=(ref:z.infer<typeof entityRefSchema>)=>`${ref.index}:${ref.version}`;
function invalid():never{throw new Error("invalid inventory identity geometry or relationship evidence");}
function checkGeometry(row:z.infer<typeof target>,world:z.infer<typeof worldRefSchema>) {
  if(!sameWorld(row.entity.world,world)||(row.kind===null)!==(row.unavailableReason!==null)||
    (row.network!==null&&row.kind!=="networkEdge")||(row.rootBounds!==null&&row.kind===null))invalid();
  if((row.network===null)!==(row.networkReason!==null)||(row.rootBounds===null)!==(row.rootBoundsReason!==null)||
    (row.assetKey===null)!==(row.assetKeyReason!==null))invalid();
  if(row.network){const n=row.network;
    if(!sameWorld(n.start.world,world)||!sameWorld(n.end.world,world)||(n.width===null)!==(n.widthReason!==null)||
      (n.layersReason===null)!==(n.requiredLayers!==null&&n.connectLayers!==null))invalid();
  }
}
/** Shared direct/O7 semantic validator. No projection or missing-value substitution. */
export function parseConstructionObservation(value:unknown,input:ConstructionRequest){
  const request=constructionRequestSchema.parse(input),observed=constructionObservationSchema.parse(value),data=observed.data;
  if(!sameWorld(observed.world,request.world)||observed.endFrame<observed.startFrame||
    observed.endFrame!==observed.startFrame&&!observed.warnings.includes("multiFrameReadNotAtomic")||
    !["partialExplicitEntityInventory","rootGeometryOnly","nativeConstructionFixtureNotRun"].every(w=>observed.warnings.includes(w))||
    data.entities.length!==request.entities.length||data.requestedEntities.length!==request.entities.length)invalid();
  data.entities.forEach((row,index)=>{
    if(!sameRef(row.entity,request.entities[index])||!sameRef(data.requestedEntities[index],request.entities[index]))invalid();
    checkGeometry(row,request.world);if(row.owner&&!sameWorld(row.owner.world,request.world))invalid();
    if((row.owner===null)!==(row.ownerReason!==null))invalid();
  });
  if(request.relationshipPage){
    if(!data.relationshipPage||!observed.warnings.includes("relationshipPagesNotAtomicSnapshot"))invalid();
    checkConstructionPage(data.relationshipPage,request);
  }else if(data.relationshipPage!=null)invalid();
  const diagnostics=data.geometryDiagnostics;
  if(request.geometryDiagnostics===undefined){if(diagnostics!=null)invalid();}
  else {
    if(!diagnostics||!observed.warnings.includes("computedGeometrySamplesNotEngineeringValidation"))invalid();
    checkGeometryDiagnostics(diagnostics,data.entities);
  }
  const r=data.relationships;
  if(request.relationships===undefined){if(r!=null)invalid();return observed;}
  if(!r||!observed.warnings.includes("directRelationshipsNotCompleteFacilityGeometry")||r.rootReads!==request.entities.length||
    r.headerReads!==request.entities.length||r.sources.length!==request.entities.length*6||r.visitedSlots!==r.links.length)invalid();
  const targets=new Map<string,number>();r.targets.forEach((row,index)=>{
    checkGeometry(row,request.world);if(targets.has(key(row.entity)))invalid();targets.set(key(row.entity),index);
    const root=data.entities.find(e=>sameRef(e.entity,row.entity));
    if(root&&Object.keys(geometryFields).some(field=>JSON.stringify(root[field as keyof typeof root])!==JSON.stringify(row[field as keyof typeof row])))invalid();
  });
  if(r.additionalTargetReads!==r.targets.filter(t=>!request.entities.some(e=>sameRef(e,t.entity))).length)invalid();
  let cursor=0;const firstSeen=new Set<number>();
  r.sources.forEach((s,index)=>{
    if(!sameRef(s.source,request.entities[Math.floor(index/6)])||s.kind!==relationshipKinds[index%6]||
      s.status!==(s.present===true?"present":s.present===false?"absent":"unavailable"))invalid();
    if(s.present!==true){if(s.observedCount!==null||s.visitedCount!==0||s.coverageComplete||!s.reason)invalid();return;}
    if(s.reason!==null||s.observedCount===null||!s.coverageComplete||s.visitedCount!==s.observedCount||index%6<2&&s.observedCount!==1)invalid();
    for(let slot=0;slot<s.observedCount;slot++){
      const l=r.links[cursor++];if(!l||!sameRef(l.source,s.source)||l.kind!==s.kind||l.slot!==slot)invalid();
      if(l.target===null){
        if(l.targetIndex!==null||l.status!=="unavailable"||!["nullReference","invalidNativeReference"].includes(l.reason??""))invalid();
        if(l.reason==="nullReference"&&(l.rawIndex!==null||l.rawVersion!==null))invalid();
        if(l.reason==="invalidNativeReference"&&(l.rawIndex===null||l.rawVersion===null||l.rawIndex>0&&l.rawVersion>0))invalid();
      }else{
        if(l.targetIndex===null||l.rawIndex!==null||l.rawVersion!==null||!sameWorld(l.target.world,request.world))invalid();
        const t=r.targets[l.targetIndex];if(!t||!sameRef(t.entity,l.target)||l.reason!==t.unavailableReason||l.status!==(t.unavailableReason===null?"available":"unavailable"))invalid();
        if(!firstSeen.has(l.targetIndex)){if(l.targetIndex!==firstSeen.size)invalid();firstSeen.add(l.targetIndex);}
      }
    }
  });
  if(cursor!==r.links.length||firstSeen.size!==r.targets.length)invalid();
  return observed;
}
