import test from 'node:test';
import assert from 'node:assert/strict';
import {createBridgeClient} from '../dist/bridge-client.js';
import {ObservationHistory} from '../dist/observation-history.js';
import {Clock,copy,wire,spec,observed,group,deferred,flush} from './history-fixtures.mjs';

test('collector uses actual typed catalog and O7 transport and retains source evidence',async()=>{
 const clock=new Clock(),calls=[];
 const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1',fetchImpl:async(url,options)=>{
 calls.push({url,options});if(url.includes('/catalog'))return Response.json(wire.catalog);
 const request=JSON.parse(options.body);assert.equal(request.items.length,1);assert.equal(request.items[0].name,'metrics.read');assert.deepEqual(request.items[0].args.world,request.expectedWorld);
 return Response.json(group(request,observed()));
 }});
 const history=new ObservationHistory(bridge,{clock});
 try{const started=await history.startCollection(spec());await flush();const page=history.readSeries(started.id,{});
 assert.equal(page.samples.length,1);assert.deepEqual(page.samples[0].observation,observed());assert.equal(calls.length,2);
 assert.equal(history.stopCollection(started.id).status,'stopped');assert.equal(history.stopCollection(started.id).sampleCount,1);
 }finally{history.dispose();}assert.equal(clock.timers.size,0);
});

test('eight pending admissions reserve capacity and disposal aborts real catalog request',async()=>{
 const pending=[];const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1',fetchImpl:(_url,options)=>new Promise((_resolve,reject)=>{
 pending.push(options.signal);options.signal.addEventListener('abort',()=>reject(new Error('aborted')),{once:true});
 })});
 const history=new ObservationHistory(bridge,{clock:new Clock()});
 const starts=Array.from({length:8},()=>history.startCollection(spec()));const settled=Promise.allSettled(starts);
 await flush();assert.equal(pending.length,2);await assert.rejects(()=>history.startCollection(spec()),e=>e.code==='SERIES_CAPACITY');
 history.dispose();assert.ok(pending.every(s=>s.aborted));assert.ok((await settled).every(r=>r.status==='rejected'));
});

test('stopped late poll holds its permit until settlement and cannot append',async()=>{
 const clock=new Clock(),polls=[];
 const bridge={metricCatalog:async()=>copy(wire.catalog),observe:(request,{signal})=>{const d=deferred();polls.push({request,signal,...d});return d.promise;}};
 const history=new ObservationHistory(bridge,{clock});
 try{
 const a=await history.startCollection(spec());await flush();const b=await history.startCollection(spec());await flush();
 assert.equal(polls.length,2);history.stopCollection(a.id);assert.equal(polls[0].signal.aborted,true);
 const cPromise=history.startCollection(spec());await flush();assert.equal(polls.length,2);
 polls[0].resolve(group(polls[0].request,observed()));const c=await cPromise;await flush();assert.equal(polls.length,3);
 assert.equal(history.readSeries(a.id,{}).samples.length,0);assert.equal(history.readSeries(a.id,{}).gaps.length,1);
 assert.equal(history.stopCollection(b.id).status,'stopped');history.stopCollection(c.id);
 polls[1].resolve(group(polls[1].request,observed()));polls[2].resolve(group(polls[2].request,observed()));await flush();
 }finally{history.dispose();}
});

test('monotonic scheduler is fair with global2, records backpressure and skips elapsed intervals',async()=>{
 const clock=new Clock(),polls=[];let utc='2026-10-16T12:00:00Z';
 const bridge={metricCatalog:async()=>copy(wire.catalog),observe:async(request)=>{polls.push(request);return group(request,observed(100+polls.length*10));}};
 const history=new ObservationHistory(bridge,{clock,utcNow:()=>utc});
 try{
 const ids=[];for(let i=0;i<3;i++)ids.push((await history.startCollection(spec())).id);await flush();
 assert.equal(polls.length,3);utc='2020-01-01T00:00:00Z';await clock.advance(5000);
 assert.equal(polls.length,5,'a late wake must not burst catch-up reads or exceed two grants');
 await clock.advance(1000);assert.equal(polls.length,7);
 for(const id of ids){const page=history.readSeries(id,{});assert.ok(page.samples.length>=2);assert.ok(page.gaps.some(g=>g.reason==='missedInterval'||g.reason==='backpressure'));}
 assert.equal(clock.timers.size,1);
 }finally{history.dispose();}
});

