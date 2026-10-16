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
