import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {createBridgeClient} from '../dist/bridge-client.js';
import {startOfflineMcp} from './fake-bridge.mjs';
const wire=JSON.parse(readFileSync(new URL('./fixtures/construction-relationships-wire.json',import.meta.url),'utf8'));
const copy=value=>structuredClone(value);

test('actual MCP literal relationship mode uses strict bounded inventory GET and preserves real C# output',async()=>{
 const calls=[];const http=createServer((req,res)=>{calls.push(req.url);res.end(JSON.stringify(wire.direct));});http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);const tool=(await mcp.client.listTools()).tools.find(t=>t.name==='cs2_construction_inventory');
 const result=await mcp.client.callTool({name:tool.name,arguments:wire.request});assert.equal(result.isError,undefined,JSON.stringify(result));assert.deepEqual(JSON.parse(result.content[0].text),wire.direct);assert.equal(new URL(calls[0],'http://test').searchParams.get('relationships'),'directDepth1');
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});
test('typed inventory transport independently accepts literal expansion',async()=>{
 let sent;const client=createBridgeClient({baseUrl:'http://127.0.0.1:1',fetchImpl:async(url)=>{sent=url;return Response.json(wire.direct);}});
 assert.deepEqual(await client.constructionInventory(wire.request),wire.direct);assert.equal(new URL(sent).searchParams.get('relationships'),'directDepth1');
});