test('count retention and sequence-gap continuation neither duplicate nor skip stored samples',async()=>{
 const clock=new Clock();let count=0;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>{
 const n=++count;if(n===2)throw new Error('ordinary read');return group(request,observed(100+n*10));
 }},{clock});
 try{
 const started=await history.startCollection({...spec(),maxSamples:3});await flush();for(let i=0;i<4;i++)await clock.advance(1000);
 let afterSequence;const sequences=[];
 do{const page=history.readSeries(started.id,{afterSequence,limit:1});sequences.push(...page.samples.map(s=>s.sequence));if(page.nextSequence===null)break;afterSequence=page.nextSequence-1;}while(true);
 assert.deepEqual(sequences,[3,4,5]);const page=history.readSeries(started.id,{});assert.ok(page.gaps.some(g=>g.reason==='retention'&&g.fromSequence===1));assert.ok(page.gaps.some(g=>g.reason==='invalidSample'&&g.fromSequence===2));
 assert.equal(history.stopCollection(started.id).samples,undefined);
 }finally{history.dispose();}
});

test('512-sample limit and ring byte accounting include wrapper metadata',async()=>{
 const clock=new Clock();let frame=100;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>group(request,observed(frame++))},{clock});
 try{const start=await history.startCollection(spec());await flush();for(let i=0;i<514;i++)await clock.advance(1000);
 const status=history.stopCollection(start.id);assert.equal(status.sampleCount,512);assert.equal(status.firstSequence,4);assert.ok(status.retainedBytes<=1048576);assert.ok(status.gaps.some(g=>g.reason==='retention'&&g.count===3));
 }finally{history.dispose();}
});

test('UTF8 sample/series/page budgets and oversized samples are actual serialized limits',async()=>{
 const clock=new Clock();let frame=100,oversized=false;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>{
 const row=observed(frame++);if(oversized)row.sampledAtUtc='2026-10-16T00:00:00.'+'1'.repeat(140000)+'Z';
 else {row.warnings=[...Array.from({length:31},()=> '界'.repeat(128)),'multiFrameReadNotAtomic'];for(const source of row.data.sources)source.warnings=copy(row.warnings);}
 return group(request,row);
 }},{clock});
 try{const start=await history.startCollection(spec());await flush();for(let i=0;i<65;i++)await clock.advance(1000);
 const status=history.stopCollection(start.id);assert.ok(status.sampleCount>0&&status.sampleCount<66);assert.ok(status.retainedBytes<=1048576);
 let afterSequence;const sequences=[];
 do{const page=history.readSeries(start.id,{afterSequence});const {historyResult}=await import('../dist/observation-history-results.js');assert.ok(Buffer.byteLength(JSON.stringify(historyResult(page)))<=262144);sequences.push(...page.samples.map(s=>s.sequence));if(page.nextSequence===null)break;afterSequence=page.nextSequence-1;}while(true);
 assert.equal(sequences.length,status.sampleCount);assert.equal(new Set(sequences).size,status.sampleCount);
 oversized=true;const next=await history.startCollection(spec());await flush();const page=history.readSeries(next.id,{});assert.equal(page.samples.length,0);assert.equal(page.gaps[0].reason,'sampleTooLarge');
 }finally{history.dispose();}
});

test('64 detailed gaps coalesce and aged loss remains in bounded summaries',async()=>{
 const clock=new Clock();let n=0;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>{
 if(++n%2)throw new Error('invalid');return {...group(request,observed(100+n)),results:[{id:'metrics',error:{code:'NOT_READY',message:'not ready',retryable:false,outcome:'notApplied'}}]};
 }},{clock});
 try{const start=await history.startCollection(spec());await flush();for(let i=0;i<150;i++)await clock.advance(1000);
 const status=history.stopCollection(start.id);assert.equal(status.gaps.length,64);assert.ok(status.agedGaps.invalidSample.count>0);assert.ok(status.agedGaps.readError.count>0);assert.equal(status.gaps.reduce((n,g)=>n+g.count,0)+Object.values(status.agedGaps).reduce((n,g)=>n+g.count,0),151);
 }finally{history.dispose();}
});

test('world changes and backwards frames stop without adopting replacement while repeated frames remain valid',async()=>{
 for(const mode of ['worldChanged','WORLD_MISMATCH','frameReset','repeated']){
 const clock=new Clock();let n=0;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>{
 if(++n===1)return group(request,observed());
 if(mode==='worldChanged')return {...group(request,observed()),worldChanged:true,endFrame:null};
 if(mode==='WORLD_MISMATCH'){const {BridgeClientError}=await import('../dist/bridge-client.js');throw new BridgeClientError({code:mode,message:'world changed',outcome:'notApplied',retryable:false});}
 return group(request,observed(mode==='frameReset'?90:100));
 }},{clock});
 try{const start=await history.startCollection(spec());await flush();await clock.advance(1000);const page=history.readSeries(start.id,{});assert.equal(page.status,mode==='repeated'?'active':'stopped');assert.equal(page.samples.length,mode==='repeated'?2:1);await clock.advance(1000);assert.equal(n,mode==='repeated'?3:2);}
 finally{history.dispose();}
 }
});

