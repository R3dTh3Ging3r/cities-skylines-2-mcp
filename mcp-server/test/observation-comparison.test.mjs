import test from 'node:test';
import assert from 'node:assert/strict';
import {compareStoredObservations} from '../dist/observation-comparison.js';
import {copy,spec,wire,observed} from './history-fixtures.mjs';
const ids=['00000000-0000-4000-8000-000000000001','00000000-0000-4000-8000-000000000002'];
const policy={mode:'rawNormalizedTime',phaseTolerance:.001,maxObservedPhaseStep:.2,frameDurationTolerance:0,maxSourceSpanFrames:3};
function series(id,base=100,kind='city'){
 const s=spec(kind),definitions=copy(wire.catalog.filter(d=>s.queries.some(q=>q.key===d.key)));
 return {id,spec:s,definitions,definitionRevision:'diagnosticMetrics.v1',status:'stopped',stopReason:'stopped',nextSequence:3,
 samples:[0,1].map(i=>({sequence:i+1,collectedAtUtc:'2026-10-16T12:00:00Z',observation:observed(base+i*10,.2+i*.1,kind)})),gaps:[],agedGaps:{}};
}
function setup(kind='city'){
 const rows=[series(ids[0],100,kind),series(ids[1],200,kind)];const request={baseline:{id:ids[0],fromSequence:1,toSequence:2},current:{id:ids[1],fromSequence:1,toSequence:2},cohort:copy(rows[0].spec.cohort),policy:copy(policy)};
 return {rows,request,run:()=>compareStoredObservations(request,id=>rows.find(s=>s.id===id))};
}
test('stored windows compare distinct absolute frames with qualified complete paired deltas',()=>{
 const {rows,run}=setup();rows[1].samples[0].observation.data.values['city.money'].value+=20;
 const result=run();assert.equal(result.status,'comparable');assert.ok(result.qualifications.includes('rawNormalizedOnly'));assert.equal(result.pairs.length,2);
 assert.equal(result.pairs[0].metrics['city.money'].delta,20);assert.equal(result.pairs[0].metrics['city.money'].improvement,null);
 assert.deepEqual(result.pairs[0].baselineEvidence.sources,rows[0].samples[0].observation.data.sources);
});
test('peak night pause wrap missing clocks source spans gaps and unavailable retained ranges refuse',()=>{
 for(const change of [
  s=>s.samples.forEach(p=>p.observation.data.clock.rawNormalizedTime+=.3),
  s=>s.samples[1].observation.data.clock.rawNormalizedTime=.2,
  s=>s.samples[1].observation.data.clock.rawNormalizedTime=.1,
  s=>s.samples[0].observation.data.clock=null,
  s=>s.samples[1].observation.data.clock.frameIndex=s.samples[0].observation.data.clock.frameIndex,
  s=>s.samples[0].observation.data.sources[0].endFrame+=10,
  s=>s.gaps=[{reason:'readError',fromSequence:1,toSequence:1,count:1}],
  s=>s.samples.shift(),s=>s.definitionRevision='other',
 ]){const {rows,run}=setup();change(rows[1]);assert.equal(run().status,'notComparable');}
});
test('fixed member versions and producer fingerprints cannot manufacture replacement gain',()=>{
 for(const change of [s=>s.spec.cohort.members[0].version++,s=>s.samples[1].observation.data.memberEvidence[0].fingerprint='a'.repeat(64),s=>s.samples[0].observation.data.memberEvidence[0].complete=false]){
 const {rows,run}=setup('line');assert.equal(run().status,'comparable');change(rows[1]);const result=run();assert.equal(result.status,'notComparable');assert.equal(result.improvement,null);
 }
});
test('zero baseline unavailable and overflow values never become improvement or infinity',()=>{
 const {rows,run}=setup();for(const p of rows[0].samples)p.observation.data.values['city.money'].value=0;
 assert.equal(run().pairs[0].metrics['city.money'].relativeChange,null);
 rows[0].samples[0].observation.data.values['city.money'].value=-Number.MAX_VALUE;rows[1].samples[0].observation.data.values['city.money'].value=Number.MAX_VALUE;
 assert.equal(run().status,'notComparable');
 const q=setup();q.rows[0].samples[0].observation.data.values['city.money'].value=null;q.rows[0].samples[0].observation.data.values['city.money'].support='unavailable';assert.equal(q.run().status,'notComparable');
});
test('unverified receipts and reversed caller ranges reject rather than clamp',()=>{
 const {request,rows}=setup();assert.throws(()=>compareStoredObservations({...request,reconciliationReceipts:[{}]},id=>rows.find(s=>s.id===id)));
 request.current.fromSequence=3;assert.throws(()=>compareStoredObservations(request,id=>rows.find(s=>s.id===id)));
});

test('State clock fallback is producer-declared and distinct calendar dates between windows are allowed',()=>{
 const {rows,run}=setup();
 for(const s of rows)for(const sample of s.samples){const o=sample.observation;const state=copy(o.data.sources[0]);state.id='state';o.data.sources.push(state);o.data.clock.sourceObservationId='state';}
 for(const sample of rows[1].samples){sample.observation.data.clock.rawNormalizedDate=.7;sample.observation.data.clock.rawYear+=1;}
 assert.equal(run().status,'comparable');
 rows[1].samples[1].observation.data.clock.rawDaysPerYear++;assert.equal(run().status,'notComparable');
});
test('definition source period scope frame tolerance and ambiguous phase evidence refuse',()=>{
 for(const change of [
  ({rows})=>rows[1].definitions[0].unit='unknown',
  ({rows})=>rows[1].definitions[0].period='hour',
  ({rows})=>rows[1].samples[1].observation.data.values['city.money'].source='other',
  ({rows})=>rows[1].samples[1].observation.data.clock.frameIndex+=2,
  ({request})=>{request.policy.phaseTolerance=.05;request.policy.maxObservedPhaseStep=.2;},
 ]){const fixture=setup();change(fixture);if(fixture.request.policy.phaseTolerance===.05)for(const s of fixture.rows)s.samples[1].observation.data.clock.rawNormalizedTime=.22;assert.equal(fixture.run().status,'notComparable');}
});
test('large complete paired comparison refuses output rather than dropping pairs',()=>{
 const {rows,request,run}=setup();
 for(const s of rows){s.samples=Array.from({length:512},(_,i)=>({sequence:i+1,collectedAtUtc:'2026-10-16T12:00:00Z',observation:observed((s.id===ids[0]?100:200)+i*10,.1+i*.001)}));s.nextSequence=513;}
 request.baseline.toSequence=512;request.current.toSequence=512;request.policy.phaseTolerance=.00001;
 assert.throws(run,error=>error.code==='RESULT_TOO_LARGE');
});
test('all result surfaces count UTF8 escaped wrappers and use a bounded error fallback',async()=>{
 const {historyResult,historyError}=await import('../dist/observation-history-results.js');
 for(const data of [{samples:['界'.repeat(100000)]},{pairs:['"'.repeat(100000)]},{status:'stopped',details:'x'.repeat(300000)}])assert.throws(()=>historyResult(data),e=>e.code==='RESULT_TOO_LARGE');
 const {HistoryError}=await import('../dist/observation-history-contracts.js');
 assert.ok(Buffer.byteLength(JSON.stringify(historyError(new HistoryError('RESULT_TOO_LARGE','request narrower ranges'))))<262144);
});
