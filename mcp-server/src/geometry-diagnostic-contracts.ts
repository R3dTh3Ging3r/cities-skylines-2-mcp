import {z} from "zod";
import {entityRefSchema} from "./construction-query.js";
import type {EntityRef} from "./contracts.js";

const point = z.object({x:z.number().finite(),y:z.number().finite(),z:z.number().finite()}).strict();
const reason = z.string().min(1).max(128).nullable();
const quantity = z.object({value:z.number().finite().nullable(),support:z.enum(["supported","unavailable"]),reason}).strict();
const sample = z.object({t:z.number().finite().min(0).max(1),position:point.nullable(),positionReason:reason,
    grade:quantity,curvature:quantity,radius:quantity}).strict();
const extremum = z.object({value:z.number().finite(),t:z.number().finite().min(0).max(1),position:point.nullable(),positionReason:reason}).strict();
const summary = z.object({supportedSamples:z.number().int().min(0).max(17),unavailableSamples:z.number().int().min(0).max(17),
    min:extremum.nullable(),max:extremum.nullable()}).strict();
const profile = z.object({entity:entityRefSchema,status:z.enum(["available","unavailable"]),reason,
    extremaCoverage:z.literal("returnedSamplesOnly"),samples:z.array(sample).max(17),
    grade:summary.nullable(),curvature:summary.nullable(),radius:summary.nullable()}).strict();
export const geometryDiagnosticSchema = z.object({mode:z.literal("sampledCubicV1"),coordinates:z.literal("nativeWorldXYZ"),
    verticalAxis:z.literal("Y"),direction:z.literal("aToDNotLaneDirection"),gradeUnit:z.literal("dimensionlessRiseRun"),
    curvatureUnit:z.literal("inverseNativeWorldCoordinate"),radiusUnit:z.literal("nativeWorldCoordinate"),
    physicalUnits:z.literal("unverified"),engineeringValidation:z.literal("unknown"),sampling:z.literal("uniformParameter"),
    betweenSamples:z.literal("unmeasured"),samplesPerCurve:z.literal(17),maxRoots:z.literal(32),maxEvaluations:z.literal(544),
    evaluations:z.number().int().min(0).max(544),profiles:z.array(profile).min(1).max(32)}).strict();
export type GeometryDiagnosticSnapshot = z.infer<typeof geometryDiagnosticSchema>;
type Root = {entity:EntityRef;unavailableReason:string|null;networkReason:string|null;
    network:{curve:{a:z.infer<typeof point>;d:z.infer<typeof point>}}|null};
const sameRef = (a:EntityRef,b:EntityRef) => a.world.bootId===b.world.bootId&&a.world.epoch===b.world.epoch&&a.index===b.index&&a.version===b.version;
const samePoint = (a:z.infer<typeof point>|null,b:z.infer<typeof point>|null) => a===null?b===null:b!==null&&a.x===b.x&&a.y===b.y&&a.z===b.z;
function invalid():never {throw new Error("invalid sampled cubic evidence");}
const reasons = {
    grade:new Set(["controlNormalizationLostPrecision","zeroHorizontalDerivative","arithmeticOverflow","arithmeticUnderflow"]),
    curvature:new Set(["controlNormalizationLostPrecision","stationaryDerivative","derivativeArithmeticUnavailable","arithmeticOverflow","arithmeticUnderflow"]),
    radius:new Set(["controlNormalizationLostPrecision","stationaryDerivative","derivativeArithmeticUnavailable","arithmeticOverflow","arithmeticUnderflow","zeroCurvatureUnboundedRadius"]),
};
/** Validate returned evidence only. The analytic geometry algorithm lives in C#. */
export function checkGeometryDiagnostics(data:GeometryDiagnosticSnapshot,roots:Root[]):void {
    if(data.profiles.length!==roots.length)invalid();
    let evaluations=0;
    data.profiles.forEach((p,index)=>{
        const root=roots[index];
        if(!sameRef(p.entity,root.entity))invalid();
        if(root.network===null){
            if(p.status!=="unavailable"||p.reason!==(root.unavailableReason??root.networkReason??"networkCurveUnavailable")||
                p.samples.length!==0||p.grade!==null||p.curvature!==null||p.radius!==null)invalid();
            return;
        }
        if(p.status!=="available"||p.reason!==null||p.samples.length!==17)invalid();
        evaluations+=17;
        p.samples.forEach((row,i)=>{
            if(row.t!==i/16||(row.position===null)!==(row.positionReason!==null))invalid();
            if(row.positionReason!==null&&row.positionReason!=="positionArithmeticUnavailable")invalid();
            for(const name of ["grade","curvature","radius"] as const){
                const q=row[name];
                if((q.value===null)!==(q.support==="unavailable")||(q.value===null)!==(q.reason!==null)||
                    q.reason!==null&&!reasons[name].has(q.reason)||name!=="grade"&&q.value!==null&&q.value<0||
                    name==="radius"&&q.value===0)invalid();
            }
            if(row.curvature.value===0&&(row.radius.value!==null||row.radius.reason!=="zeroCurvatureUnboundedRadius"))invalid();
            if(row.radius.reason==="zeroCurvatureUnboundedRadius"&&row.curvature.value!==0)invalid();
        });
        if(!samePoint(p.samples[0].position,root.network.curve.a)||!samePoint(p.samples[16].position,root.network.curve.d))invalid();
        for(const name of ["grade","curvature","radius"] as const){
            const s=p[name];if(!s)invalid();
            const known=p.samples.filter(row=>row[name].value!==null);
            if(s.supportedSamples!==known.length||s.unavailableSamples!==17-known.length)invalid();
            if(known.length===0){if(s.min!==null||s.max!==null)invalid();continue;}
            // Earliest equal extremum wins; no continuous extrema calculation here.
            const min=known.reduce((a,b)=>b[name].value!<a[name].value!?b:a);
            const max=known.reduce((a,b)=>b[name].value!>a[name].value!?b:a);
            for(const [ext,row] of [[s.min,min],[s.max,max]] as const)
                if(!ext||ext.value!==row[name].value||ext.t!==row.t||!samePoint(ext.position,row.position)||ext.positionReason!==row.positionReason)invalid();
        }
    });
    if(data.evaluations!==evaluations)invalid();
}