test('admission snapshots mutable caller data and failed cancelled late catalog cannot commit',async()=>{
 const clock=new Clock(),pending=deferred();let signal;
 const history=new ObservationHistory({metricCatalog:(_family,options)=>{signal=options.signal;return pending.promise;},observe:async request=>group(request,observed())},{clock});
 const input=spec(),started=history.startCollection(input);input.world.bootId='changed';input.queries.length=0;await flush();history.dispose();assert.equal(signal.aborted,true);pending.resolve(copy(wire.catalog));await assert.rejects(started,e=>e.code==='DISPOSED');assert.equal(clock.timers.size,0);
});

test('all eight stopped histories remain retained and malformed admission makes zero reads',async()=>{
 const clock=new Clock();let calls=0;
 const history=new ObservationHistory({metricCatalog:async()=>{calls++;return copy(wire.catalog);},observe:async request=>group(request,observed())},{clock});
 try{
 for(const invalid of [{...spec(),unknown:1},{...spec(),scope:{kind:'city',line:null}},{...spec(),queries:[]},{...spec(),maxSamples:513},{...spec(),intervalMs:999},{...spec(),cohort:{...spec().cohort,world:{bootId:'other',epoch:1}}}])await assert.rejects(()=>history.startCollection(invalid));assert.equal(calls,0);
 const ids=[];for(let i=0;i<8;i++){const start=await history.startCollection(spec());await flush();ids.push(start.id);history.stopCollection(start.id);assert.equal(clock.timers.size,0);}
 await assert.rejects(()=>history.startCollection(spec()),e=>e.code==='SERIES_CAPACITY');for(const id of ids)assert.equal(history.readSeries(id,{}).status,'stopped');assert.equal(calls,8);
 }finally{history.dispose();}
});
test('successful admission freezes caller spec before its asynchronous catalog fetch',async()=>{
 const clock=new Clock(),catalog=deferred(),requests=[];
 const history=new ObservationHistory({metricCatalog:()=>catalog.promise,observe:async request=>{requests.push(request);return group(request,observed());}},{clock});
 try{const input=spec(),original=copy(input);const started=history.startCollection(input);input.world.bootId='mutated';input.queries.length=0;await flush();catalog.resolve(copy(wire.catalog));const status=await started;await flush();assert.deepEqual(status.spec,original);assert.deepEqual(requests[0].expectedWorld,original.world);
 const page=history.readSeries(status.id,{});page.samples[0].observation.data.values['city.money'].value=0;assert.notEqual(history.readSeries(status.id,{}).samples[0].observation.data.values['city.money'].value,0);
 }finally{history.dispose();}
});

test('eight byte-pressured histories retain at most8MiB including metadata',async()=>{
 const clock=new Clock();let frame=100;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>{
 const row=observed(frame++);row.warnings=[...Array.from({length:31},()=> '界'.repeat(128)),'multiFrameReadNotAtomic'];for(const source of row.data.sources)source.warnings=copy(row.warnings);return group(request,row);
 }},{clock});
 try{const ids=[];for(let i=0;i<8;i++){ids.push((await history.startCollection(spec())).id);await flush();}for(let i=0;i<200;i++)await clock.advance(1000);
 const statuses=ids.map(id=>history.stopCollection(id));const total=statuses.reduce((n,s)=>n+s.retainedBytes,0);assert.ok(total>6*1048576,'fixture must apply actual byte pressure');assert.ok(total<=8388608);assert.ok(statuses.every(s=>s.retainedBytes<=1048576&&s.sampleCount<51));
 }finally{history.dispose();}
});

test('source-frame regression terminates even when the enclosing observation end keeps advancing',async()=>{
 const clock=new Clock();let calls=0;
 const history=new ObservationHistory({metricCatalog:async()=>copy(wire.catalog),observe:async request=>{
 const row=observed();if(++calls===2){row.endFrame=110;for(const source of row.data.sources)source.endFrame=101;row.data.clock.endFrame=101;row.data.clock.frameIndex=101;}return group(request,row);
 }},{clock});
 try{const start=await history.startCollection(spec());await flush();await clock.advance(1000);const page=history.readSeries(start.id,{});assert.equal(page.status,'stopped');assert.equal(page.stopReason,'frameReset');assert.equal(page.samples.length,1);}
 finally{history.dispose();}
});
