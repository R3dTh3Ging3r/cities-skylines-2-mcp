import test from 'node:test';
import assert from 'node:assert/strict';
import {createBridgeClient} from '../dist/bridge-client.js';
import {startOfflineMcp} from './fake-bridge.mjs';
import {createServer} from 'node:http';
import {once} from 'node:events';

const world={bootId:'observed + world',epoch:3};
const line={world,index:12,version:2};
const request={line,mode:'airplane',purpose:'cargo'};
const capabilities={scope:'explicitLine',support:'partial',maxRouteEntries:128,modes:['bus','tram','metro','train','airplane'],nativeVerification:'transit-readback-v1:notRun',discovery:false,load:false,nativeValidity:false};
export function sample(){return structuredClone({world,startFrame:10,endFrame:10,sampledAtUtc:'2026-10-16T00:00:00Z',freshness:'current',warnings:['partialSelectedTransit','nativeTransitFixtureNotRun'],data:{scope:'explicitLine',requestedLine:line,items:[],returned:0,matched:0,filter:{state:'excluded',reasons:['modeMismatch']},nextCursor:null,truncated:false,nativeVerification:'transit-readback-v1:notRun',capabilities}});}
function selectedSample(){
 const result=sample();result.data.matched=null;result.data.returned=1;result.data.filter={state:'unverifiable',reasons:['modeUnavailable','purposeUnavailable']};
 result.data.items=[{entity:structuredClone(line),assetKey:null,assetKeyReason:'prefabNameUnavailable',owner:null,ownerReason:'ownerUnavailable',routeNumber:null,mode:null,modeReason:'transportModeUnavailable',purpose:'unknown',purposeFlags:{passenger:null,cargo:null},purposeReason:'transportPurposeUnavailable',name:null,nameReason:'labelReaderNotBounded',active:null,activeReason:'lineActiveReaderNotVerified',vehicles:null,vehiclesReason:'boundedFleetReaderUnavailable',load:{kind:null,value:null,capacity:null,unit:null,occupancyRatio:null,reason:'boundedLoadReaderUnavailable'},completeness:{state:'complete',waypointCount:0,segmentCount:0,readWaypoints:0,readSegments:0,reasons:[]},waypoints:[],segments:[],health:{state:'unknown',reasons:['emptyRouteBuffer'],brokenWaypoints:[],invalidSegments:[],rawLength:null,lengthUnit:null,lengthUnitReason:'nativeSegmentDistanceUnitNotVerified',lengthValidation:'finiteNonnegativeSegmentsOnly',nativeValidity:'unknown',nativeValidityReason:'committedRouteFixtureNotRun',assessment:'structuralFactsOnly'},candidateDuplicates:null,candidateDuplicatesReason:'discoveryUnsupported'}];return result;
}

test('selected transit transport requires caller-world refs and preserves strict compact GET',async()=>{
 let calls=0,url;let payload=sample();
 const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1234',fetchImpl:async u=>{calls++;url=String(u);return new Response(JSON.stringify(payload));}});
 assert.equal(typeof bridge.transitStatus,'function','typed selected-line transport missing');
 assert.deepEqual(await bridge.transitStatus(request),payload);
 const parsed=new URL(url);assert.equal(parsed.pathname,'/v2/transit/status');assert.equal(parsed.searchParams.get('bootId'),world.bootId);assert.equal(parsed.searchParams.get('line'),'12:2');assert.equal(parsed.searchParams.get('mode'),'airplane');
 for(const bad of [{line:{index:12,version:2}},{...request,line:{...line,world:undefined}},{...request,cursor:'x'},{...request,mode:'air'},{...request,line:{...line,index:0}}])await assert.rejects(bridge.transitStatus(bad));
 assert.equal(calls,1);
 payload={...sample(),world:{...world,epoch:4}};await assert.rejects(bridge.transitStatus(request));
 payload=sample();payload.data.requestedLine={...line,version:3};await assert.rejects(bridge.transitStatus(request));
 payload=sample();payload.data.filter.state='unverifiable';payload.data.matched=0;await assert.rejects(bridge.transitStatus(request));
});