test('actual C# direct and O7 snapshots pass shared semantic parser and preserve source coverage',async()=>{
 const {parseConstructionObservation}=await import('../dist/construction-contracts.js');const {parseObservationResult}=await import('../dist/observation-contracts.js');
 assert.deepEqual(parseConstructionObservation(wire.direct,wire.request),wire.direct);
 const result=parseObservationResult(wire.o7,{expectedWorld:wire.request.world,items:[{id:'i',name:'construction.inventory',args:wire.request}]});assert.deepEqual(result.results[0].observation.data,wire.direct.data);
 const baseline={world:wire.request.world,entities:wire.request.entities};assert.deepEqual(parseConstructionObservation(wire.baseline,baseline),wire.baseline);
 const r=wire.direct.data.relationships;assert.equal(r.sources.length,12);assert.equal(r.links.length,9);assert.equal(r.targets.length,5);assert.equal(r.sources.find(s=>s.kind==='owner'&&s.source.index===1).coverageComplete,true);assert.equal(r.links[2].targetIndex,r.links[3].targetIndex);assert.equal(r.links[4].reason,'nullReference');assert.equal(r.links[5].rawIndex,-3);
});
test('direct and O7 consumers reject malformed identity geometry and relationship semantics',async()=>{
 const {parseConstructionObservation}=await import('../dist/construction-contracts.js');const {parseObservationResult}=await import('../dist/observation-contracts.js');
 for(const mutate of [
 value=>value.world.epoch++,value=>value.data.requestedEntities[0].version++,value=>value.data.entities.pop(),
 value=>value.data.entities[1].rootBounds.axisX.x=2,value=>value.data.relationships.targets[1].network.curve.b.y=Infinity,
 value=>value.data.relationships.links[0].targetIndex=2,value=>value.data.relationships.links[1].target.version++,
 value=>value.data.relationships.links[4].targetIndex=0,value=>value.data.relationships.links[5].rawIndex=3,
 value=>value.data.relationships.sources[0].observedCount=2,value=>value.data.relationships.sources[0].coverageComplete=false,
 value=>value.data.relationships.targets.push(copy(value.data.relationships.targets[0])),value=>value.data.relationships.additionalTargetReads=99,
 value=>value.data.relationships.limits.maxSlots=129,value=>value.data.relationships=null,
 value=>value.data.relationships.sources[2].observedCount=0,
 ]){const value=copy(wire.direct);mutate(value);assert.throws(()=>parseConstructionObservation(value,wire.request));const group=copy(wire.o7);group.results[0].observation=value;assert.throws(()=>parseObservationResult(group,{expectedWorld:wire.request.world,items:[{id:'i',name:'construction.inventory',args:wire.request}]}));}
 const input={world:wire.request.world,entities:wire.request.entities};assert.throws(()=>parseConstructionObservation(wire.direct,input));
 const bad=copy(wire.baseline);bad.data.entities[0].family='station';assert.throws(()=>parseConstructionObservation(bad,input));
});
test('actual SDK advertises required refs plus optional literal and rejects huge malformed options before HTTP',async()=>{
 let calls=0;const http=createServer((_req,res)=>{calls++;res.end(JSON.stringify(wire.direct));});http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);const tool=(await mcp.client.listTools()).tools.find(t=>t.name==='cs2_construction_inventory');const schema=tool.inputSchema;
 assert.deepEqual(new Set(schema.required),new Set(['world','entities']));assert.deepEqual(Object.keys(schema.properties).sort(),['entities','geometryDiagnostics','relationshipPage','relationships','world']);assert.equal(schema.additionalProperties,false);assert.equal(schema.properties.relationships.const,'directDepth1');assert.equal(schema.properties.entities.maxItems,32);assert.deepEqual(new Set(schema.properties.entities.items.required),new Set(['world','index','version']));
 const huge='界'.repeat(100000);
 for(const args of [{},{[huge]:1},{...wire.request,[huge]:1},{...wire.request,world:{...wire.request.world,[huge]:1}},...['true',true,2,'directDepth2',null].map(relationships=>({...wire.request,relationships})),{...wire.request,depth:1},{...wire.request,includeFamilies:true},{...wire.request,entities:[wire.request.entities[0],wire.request.entities[0]]}]){
 const result=await mcp.client.callTool({name:tool.name,arguments:args});assert.equal(result.isError,true);assert.ok(Buffer.byteLength(JSON.stringify(result))<=262144);assert.equal(calls,0);
 }
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});
test('typed direct client caps all status bodies and validates baseline before exposure',async()=>{
 for(const [index,response] of [()=>new Response('x'.repeat(262145)),()=>new Response('x'.repeat(262145),{status:500,headers:{'content-length':'1'}}),()=>new Response(new Uint8Array([255])),()=>Response.json({}),()=>Response.json({...wire.direct,unexpected:true})].entries()){
 const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1',fetchImpl:async()=>response()});await assert.rejects(()=>bridge.constructionInventory(wire.request),e=>e.details.code===(index<2?'RESULT_TOO_LARGE':'PROTOCOL_ERROR')&&(index>=2||e.details.message.startsWith('transportResponseByteLimit:')));
 }
 let calls=0;const bridge=createBridgeClient({baseUrl:'http://example.com',fetchImpl:async()=>{calls++;return Response.json(wire.direct);}});await assert.rejects(()=>bridge.constructionInventory(wire.request),e=>e.details.code==='INVALID_ARGUMENT');assert.equal(calls,0);
 const invalid=copy(wire.baseline);invalid.data.entities[0].ownerReason=null;const base={world:wire.request.world,entities:wire.request.entities};await assert.rejects(()=>createBridgeClient({baseUrl:'http://127.0.0.1',fetchImpl:async()=>Response.json(invalid)}).constructionInventory(base));
});
test('actual MCP independently refuses escaped result exceeding wrapper cap after valid bounded HTTP',async()=>{
 const request={world:copy(wire.request.world),entities:Array.from({length:32},(_,i)=>({world:copy(wire.request.world),index:i+1,version:1}))};
 const payload=copy(wire.baseline);payload.data.requestedEntities=copy(request.entities);payload.data.entities=request.entities.map(entity=>{const value={...copy(wire.baseline.data.entities[0]),entity};for(const key of Object.keys(value))if(key.endsWith('Reason'))value[key]='"'.repeat(128);return value;});
 const raw=JSON.stringify(payload);assert.ok(Buffer.byteLength(raw)<262144);assert.ok(Buffer.byteLength(JSON.stringify({content:[{type:'text',text:raw}]}))>262144);
 const http=createServer((_req,res)=>res.end(raw));http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{const bridge=createBridgeClient({baseUrl:`http://127.0.0.1:${http.address().port}`});assert.equal((await bridge.constructionInventory(request)).data.entities.length,32);
 mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);const result=await mcp.client.callTool({name:'cs2_construction_inventory',arguments:request});assert.equal(result.isError,true);assert.equal(JSON.parse(result.content[0].text).error.code,'RESULT_TOO_LARGE');assert.ok(Buffer.byteLength(JSON.stringify(result))<262144);
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});
test('actual MCP O7 forwards the same literal mode through POST and validates its returned slots',async()=>{
 const calls=[];const http=createServer(async(req,res)=>{let body='';for await(const chunk of req)body+=chunk;calls.push({path:req.url,request:JSON.parse(body)});res.end(JSON.stringify(wire.o7));});http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);const input={expectedWorld:wire.request.world,items:[{id:'i',name:'construction.inventory',args:wire.request}]};
 const result=await mcp.client.callTool({name:'cs2_observe_batch',arguments:input});assert.equal(result.isError,undefined);assert.deepEqual(calls,[{path:'/v2/observations',request:input}]);
 for(const patch of [{relationships:true},{relationships:'recursive'},{family:'station'}]){assert.equal((await mcp.client.callTool({name:'cs2_observe_batch',arguments:{...input,items:[{...input.items[0],args:{...wire.request,...patch}}]}})).isError,true);}assert.equal(calls.length,1);
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});
