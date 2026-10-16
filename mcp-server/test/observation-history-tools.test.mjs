import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {startOfflineMcp} from './fake-bridge.mjs';
import {spec,wire,observed,group} from './history-fixtures.mjs';
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
test('actual MCP catalog observe collect stop read compare workflow uses only fixed reads',async()=>{
 const calls=[];let polls=0;
 const http=createServer(async(req,res)=>{
 calls.push(req.url);
 if(req.url.startsWith('/v2/metrics/catalog'))return res.end(JSON.stringify(wire.catalog));
 if(req.url.startsWith('/v2/metrics?'))return res.end(JSON.stringify(wire['city-direct']));
 let body='';for await(const chunk of req)body+=chunk;const request=JSON.parse(body);
 assert.equal(req.url,'/v2/observations');const index=polls++;res.end(JSON.stringify(group(request,observed(100+index*10,.2+(index%2)*.1))));
 });http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{
 mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
 const tool=async(name,args)=>{const response=await mcp.client.callTool({name,arguments:args});assert.equal(response.isError,undefined,JSON.stringify(response));assert.ok(Buffer.byteLength(JSON.stringify(response))<=262144);return JSON.parse(response.content[0].text);};
 const tools=(await mcp.client.listTools()).tools;for(const name of ['cs2_collect_observations','cs2_stop_observation_collection','cs2_read_observation_history','cs2_compare_observations'])assert.ok(tools.some(t=>t.name===name));
 await tool('cs2_metric_catalog',{});await tool('cs2_observe',{world:spec().world,scope:{kind:'city'},queries:spec().queries});
 const before=calls.length;assert.equal((await mcp.client.callTool({name:'cs2_collect_observations',arguments:{...spec(),unknown:true}})).isError,true);assert.equal(calls.length,before);
 const a=await tool('cs2_collect_observations',spec());for(let i=0;polls<2&&i<150;i++)await delay(10);
 const stopped=await tool('cs2_stop_observation_collection',{id:a.id});assert.equal(stopped.sampleCount,2);assert.equal(stopped.samples,undefined);
 const page=await tool('cs2_read_observation_history',{id:a.id,limit:1});assert.equal(page.samples.length,1);assert.equal(page.nextSequence,2);
 const b=await tool('cs2_collect_observations',spec());for(let i=0;polls<4&&i<150;i++)await delay(10);await tool('cs2_stop_observation_collection',{id:b.id});
 const count=calls.length;const compared=await tool('cs2_compare_observations',{baseline:{id:a.id,fromSequence:1,toSequence:2},current:{id:b.id,fromSequence:1,toSequence:2},cohort:spec().cohort,policy:{mode:'rawNormalizedTime',phaseTolerance:.001,maxObservedPhaseStep:.2,frameDurationTolerance:0,maxSourceSpanFrames:3}});
 assert.equal(compared.status,'comparable');assert.equal(compared.improvement,null);assert.equal(calls.length,count);
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});
test('actual MCP cancellation aborts catalog admission HTTP',async()=>{
 let received;const incoming=new Promise(resolve=>received=resolve);let closed=false;
 const http=createServer((_req,res)=>{res.on('close',()=>closed=true);received();});http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);const abort=new AbortController();
 const pending=mcp.client.callTool({name:'cs2_collect_observations',arguments:spec()},undefined,{signal:abort.signal});const rejected=pending.catch(error=>error);
 await Promise.race([incoming,delay(1500).then(()=>{throw new Error('no admission HTTP');})]);abort.abort();await rejected;
 for(let i=0;!closed&&i<100;i++)await delay(10);assert.equal(closed,true);
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});