test('actual MCP tools register selected status and partial capabilities and use typed GET',async()=>{
 const calls=[];const server=createServer((req,res)=>{calls.push(req.url);res.end(JSON.stringify(req.url.startsWith('/v2/transit/status')?sample():{scope:'explicitLine',support:'partial',maxRouteEntries:128,modes:['bus','tram','metro','train','airplane'],nativeVerification:'transit-readback-v1:notRun',discovery:false,load:false,nativeValidity:false}));});
 server.listen(0,'127.0.0.1');await once(server,'listening');let mcp;
 try{mcp=await startOfflineMcp(`http://127.0.0.1:${server.address().port}`);const {tools}=await mcp.client.listTools();assert.ok(tools.some(t=>t.name==='cs2_transit_status'));assert.ok(tools.some(t=>t.name==='cs2_transit_capabilities'));
  const result=await mcp.client.callTool({name:'cs2_transit_status',arguments:request});assert.equal(result.isError,undefined);assert.deepEqual(JSON.parse(result.content[0].text),sample());
  const caps=await mcp.client.callTool({name:'cs2_transit_capabilities',arguments:{}});assert.equal(caps.isError,undefined);
  const bad=await mcp.client.callTool({name:'cs2_transit_status',arguments:{line:{index:12,version:2}}});assert.equal(bad.isError,true);assert.equal(calls.length,2);
 }finally{await mcp?.close();server.closeAllConnections();server.close();await once(server,'close');}
});

test('observation catalog accepts full selected ref and rejects mismatched returned world',async()=>{
 const input={items:[{id:'route',name:'transit.selectedLine',args:request}]};const result={world,startFrame:10,endFrame:10,worldChanged:false,results:[{id:'route',observation:sample()}]};let calls=0;
 const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1234',fetchImpl:async()=>{calls++;return new Response(JSON.stringify(result));}});
 assert.deepEqual(await bridge.observe(input),result);
 await assert.rejects(bridge.observe({items:[{id:'route',name:'transit.selectedLine',args:{line:{index:12,version:2}}}]}));assert.equal(calls,1);
 result.world={...world,epoch:4};await assert.rejects(bridge.observe(input));
});

test('selected transit caps streamed error bodies as well as success bodies',async()=>{
 for(const status of [200,503]){
  let cancelled=false;
  const bridge=createBridgeClient({baseUrl:'http://127.0.0.1:1234',fetchImpl:async()=>{const response=new Response(new ReadableStream({start(controller){controller.enqueue(new Uint8Array(262145));},cancel(){cancelled=true;}}),{status,headers:{'content-length':'1'}});response.text=async()=>{throw new Error('unbounded text reader used');};return response;}});
  await assert.rejects(bridge.transitStatus(request));assert.equal(cancelled,true,`status${status} must use capped stream`);
 }
});

test('selected diagnostic item retains unknown filters and refuses native validity or unit claims',async()=>{
 let payload=selectedSample();const bridge=createBridgeClient({baseUrl:'http://127.0.0.1',fetchImpl:async()=>new Response(JSON.stringify(payload))});
 assert.deepEqual(await bridge.transitStatus(request),payload);
 for(const mutate of [p=>p.data.items[0].health.nativeValidity='valid',p=>p.data.items[0].health.lengthUnit='m',p=>p.data.matched=0,p=>p.data.items[0].entity.world={...world,epoch:4},p=>p.data.items[0].health.rawLength=-1,p=>p.data.items[0].purpose='freight',p=>p.data.items[0].health.state='invalid',p=>p.data.filter.state='notRequested']){
  payload=selectedSample();mutate(payload);await assert.rejects(bridge.transitStatus(request));
 }
});
