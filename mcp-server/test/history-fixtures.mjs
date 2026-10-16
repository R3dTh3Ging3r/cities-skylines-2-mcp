import {readFileSync} from 'node:fs';
export const wire=JSON.parse(readFileSync(new URL('./fixtures/diagnostic-metrics-wire.json',import.meta.url),'utf8'));
export const copy=value=>structuredClone(value);
export const spec=(kind='city')=>{
 const request=copy(wire[kind==='city'?'city-request':'line-request']);
 return {...request,cohort:{kind:kind==='city'?'city':'fixedSelectedLine',world:copy(request.world),members:kind==='city'?[]:[copy(request.scope.line)]},maxSamples:512,intervalMs:1000};
};
export function observed(frame=100,phase=.2,kind='city'){
 const row=copy(wire[kind==='city'?'city':'line']);
 // Controlled clock fixtures preserve the real wire shape; native calendar parity is not claimed.
 if(kind==='line'){row.data.clock=copy(wire.city.data.clock);row.data.clock.sourceObservationId='state';row.warnings=row.warnings.filter(w=>w!=='rawNormalizedClockUnavailable');}
 row.startFrame=frame;row.endFrame=frame+2;row.warnings=[...new Set([...row.warnings,'multiFrameReadNotAtomic'])];
 for(const source of row.data.sources){source.startFrame=frame;source.endFrame=frame+2;}
 if(row.data.clock)Object.assign(row.data.clock,{frameIndex:frame+2,startFrame:frame,endFrame:frame+2,rawNormalizedTime:phase,rawNormalizedDate:.2});
 return row;
}
export const group=(request,row)=>({world:copy(request.expectedWorld),startFrame:row.startFrame,endFrame:row.endFrame,worldChanged:false,results:[{id:request.items[0].id,observation:row}]});
export const deferred=()=>{let resolve,reject;const promise=new Promise((a,b)=>{resolve=a;reject=b;});return {promise,resolve,reject};};
export async function flush(){for(let i=0;i<15;i++)await new Promise(resolve=>setImmediate(resolve));}
export class Clock{
 time=0;timers=new Map();next=0;nowMs=()=>this.time;
 setTimeout=(callback,ms)=>{const id=++this.next;this.timers.set(id,{at:this.time+ms,callback});return id;};
 clearTimeout=id=>this.timers.delete(id);
 async advance(ms){this.time+=ms;for(const [id,timer]of [...this.timers])if(timer.at<=this.time){this.timers.delete(id);timer.callback();}await flush();}
}