test('actual MCP stop aborts pending collection HTTP while preserving readable status',async()=>{
 let received;const incoming=new Promise(resolve=>received=resolve);let closed=false;
 const http=createServer(async(req,res)=>{if(req.url.startsWith('/v2/metrics/catalog'))return res.end(JSON.stringify(wire.catalog));for await(const chunk of req){}res.on('close',()=>closed=true);received();});http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);const start=JSON.parse((await mcp.client.callTool({name:'cs2_collect_observations',arguments:spec()})).content[0].text);
 await Promise.race([incoming,delay(1500).then(()=>{throw new Error('no observation HTTP');})]);
 const stop=await mcp.client.callTool({name:'cs2_stop_observation_collection',arguments:{id:start.id}});assert.equal(JSON.parse(stop.content[0].text).status,'stopped');
 for(let i=0;!closed&&i<100;i++)await delay(10);assert.equal(closed,true);const page=JSON.parse((await mcp.client.callTool({name:'cs2_read_observation_history',arguments:{id:start.id}})).content[0].text);assert.equal(page.samples.length,0);assert.equal(page.gaps[0].reason,'stopped');
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});

test('actual SDK validation errors for huge unknown keys remain below the history result cap',async()=>{
 const http=createServer((_req,res)=>res.end('{}'));http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
 const tools=(await mcp.client.listTools()).tools;
 for(const name of ['cs2_collect_observations','cs2_stop_observation_collection','cs2_read_observation_history','cs2_compare_observations']){
 assert.ok(Object.keys(tools.find(tool=>tool.name===name).inputSchema.properties).length>0,'bounded validation must retain advertised fields');
 const response=await mcp.client.callTool({name,arguments:{['界'.repeat(100000)]:1}});assert.equal(response.isError,true);assert.ok(Buffer.byteLength(JSON.stringify(response))<=262144,'SDK error exceeded result cap');
 }
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});

test('actual listTools preserves exact required optional default and nested history constraints',async()=>{
 const mcp=await startOfflineMcp('http://127.0.0.1:1');
 try{
 const listed=(await mcp.client.listTools()).tools;
 const required={cs2_collect_observations:['world','queries','scope','cohort','maxSamples','intervalMs'],cs2_stop_observation_collection:['id'],cs2_read_observation_history:['id'],cs2_compare_observations:['baseline','current','cohort','policy']};
 const schemas=Object.fromEntries(Object.keys(required).map(name=>[name,listed.find(tool=>tool.name===name).inputSchema]));
 const exact=(schema,keys)=>{assert.deepEqual(new Set(schema.required),new Set(keys));assert.equal(schema.additionalProperties,false);};
 const resolve=(root,node)=>{while(node.$ref)node=node.$ref.slice(2).split('/').reduce((value,key)=>value[key],root);return node;};
 for(const [name,keys]of Object.entries(required))exact(schemas[name],keys);
 const collect=schemas.cs2_collect_observations,c=collect.properties;
 exact(c.world,['bootId','epoch']);assert.deepEqual(c.world.properties.bootId,{type:'string',minLength:1,maxLength:128});assert.deepEqual(c.world.properties.epoch,{type:'integer',minimum:0,maximum:Number.MAX_SAFE_INTEGER});
 assert.equal(c.queries.minItems,1);assert.equal(c.queries.maxItems,64);exact(c.queries.items,['key','parameters']);assert.deepEqual(new Set(c.queries.items.properties.key.enum),new Set(wire.catalog.map(definition=>definition.key)));assert.deepEqual(c.queries.items.properties.parameters,{type:'object',properties:{},additionalProperties:false});
 exact(c.scope.anyOf[0],['kind']);assert.equal(c.scope.anyOf[0].properties.kind.const,'city');exact(c.scope.anyOf[1],['kind','line']);assert.equal(c.scope.anyOf[1].properties.kind.const,'selectedLine');
 const line=resolve(collect,c.scope.anyOf[1].properties.line);exact(line,['world','index','version']);assert.deepEqual(resolve(collect,line.properties.world),c.world);for(const field of ['index','version'])assert.deepEqual(line.properties[field],{type:'integer',minimum:1,maximum:2147483647});
 for(const [index,kind,count]of [[0,'city',0],[1,'fixedSelectedLine',1]]){const branch=c.cohort.anyOf[index];exact(branch,['kind','world','members']);assert.equal(branch.properties.kind.const,kind);assert.equal(branch.properties.members.minItems,count);assert.equal(branch.properties.members.maxItems,count);assert.deepEqual(resolve(collect,branch.properties.members.items),line);}
 assert.deepEqual(c.maxSamples,{type:'integer',minimum:1,maximum:512});assert.deepEqual(c.intervalMs,{type:'integer',minimum:1000,maximum:86400000});
 const stop=schemas.cs2_stop_observation_collection;assert.deepEqual(stop.properties.id,{type:'string',format:'uuid'});
 const read=schemas.cs2_read_observation_history;assert.deepEqual(read.properties.id,{type:'string',format:'uuid'});assert.deepEqual(read.properties.limit,{type:'integer',minimum:1,maximum:128,default:128});assert.deepEqual(read.properties.afterSequence,{type:'integer',minimum:1,maximum:Number.MAX_SAFE_INTEGER});
 const compare=schemas.cs2_compare_observations;for(const field of ['baseline','current']){const range=resolve(compare,compare.properties[field]);exact(range,['id','fromSequence','toSequence']);assert.deepEqual(range.properties.id,{type:'string',format:'uuid'});for(const bound of ['fromSequence','toSequence'])assert.deepEqual(resolve(compare,range.properties[bound]),{type:'integer',minimum:1,maximum:Number.MAX_SAFE_INTEGER});}
 for(const [index,kind,count]of [[0,'city',0],[1,'fixedSelectedLine',1]]){const branch=compare.properties.cohort.anyOf[index];exact(branch,['kind','world','members']);assert.equal(branch.properties.kind.const,kind);assert.equal(branch.properties.members.minItems,count);assert.equal(branch.properties.members.maxItems,count);const member=resolve(compare,branch.properties.members.items);exact(member,['world','index','version']);assert.deepEqual(resolve(compare,member.properties.world),c.world);for(const field of ['index','version'])assert.deepEqual(member.properties[field],{type:'integer',minimum:1,maximum:2147483647});}
 const policy=compare.properties.policy;exact(policy,['mode','phaseTolerance','maxObservedPhaseStep','frameDurationTolerance','maxSourceSpanFrames']);assert.equal(policy.properties.mode.const,'rawNormalizedTime');assert.deepEqual(policy.properties.phaseTolerance,{type:'number',minimum:0,maximum:.05});assert.deepEqual(policy.properties.maxObservedPhaseStep,{type:'number',exclusiveMinimum:0,exclusiveMaximum:1});for(const field of ['frameDurationTolerance','maxSourceSpanFrames'])assert.deepEqual(policy.properties[field],{type:'integer',minimum:0,maximum:Number.MAX_SAFE_INTEGER});
 }finally{await mcp.close();}
});

test('all four SDK schemas reject missing invalid and huge unknown inputs with bounded errors and zero HTTP',async()=>{
 let calls=0;const http=createServer((_req,res)=>{calls++;res.end('{}');});http.listen(0,'127.0.0.1');await once(http,'listening');let mcp;
 const id='00000000-0000-4000-8000-000000000001',range={id,fromSequence:1,toSequence:2};
 const args={cs2_collect_observations:spec(),cs2_stop_observation_collection:{id},cs2_read_observation_history:{id},cs2_compare_observations:{baseline:range,current:range,cohort:spec().cohort,policy:{mode:'rawNormalizedTime',phaseTolerance:.001,maxObservedPhaseStep:.2,frameDurationTolerance:0,maxSourceSpanFrames:3}}};
 const huge='界'.repeat(100000);
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${http.address().port}`);
 for(const [name,valid]of Object.entries(args)){
 const nested=name==='cs2_collect_observations'?{...valid,world:{...valid.world,[huge]:1}}:name==='cs2_compare_observations'?{...valid,policy:{...valid.policy,[huge]:1}}:{id:{[huge]:1}};
 const missing=Object.keys(valid).filter(key=>!['limit','afterSequence'].includes(key)).map(key=>Object.fromEntries(Object.entries(valid).filter(([field])=>field!==key)));
 for(const input of [{},{[huge]:1},{...valid,[huge]:1},nested,...missing]){
 const result=await mcp.client.callTool({name,arguments:input});assert.equal(result.isError,true);assert.ok(Buffer.byteLength(JSON.stringify(result))<=262144);assert.equal(calls,0);
 }
 }
 }finally{await mcp?.close();http.closeAllConnections();http.close();await once(http,'close');}
});
